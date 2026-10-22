# Changelog

All notable changes to `Purview.SourceGeneratorFramework` and its testing packages.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/). Versions come from the
`version` field in `package.json`, which is the single source of truth: a merge to `main` publishes
that version to nuget.org.

This file was reconstructed from the conventional-commit history at 1.1.1, so entries before that
release summarise what the commits recorded rather than what was written at the time.

## [1.2.0] - 2026-10-21

### Fixed

- `TypeLibraryModelLibrary` held an `INamedTypeSymbol` in its pipeline node value and combined it with
  `CompilationProvider`. A symbol compares by reference, so the stage reported `Modified` on every new
  compilation and rebuilt its target on every keystroke. The whole target is now built inside the
  `ForAttributeWithMetadataName` transform, and the framework `PurviewTypeLibrary` shape is memoised per
  compilation through a `ConditionalWeakTable` — preserving the once-per-compilation walk without a
  pipeline node holding the compilation. The `GetFrameworkTypeLibraryTree` stage no longer exists.
- `AttributeDataModelLibrary`'s type-library spec provider returned the symbol and projected it in a
  following `Select`; the read now happens inside the transform, so the node value is the
  value-equatable `TypeLibrarySpecData`.
- `AvoidSymbolsInPipelineModelAnalyzer` (`PSGFR33`) matched banned types by exact display string, so
  `INamedTypeSymbol` never matched `ISymbol` and `CSharpCompilation` never matched `Compilation`. It now
  walks base types and implemented interfaces, which is what real pipeline code names.
- `AttributeDataModelGenerator` had three `_ => throw new InvalidOperationException(...)` arms in its emit
  helpers — exactly the `PSGFR44` shape, and reachable by a consumer only as a `CS8785` that would cost them
  every file the generator produces. All three were unreachable by validation (`ADM0008` is an error unless a
  type-argument property is a `TypeIdentity`), so each now falls through to the equivalent defined arm with a
  comment naming the validation that makes it unreachable. No behaviour changes for any defined input.
- Corrected the `Attirbutes` spelling in `GeneratorTypeLibrary` (internal).
- Removed a committed zero-byte `AttributeDataGenerationModel.cs`.

### Added

- **`ComponentGuard` / `ComponentFailure` — an escaping exception becomes a diagnostic.** Covers every
  component kind the framework supports: source generators, analyzers, code fixes, refactorings and
  diagnostic suppressors. `Run` reports the failure where there is a channel to report on (a generator's
  `SourceProductionContext`, an analyzer's `ReportDiagnostic`); `TryRun` returns whether the body succeeded
  where there is not (suppressors, code fixes — the correct behaviour there is to suppress nothing and offer
  no fix). Cancellation is never caught: an `OperationCanceledException` raised while the supplied token is
  cancelled propagates untouched, because Roslyn cancels analysis on every keystroke.

  The reported diagnostic defaults entirely from the component, because it belongs to the consumer rather
  than to the framework: the id is derived from the component's namespace (`Acme.Widgets.Generators` →
  `AWG0001`), the title from the component kind, the category from the root namespace, and the severity is
  `Warning`. Every part is overridable through `ComponentFailureOptions`, including an opt-in stack trace.
  See [`docs/wiki/Component-Failures.md`](docs/wiki/Component-Failures.md).

  The framework's own `RegisterSourceOutput` now runs the emit callback through the guard, so a generator
  built on it is covered without changing a line. Its signature is unchanged — the overrides are declared on
  the new `GenerationSettings.FailureOptions`, alongside everything else a generator says about itself.
  Adding an optional parameter to `RegisterSourceOutput` instead would have removed the four-parameter
  signature from metadata and broken every assembly already compiled against it; package validation caught
  that before it shipped.
- **`PSGFR44` — source generators must not throw.** An exception escaping a generator is reported as
  `CS8785` in every consuming build and IDE, and the generator produces no output at all for that
  compilation, so the consumer also gets every "type or namespace not found" error that follows — with a
  stack trace through the generator's internals rather than a diagnostic on their own code. The rule flags a
  `throw` anywhere inside a type implementing `IIncrementalGenerator` (the `Initialize` body, the pipeline
  lambdas, and the emit helpers hanging off it) and in any lambda handed to a pipeline method. Report a
  diagnostic instead: `GeneratorResult<T>` with a `ReportableDiagnostic`, or
  `SourceProductionContext.ReportDiagnostic`. Cancellation is unaffected, because
  `CancellationToken.ThrowIfCancellationRequested()` is a method call rather than a `throw`.
- `PSGFR33` now also inspects `IncrementalValue(s)Provider<T>` type arguments, so a transform returning a
  symbol or a tuple carrying one is reported. Previously the rule only walked the members of named model
  types, which is a shape the framework's own generators did not use — so the rule could not see them.
  Findings are collected during the compilation and reported at compilation end, because the descriptor
  carries `WellKnownDiagnosticTags.CompilationEnd` and Roslyn drops such diagnostics when they are
  reported from an operation action.
- `GeneratorCompilationRetentionTests`, which pins that a generator's pipeline nodes do not keep earlier
  compilations reachable.

### Changed

- **Breaking for test authors.** `SourceGeneratorTestRunner.RunIncremental` now builds a fresh
  `Compilation` for every run, matching what an IDE produces on every keystroke. It previously reused one
  instance across runs with identical sources, which let a stage holding a symbol report `Cached` on
  reference equality alone and hid exactly the regression these tests exist to catch. Set
  `SourceGeneratorTestOptions.ReuseCompilationForIdenticalSources` to restore the old behaviour where a
  scenario genuinely needs it. Assert the generator's own tracked stages with `StepIsCached` rather than
  `AllStepsCachedOrUnchanged`, which also covers Roslyn's internal steps.

### Documentation

- `docs/wiki/Step-Cache-Tests.md` documents the fresh-compilation default and the opt-out.

## [1.1.1]

### Fixed

- Incorrect identification of a drive result.

### Changed

- Migrated authored agent skills to the repository-root `skills/` folder and the skills CLI.

## [1.1.0]

### Added

- `PSGFR42` for undisposed test-harness results.

### Performance

- Various caching and expensive-operation fixes across the framework.
- `MetadataReference` instances shared across test compilations.

### Fixed

- The logging sink is registered for incremental runs, and entries stay ordered.
- A scoped `CodeQuery` no longer repeats matches once per tree.
- The `TypeIdentity` symbol cache is keyed on the symbol.

## [1.0.0]

### Added

- ILRepack-based merge so a component ships a self-contained analyzer and two generators cannot collide
  on assembly identity in `VBCSCompiler`.
- Expanded type library, `EnumValue`/`EnumValues` support, operator method names.

### Fixed

- A component's own internals grants survive the merge.
- Internaliser incorrectly matching tested types.
- Source-generator resolution on pack, and framework types leaking from a component.
- XML cref fixers.

### Changed

- Removed the separate `Shared` assembly; its sources are link-compiled into the framework.

[Unreleased]: https://github.com/purview-dev/sourcegenerator-framework/compare/v1.2.0...HEAD
[1.2.0]: https://github.com/purview-dev/sourcegenerator-framework/compare/v1.1.1...v1.2.0
[1.1.1]: https://github.com/purview-dev/sourcegenerator-framework/compare/v1.1.0...v1.1.1
[1.1.0]: https://github.com/purview-dev/sourcegenerator-framework/compare/v1.0.0...v1.1.0
[1.0.0]: https://github.com/purview-dev/sourcegenerator-framework/releases/tag/v1.0.0
