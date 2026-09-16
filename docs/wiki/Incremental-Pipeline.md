# Incremental Pipeline

`IncrementalPipeline` provides extension methods for composing `IncrementalValueProvider<T>` and
`IncrementalValuesProvider<T>` pipelines — attribute-based discovery, generation-context creation,
disable-property checks, and thin source-output registration. It is designed around the golden rule
from the [best-practices guide](Guide.md): **pipeline values must be immutable and value-equatable.**

## GenerationContext

`GenerationContext` is a base execution-services context that carries:

- the Roslyn `Compilation`;
- immutable generator `GenerationSettings`;
- an optional `ISourceGenLogger`; and
- a factory for independently owned `CodeWriter` instances.

```csharp
var contextProvider = IncrementalPipeline.DefaultGenerationContextValueProvider<MyGenerator>(context);
```

Create a fresh writer through the generation context so it inherits the configuration:

```csharp
var writer = generationContext.CreateCodeWriter();
```

`CreateCodeWriter()` returns a new, independently owned instance on every call. The writer is not
stored on `GenerationContext`; keep it scoped to the source-output operation that owns the generated
source.

### Custom generation contexts

Custom contexts do not need to accept or read build properties themselves:

```csharp
public sealed class MyGenerationContext : GenerationContext
{
    public MyGenerationContext(
        Compilation compilation,
        GenerationSettings settings,
        ISourceGenLogger? logger)
        : base(compilation, settings, logger)
    {
    }
}
```

Use the ordinary context-provider overload. The framework combines the compiler-visible property
with the compilation and supplies the resulting immutable settings to the custom context factory:

```csharp
var contextProvider = IncrementalPipeline.GenerationContextValueProvider(
    context,
    nameof(MyGenerator),
    "1.0.0",
    factory: static (compilation, settings, logger, cancellationToken) =>
    {
        cancellationToken.ThrowIfCancellationRequested();
        return new MyGenerationContext(compilation, settings, logger);
    },
    disablePropertyName: "MyGenerator_Disable"
);
```

The provider resolves scope validation, generator disabling, and test logging from analyzer-config
properties before invoking the factory. The supplied logger is created internally only when logging
is enabled and a sink is registered for that run.

## Keep CodeWriter out of incremental contexts

Treat `GenerationContext` values as cached incremental-pipeline state and each `CodeWriter` as
mutable, output-scoped execution state. Create the writer inside the registered source-output
callback, after the incremental cache boundary. Creating it in the callback and passing it to
emitter/helper methods called from that same callback is the intended pattern; the only thing that
is forbidden is persisting the writer in pipeline state, where Roslyn caches it:

```csharp
IncrementalPipeline.RegisterSourceOutput(
    context,
    targets,
    contextProvider,
    static (spc, target, generationContext) =>
    {
        var writer = generationContext.CreateCodeWriter();
        EmitTarget(generationContext, writer, target);
        spc.AddSource($"{target.Name}.g.cs", writer.ToString());
    }
);
```

This separation is intentional:

- Roslyn caches the complete value published by an incremental provider. It does not provide a way
  to exclude one property of that value from caching.
- `CodeWriter` is mutable. Caching one can retain previously written source when the context is
  reused for another output or generator run.
- Source-output callbacks may process independent targets concurrently. Sharing a writer can mix
  their output and introduce data races.
- A fresh writer gives each generated source independent scope tracking and deterministic ownership.

These rules also apply to custom contexts: **never add or assign a `CodeWriter` property or field on
a class derived from `GenerationContext`**. A custom context is still produced by an incremental
provider and cached as one complete value. Store only compilation-derived services and immutable
configuration there, and call `CreateCodeWriter()` in the output callback.

When emitter methods need both logging/context services and writing, either pass the context and
output-scoped writer separately, or compose them into a short-lived output wrapper created inside
the callback. Such a wrapper must never be returned from an incremental provider:

```csharp
public sealed class GenerationOutputContext<TContext> : ISourceGenLogger
    where TContext : GenerationContext
{
    public GenerationOutputContext(TContext generation)
    {
        Generation = generation;
        Writer = generation.CreateCodeWriter();
    }

    public TContext Generation { get; }
    public CodeWriter Writer { get; }

    public void Log(
        SourceGenLogLevel level,
        int indentation,
        string message,
        params object[] args) =>
        Generation.Log(level, indentation, message, args);
}
```

The wrapper reduces emitter parameter noise without extending the writer's lifetime into Roslyn's
incremental cache.

## GeneratorResult<T> and diagnostics that don't stop generation

