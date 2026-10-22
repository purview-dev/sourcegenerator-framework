# Component failures

A Roslyn component must never let an exception escape. This page covers what it costs when one does, the
guard the framework provides, and how to configure the diagnostic it reports.

`PSGFR44` enforces the rule at authoring time; `ComponentGuard` is how you satisfy it at runtime.

## What an escaping exception costs

The consequence depends on the component, and none of them are good:

| Component | What the consumer sees |
| -- | -- |
| Source generator | `CS8785`, **and no generated files at all** for that compilation — so every type the generator was meant to produce is also reported missing. |
| Analyzer | `AD0001`, with a stack trace through code they do not own. |
| Code fix / refactoring | The light-bulb fails; the action is not offered or not applied. |
| Diagnostic suppressor | It silently stops suppressing, and the diagnostics it was hiding reappear with no explanation. |

In every case the failure is attributed to the consumer's build rather than to the component, and the
information they are given is not something they can act on.

## The guard

`ComponentGuard` runs a body and converts an unhandled exception into a diagnostic. Not every component can
report a diagnostic, so it offers two shapes rather than pretending to a uniform one.

### Source generators — reported for you

Nothing to do. The framework's `RegisterSourceOutput` already wraps the emit callback:

```csharp
context.RegisterSourceOutput(
    targets,
    generationContext,
    static (spc, target, ctx) =>
    {
        // An exception here is reported as a diagnostic, not CS8785.
        var writer = ctx.CreateCodeWriter();
        writer.Line($"partial class {target.Name} {{ }}");
        spc.AddSource($"{target.Name}.g.cs", writer.ToString());
    });
```

For a generator that registers its own output, call the guard directly:

```csharp
context.RegisterSourceOutput(
    targets,
    (spc, target) => ComponentGuard.Run<MyGenerator>(spc, () => Emit(spc, target)));
```

### Analyzers — report through the analysis context

An analyzer must declare every diagnostic it can report in `SupportedDiagnostics`, so declare the failure
descriptor alongside your rules. `ComponentFailure.For<T>()` is cached, so the instance you declare is the
instance the guard reports:

```csharp
public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    [Rule, ComponentFailure.For<MyAnalyzer>()];

static void Analyze(SyntaxNodeAnalysisContext context) =>
    ComponentGuard.Run<MyAnalyzer>(
        context.ReportDiagnostic,
        () => AnalyzeCore(context),
        cancellationToken: context.CancellationToken);
```

Reporting an id that is not in `SupportedDiagnostics` makes Roslyn throw — which is the failure this guard
exists to prevent, so do not skip the declaration.

### Suppressors, code fixes and refactorings — no channel to report on

None of these can report a diagnostic. The correct behaviour is to do nothing: suppress nothing, offer no
fix. `TryRun` says whether the body succeeded:

```csharp
public override void ReportSuppressions(SuppressionAnalysisContext context)
{
    if (!ComponentGuard.TryRun<MySuppressor>(() => SuppressCore(context), out var failure))
    {
        // Nothing was suppressed. Log it if you have a logger; there is nowhere to report it.
        return;
    }
}
```

## Cancellation is never caught

Roslyn cancels analysis on every keystroke. An `OperationCanceledException` raised while the supplied token
is cancelled propagates untouched — swallowing it would turn a normal cancellation into a reported defect
and hide from Roslyn that the work was abandoned.

An `OperationCanceledException` thrown when nothing was cancelled is an ordinary defect and *is* reported.

## The default diagnostic

Every part of the diagnostic defaults from the component itself, because the diagnostic belongs to the
consumer rather than to the framework — it appears in their build, and they decide whether to suppress it.

| Part | Default |
| -- | -- |
| Id | The initial of each namespace segment, upper cased, capped at four, plus `0001`. `Acme.Widgets.Generators` → `AWG0001`. A single-segment namespace uses its first three letters; a type with no namespace falls back to `GEN`. |
| Title | `Source generator failed`, `Analyzer failed`, `Code fix failed`, … — resolved from the base type or interface the component implements. |
| Message | The component name, the exception type, and the exception message. |
| Category | The component's root namespace, so failures group under the consumer's own product. |
| Severity | `Warning`. A component that failed has produced nothing; failing the build on top of that rarely helps. |

The derived id is stable for a given namespace, which is what matters — a consumer suppressing it in an
`.editorconfig` must not find it renamed by an unrelated change. It is **not** guaranteed unique across two
assemblies sharing a namespace shape; set the id explicitly when that matters.

## Overriding the details

Pass `ComponentFailureOptions` to override any part:

```csharp
static readonly ComponentFailureOptions Failure = new()
{
    Id = "ACME9999",
    Title = "Widget generator fell over",
    Severity = DiagnosticSeverity.Error,
    HelpLinkUri = "https://acme.example/diagnostics/acme9999",
};

public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    [Rule, ComponentFailure.For<MyAnalyzer>(Failure)];
```

For a generator using the framework's `RegisterSourceOutput`, declare them on `GenerationSettings`
alongside everything else the generator says about itself:

```csharp
var generationContext = IncrementalPipeline.DefaultGenerationContextValueProvider(
    context,
    new GenerationSettings("MyGenerator", "1.0.0") { FailureOptions = Failure });
```

Hold the options in a `static readonly` field and pass the same instance everywhere. `ComponentFailure.For`
caches on `(component, options)`, so a fresh options instance per call produces a fresh descriptor — and an
analyzer that declares one descriptor then reports another makes Roslyn throw.

`IncludeStackTrace` adds the stack trace to the message. It is off by default: a stack trace through a
component's internals is noise to the consumer and makes the diagnostic text unstable between builds. Turn
it on when you are asking someone to report a bug.

### Why these are code settings, not build properties

An analyzer declares `SupportedDiagnostics` before any analyzer options exist, so a build property could not
reach it in time. Code-level configuration is the only form that works for all four component kinds.
