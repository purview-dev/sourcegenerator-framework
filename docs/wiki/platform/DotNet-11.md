# .NET 11 support and features

Version 2.0.0 moves the repository to .NET 11 and drops .NET 8 and .NET 9 from the build and test
matrix. .NET 11 is the current release; .NET 10 remains the LTS target.

## Support matrix

| Component | Target frameworks |
| --- | --- |
| Source generators, analysers, code fixers | `netstandard2.0` |
| `Purview.SourceGeneratorFramework` | `netstandard2.0` |
| `Purview.SourceGeneratorFramework.Testing` / `.Testing.TUnit` | `netstandard2.0`, `net10.0`, `net11.0` |
| Test projects | `net10.0`, `net11.0` |
| Merge tool (packaged under `tools/`) | `net10.0`, rolls forward to `net11.0` |
| Benchmarks | `net10.0`, `net11.0` |

A .NET 8 or .NET 9 consumer still resolves the `netstandard2.0` assets of the libraries, but those
runtimes are no longer built against or tested.

## Requirements

- **Build the repository:** .NET SDK 11.0.100 or later. `global.json` pins that SDK and sets
  `allowPrerelease: false` now that .NET 11 has shipped.
- **Load a component as an analyser:** Roslyn 5.9 or later, which is the version the framework
  compiles against. The .NET 11 SDK ships Roslyn 5.12 (C# 15 / .NET 11 generation), so it satisfies
  the requirement; a Visual Studio 2026 release that ships Roslyn 5.9 or later does too.
- **Build a component that embeds the framework:** .NET 10 or later. The merge tool targets the
  .NET 10 LTS floor and sets `RollForward=LatestMajor`, so `dotnet exec` runs it on the installed
  .NET 10 or .NET 11 runtime (preferring 11 when both are present), independent of the consuming
  project's target framework.

## What the .NET 11 SDK enables for `net11.0`

`Purview.BuildSdk` 2.0.0 applies these to every project that targets `net11.0`; the repository does
not repeat them per project.

| Feature | How it is applied |
| --- | --- |
| Runtime Async (V2) | `<Features>runtime-async=on</Features>`, without `EnablePreviewFeatures` |
| C# 15 | `LangVersion=latest` — collection expression arguments, closed hierarchies, extension indexers, labelled `break`/`continue` |
| Memory safety tier 2 | `updated-memory-safety-rules` with `MemorySafetyRules=2`; tiers `0`, `1` and `2` are supported |
| .NET 11 analysis rules | `AnalysisLevel=latest`, `AnalysisMode=All`; .NET 11 fixed `latest` resolving to the .NET 9 rule set |
| Multithreaded build | MSBuild `-mt`, written into the managed `Directory.Build.rsp` |
| Warmer builds | The .NET 11 SDK enables the MSBuild server by default and matches wildcards faster during evaluation |
| Faster CLI | The SDK's `dotnet` host is NativeAOT-compiled by default (`DOTNET_CLI_ENABLEAOT=false` opts out) |

Runtime Async and the memory-safety rules only apply to the `net11.0` leg. The `netstandard2.0` and
`net10.0` legs share the C# 15 language level but none of the net11-only runtime behaviour, so shared
sources must stay free of APIs that exist only on .NET 11.

Opting out is per project: `<PurviewRuntimeAsync>false</PurviewRuntimeAsync>` (or the compiler's
`<UseRuntimeAsync>false</UseRuntimeAsync>`), and `<MemorySafetyRules>0</MemorySafetyRules>`.

## What this repository turned on

- **`--results-directory-layout per-module`** on `dotnet test` (the `just test` recipe). A
  multi-target run previously had `net10.0` and `net11.0` writing reports with the same relative file
  name; the .NET 11 option gives each test application its own directory.
- **`minPublishAgeHours="24"`** on the `nuget.org` source. The .NET 11 SDK writes this setting for
  `dotnet nuget add source --min-publish-age-hours`. It only affects version selection for
  `dotnet package add/update` and floating references, so the repository's pinned versions restore
  normally while a newly published, not-yet-reviewed version cannot be picked up by accident.
- **Benchmarks default to `net11.0`.** `dotnet run` cannot choose a target framework for a
  multi-targeted project, so the recipe now passes `--framework net11.0`; run
  `dotnet run --project src/src/SourceGeneratorFramework.Benchmarks --framework net10.0` directly
  when a `net10.0` comparison is wanted.
- **No redundant `System.Reflection.Metadata` package.** It is in the shared framework for
  `net10.0`/`net11.0`, so referencing it produced NU1510. The `netstandard2.0` library still needs the
  package reference.
- **The merge tool's target framework is a single property, and it serves both supported runtimes.**
  `$(MergeToolTFM)` selects the build output and the packed `tools/` folder, and
  `_PurviewSourceGeneratorFrameworkMergeToolTargetFramework` is the packaged default. Both are
  `net10.0`, with `RollForward=LatestMajor` in the tool's runtimeconfig; previously the projects built
  `net11.0` while looking under `net10.0`.
- **`closed` on `CodeQueryResultAssertion<T>`, on the `net11.0` asset.** The assertion chain is a fixed
  set owned by the package; the public base exists for the sealed assertions, not as a consumer
  extension point. `closed` turns that into a compile-time guarantee. `RoslynTestRunner` was the other
  candidate but cannot be closed: its derived runners are generic and C# 15 requires a derived generic
  type to reference its type parameters in a closed base (CS9383). The `netstandard2.0` and `net10.0`
  assets keep the previous `abstract` declaration, because only .NET 11 ships `IsClosedTypeAttribute`.

## Union types and closed hierarchies

Both C# 15 features were evaluated against the API surface rather than adopted wholesale. The
framework does not *use* either feature in its own shared sources, but `CodeWriter` can *emit* both:
`TypeDeclarationKind.Union` with `UnionCaseTypes` produces a `union` declaration, and
`TypeDeclarationOptions.IsClosed` produces the `closed` modifier. That lets a generator targeting C#
15 write these constructs through the structured API instead of raw text.

### Union types

No type in this repository is a true exactly-one-of today, so no union was introduced:

| Candidate | Why it is not a union |
| --- | --- |
| `GeneratorResult<T>` | Carries a value **and** diagnostics (plus blocking/error flags) at the same time. A union is exactly one case, so this would lose information and break every pipeline. |
| `AttributeDataModelProperty` / `PropertySource`, the `TypeLibrary*` models | Sealed records whose value equality is the incremental cache contract. A union adds no equality or `Deconstruct` (the language reference is explicit about this), so converting would require hand-written equality and risk cache invalidation. |
| `ComponentGuard.TryRun` | Returns `bool` plus a `Diagnostic?`. `Diagnostic` is already a reference, so there is no box to remove; a union would add API churn and a second way to express the same result. |
| `AttributeArgument` | Represents a Roslyn `TypedConstant` (an open set of kinds), not a closed set of case types. |

If a union is added later, it must use the **non-boxing access pattern** from the language
reference: a custom `[Union]` type implementing `IUnion` with a `HasValue` property and a
`TryGetValue(out TCase)` overload per case type. The `union` declaration is not suitable for
performance-sensitive value-type cases because it always stores `object?` and boxes them. The
compiler calls `TryGetValue` for type patterns and `HasValue` for null patterns, so matching never
boxes; a missing member silently falls back to the boxing `Value` path.

```csharp
using System.Runtime.CompilerServices;

[Union]
public struct IntOrError : IUnion
{
    readonly int _value;
    readonly Exception? _error;
    readonly byte _tag; // 0 = none, 1 = int, 2 = error

    public IntOrError(int value) { _value = value; _tag = 1; }
    public IntOrError(Exception error) { _error = error; _tag = 2; }

    public object? Value => _tag switch { 1 => _value, 2 => _error, _ => null };
    public bool HasValue => _tag != 0;
    public bool TryGetValue(out int value) { value = _value; return _tag == 1; }
    public bool TryGetValue(out Exception error) { error = _error!; return _tag == 2; }
}
```

### Closed hierarchies

`CodeQueryResultAssertion<T>` is declared `closed` on the `net11.0` asset. It is a public base for a
fixed set of sealed assertion types, and external derivation is now rejected with CS9382 there instead
of being an unsupported-but-possible extension. A consumer that wants its own chained assertion should
derive from TUnit's `Assertion<T>` directly. The other assets keep `abstract` so their consumers, which
may not compile with a C# 15 compiler, are unaffected.

`RoslynTestRunner` is the other infrastructure base, but C# 15 rejects a closed base whose derived
types are generic and do not reference their type parameters in it (CS9383); the concrete runners are
`CodeFixTestRunner<TAnalyzer, TCodeFix>`, `DiagnosticAnalyzerTestRunner<TAnalyzer>` and
`RefactoringTestRunner<TRefactoring>`. The documented test bases (`AnalyzerTestBase`,
`SourceGeneratorTestBase` and the TUnit variants) are consumer extension points and stay open.

## Evaluated and deliberately not enabled

| Feature | Why not |
| --- | --- |
| `dotnet test --timeout` / `--maximum-failed-tests` as defaults | The reusable workflow already bounds the job. A local default can abort a legitimate long build-integration run; pass the options per invocation instead. |
| SDK artifacts output layout (`ArtifactsPath`) | The Roslyn-version inner build sets its own `BaseOutputPath`/`BaseIntermediateOutputPath`, and pack validation asserts paths under `bin/`; moving to the artifacts layout would change both. |
| NativeAOT or ReadyToRun for the merge tool | The tool ships inside a framework-neutral package. AOT output is per-RID, and R2R would add architecture-specific binaries to `tools/`. |
| `PublishTrimmed` / `InvariantGlobalization` for the merge tool | Not proven safe for ILRepack/Mono.Cecil, which use reflection and culture-sensitive paths. |
| C# 15 runtime-dependent features in shared sources | `closed` is a declaration-only feature and is used above. Union types and net11-only APIs are not, because the `netstandard2.0`/`net10.0` legs still compile shared sources and the language-level declaration would not remove boxing. |
| .NET 11 `Process` helpers, `EqualityComparer<T>.Create`, the new `Stream` wrappers | The call sites are either multi-targeted (would need `#if NET11_0_OR_GREATER`) or on the `netstandard2.0` legs, where the APIs do not exist. No measured benefit for this codebase. |
| `NuGetAuditMode=all` | Auditing direct dependencies is the SDK default and currently reports nothing; transitive auditing can be enabled if the restore cost becomes worthwhile. |
| In-process crash reports (`DOTNET_EnableCrashReport=1`) | Useful when diagnosing a test-host crash on Linux/macOS, but it writes a report to disk, so it stays opt-in. |

## Useful .NET 11 switches

```bash
# Run tests with a whole-run limit or failure cap
dotnet test src/SourceGeneratorFramework.slnx --timeout 20m --maximum-failed-tests 5

# Select the runner for one session instead of editing global.json
DOTNET_TEST_RUNNER=Microsoft.Testing.Platform dotnet test src/SourceGeneratorFramework.slnx

# Pass environment variables without a launch profile
dotnet run -e DOTNET_TieredPGO=1 --project src/src/SourceGeneratorFramework.Benchmarks --framework net11.0

# Skip building project references when the tree is already built
dotnet test src/SourceGeneratorFramework.slnx --no-build --no-dependencies
```