`IncrementalPipeline.RegisterSourceOutput` combines targets with the generation context, reports
diagnostics, and runs the generator callback only for successful results:

```csharp
var targets = IncrementalPipeline.ForAttributeWithMetadataName(
    context,
    AttributeType,
    static (ctx, ct) =>
    {
        var symbol = ctx.TargetSymbol;
        return symbol is null
            ? GeneratorResult<string>.Empty
            : GeneratorResult<string>.Create(symbol.Name);
    }
);

var contextProvider = IncrementalPipeline.DefaultGenerationContextValueProvider<MyGenerator>(context);

IncrementalPipeline.RegisterSourceOutput(
    context,
    targets,
    contextProvider,
    static (spc, name, generationContext) =>
    {
        var writer = generationContext.CreateCodeWriter();
        writer.Comment($"generated {name}");
        spc.AddSource($"{name}.g.cs", writer.ToString());
    }
);
```

The registered callback runs only when `GeneratorResult<T>.ShouldProcess` is `true` — the result
carries a value and none of its carried diagnostics are blocking.

`ReportableDiagnostic.IsBlocking` is an explicit, per-diagnostic decision, independent of the
diagnostic's severity. `GeneratorResult<T>.ShouldProcess` is `true` when the result carries a value
and none of its diagnostics are blocking, so an `Error`-severity diagnostic can still allow
generation to continue. This is useful when the generated code helps the developer fix the problem —
for example, a generator that emits an abstract base class with methods the user must override can
report an error for each missing override while still emitting the base class, so the user can see
exactly what to implement:

```csharp
static readonly DiagnosticDescriptor MissingOverride = new(
    "MYGEN001",
    "Missing override",
    "Type '{0}' must override '{1}'",
    "Usage",
    DiagnosticSeverity.Error,
    isEnabledByDefault: true
);

var targets = IncrementalPipeline.ForAttributeWithMetadataName(
    context,
    AttributeType,
    static (ctx, ct) =>
    {
        var symbol = ctx.TargetSymbol;
        var model = new BaseModel(symbol.Name);

        // An error-severity diagnostic that explicitly allows generation to continue:
        // IsBlocking is false, so ShouldProcess stays true and the base class is emitted.
        var diagnostic = ReportableDiagnostic.Create(
            MissingOverride,
            isBlocking: false,
            symbol,
            symbol.Name,
            "Execute"
        );

        return GeneratorResult<BaseModel>.Create(model, diagnostic);
    }
);
```

Blocking diagnostics (`isBlocking: true`) stop generation for that target while still being reported.
`GeneratorResult<T>.HasBlockingDiagnostics` reports whether any carried diagnostic blocked processing;
`HasErrorDiagnostics` reports the severity-based view (whether any diagnostic has an `Error`
`DefaultSeverity`).

## Disabling a generator at build time

Pass the generator's compiler-visible disable property to the context provider. Its resolved value is
included in `GenerationSettings` automatically:

```xml
<PropertyGroup>
  <MyGenerator_Disable>true</MyGenerator_Disable>
</PropertyGroup>
```

```csharp
var contextProvider = IncrementalPipeline.DefaultGenerationContextValueProvider(
    context,
    nameof(MyGenerator),
    "1.0.0",
    disablePropertyName: "MyGenerator_Disable"
);

// In the output stage:
if (generationContext.Settings.IsSourceGeneratorDisabled)
    return;
```

`IsDisabledValueProvider` remains available when expensive upstream transforms must be filtered
before they are combined with the generation context.

## Scope validation

The default generation-context provider reads the
`PurviewSourceGeneratorFrameworkValidateCodeWriterScopes` MSBuild property and threads it into
`GenerationSettings.ValidateCodeWriterScopes`. When enabled, `ToString()` throws
`CodeWriterScopeValidationException` if `OpenScopeCount` is not zero. See
[Code-Writer.md](Code-Writer.md#construction-and-scope-validation).

## Test logging

Framework logging is disabled in ordinary compiler runs. The testing integration enables it by
registering an isolated sink and supplying a per-run session ID through analyzer config. Context
providers create the internal logger automatically; generators do not implement a logging interface
and no logging-support source is generated.

The sink registry stores callbacks only. It never buffers log entries. If logging is disabled, the
session ID is missing, or no matching sink is registered, the provider supplies no logger and log
calls are discarded without storing entries. Test sinks own any entries they choose to capture and
are removed when the test run completes.

## Tracking names and step-cache tests

The framework's pipeline helpers assign a tracking name to every stage so cache tests can assert
which stages were recomputed. See [Step-Cache-Tests.md](Step-Cache-Tests.md) for the tracking-name
table and the golden test matrix.