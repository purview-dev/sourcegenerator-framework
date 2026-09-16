# SourceGenerator Framework Wiki

This wiki is the project documentation hub for **Purview.SourceGeneratorFramework** — a strongly typed
framework for building, testing, and maintaining incremental C# source generators with Roslyn. It
includes structured code generation (`CodeWriter`), incremental pipeline helpers, attribute data
models, type libraries, a step-cache test runner for verifying incremental behaviour, and bundled
analysers that guide generators back to best practice.

## Start here

- [Getting Started](Getting-Started.md)
- [Source Generator & Analyser Best Practices](Guide.md)
- [CodeWriter structured API reference](Code-Writer.md)
- [TypeLibraryGenerator](Type-Library.md)
- [Attribute Data Models](Attribute-Data-Models.md)
- [Incremental Pipeline](Incremental-Pipeline.md)
- [Analyzers](Analyzers.md)
- [Testing](Testing.md)
- [Testing with TUnit](Testing-TUnit.md)
- [Step-Cache Tests](Step-Cache-Tests.md)
- [Packaging](Packaging.md)
- [Performance](Performance.md)
- [Release Flow](Release-Flow.md)

## Packages

| Package | Description | Packable |
| --- | --- | --- |
| [`Purview.SourceGeneratorFramework`](../../src/src/SourceGeneratorFramework) | Core helpers, models, and MSBuild integration for writing incremental source generators. | Yes |
| [`Purview.SourceGeneratorFramework.Testing`](../../src/src/SourceGeneratorFramework.Testing) | Framework-agnostic test runner and assertions for source generator unit tests. | Yes |
| [`Purview.SourceGeneratorFramework.Testing.TUnit`](../../src/src/SourceGeneratorFramework.Testing.TUnit) | TUnit-specific test base classes and assertions for source generator tests. | Yes |
| [`Purview.SourceGeneratorFramework.Generators`](../../src/src/SourceGeneratorFramework.Generators) | Internal Roslyn source generator used by the framework package. | No |
| [`Purview.SourceGeneratorFramework.ExampleGenerator`](../../src/src/SourceGeneratorFramework.ExampleGenerator) | Reference implementation showing how to build a generator with the framework. | No |

## Feature highlights

- **`CodeWriter`** — an allocation-conscious writer for generated C# source with indentation,
  namespace/type declarations, structured statements, XML documentation, conditional compilation
  blocks, and deterministic output. Declarations and statements are structured values rather than raw
  text; see [Code-Writer.md](Code-Writer.md).
- **`IncrementalPipeline`** — extension methods for composing `IncrementalValueProvider<T>` /
  `IncrementalValuesProvider<T>` pipelines, including attribute-based discovery, generation-context
  creation, and disable-property checks; see [Incremental-Pipeline.md](Incremental-Pipeline.md).
- **`GenerationContext`** — a base execution-services context carrying the Roslyn `Compilation`,
  immutable generator settings, optional logging, and a factory for independently owned `CodeWriter`
  instances.
- **`GeneratorResult<T>`** — a value-or-diagnostics result type for incremental transforms, with
  explicit per-diagnostic `IsBlocking` control over whether generation continues.
- **`AttributeDataModelGenerator`** — bundled generator that emits `readonly record struct` attribute
  parser models from `[Generate]` declarations; see [Attribute-Data-Models.md](Attribute-Data-Models.md).
- **`TypeLibraryGenerator`** — generates a self-contained `public static partial` type library from a
  small declarative spec; see [Type-Library.md](Type-Library.md).
- **Bundled analysers and code fixes** — `PSGFR11`–`PSGFR38` diagnostics for Roslyn best practice,
  plus code fixes; see [Analyzers.md](Analyzers.md).
- **Testing framework** — `SourceGeneratorTestRunner<TGenerator>`, `CodeQuery` syntax-node
  assertions, refactoring tests, and incremental cache tests; see [Testing.md](Testing.md) and
  [Testing-TUnit.md](Testing-TUnit.md).
- **Step-cache tests** — prove a generator caches correctly stage-by-stage; see
  [Step-Cache-Tests.md](Step-Cache-Tests.md).

## Requirements

- .NET SDK 10.0 or later to build the framework.
- The framework is built against Roslyn 5.0 (`Microsoft.CodeAnalysis` 5.x), so compiler hosts that
  load the generator, analyser, and testing assemblies must be Roslyn 5.0 or later (`.NET 10` SDK /
  Visual Studio 2026 18.0).
- Source generators target `netstandard2.0`; test projects target `net8.0`, `net9.0`, and `net10.0`.

## Repository layout

- `src/src/SourceGeneratorFramework` — core framework package.
- `src/src/SourceGeneratorFramework.Generators` — bundled generators (attribute data models, type
  library), shipped inside the core package.
- `src/src/SourceGeneratorFramework.Analyzers` — bundled analysers.
- `src/src/SourceGeneratorFramework.CodeFixers` — bundled code fix providers.
- `src/src/SourceGeneratorFramework.Testing` — framework-agnostic testing package.
- `src/src/SourceGeneratorFramework.Testing.TUnit` — TUnit testing integration.
- `src/src/SourceGeneratorFramework.ExampleGenerator` — reference generator implementation.
- `src/src/SourceGeneratorFramework.Benchmarks` — BenchmarkDotNet benchmarks (see
  [Performance.md](Performance.md)).