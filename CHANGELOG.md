# Changelog

All notable changes to `Purview.SourceGeneratorFramework` and its testing packages.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/). Versions come from the
`version` field in `package.json`, which is the single source of truth: a merge to `main` publishes
that version to nuget.org.

This file was reconstructed from the conventional-commit history at 1.1.1, so entries before that
release summarise what the commits recorded rather than what was written at the time.

## [Unreleased]

### Added

- **C# 15 construct support in `CodeWriter`.** `TypeDeclarationKind.Union` with
  `TypeDeclarationOptions.UnionCaseTypes`, plus `Union`/`UnionScope` overloads, emits a
  `union Name(CaseTypes)` declaration (semicolon-terminated or with a body). `IsClosed` emits the
  `closed` modifier on a class or record class, suppressing the default `sealed`; `closed` combines
  with `partial` and rejects `static`/`abstract`/non-class declarations. Reopening a containing type
  with `TypeHelpers.CreatePartialTypeDeclarationOptions` detects the compiler's
  `IsClosedTypeAttribute` and preserves `closed` instead of emitting `abstract`.
- Code fixes for the structured-`CodeWriter` suggestions: single-line statements now rewrite to
  `Return`, `Throw`, `MethodCall`, `MethodCallOn`, `AwaitedMethodCall`, `AwaitedMethodCallOn`,
  `Assignment`, `Comment`, `Using`, or `UsingAlias` (`PSGFR19`); a bare `*DeclarationOptions`
  construction moves to the minimal overload (`PSGFR20`); and `#pragma warning disable` rewrites to
  `PragmaDisable` (`PSGFR22`).

### Changed

- **The structured-API suggestions are warnings.** `PSGFR16` and `PSGFR18`–`PSGFR23`, which recommend a
  structured `CodeWriter` API (for example `Return` instead of `Line("return value;")`), and `PSGFR34`
  for extension blocks, are now `Warning` rather than `Info`. A build that treats warnings as errors
  now fails until the suggestion is adopted or explicitly suppressed. Informational findings such as a
  duplicate enum value stay `Info`.

## [2.0.0] - 2026-11-10

### Changed

- **.NET 8 and .NET 9 are out of support.** The libraries still ship a `netstandard2.0` asset, so a
  .NET 8 or .NET 9 consumer keeps working, but neither runtime is built against or tested. The test
  matrix is `net10.0` and `net11.0`, and the benchmarks target the same pair. See
  [docs/wiki/platform/DotNet-11.md](docs/wiki/platform/DotNet-11.md).
- **The repository requires the .NET 11 SDK** (`11.0.100`, with `allowPrerelease` now `false` because
  .NET 11 has shipped) and `Purview.BuildSdk` 2.0.0.
- **The packaged merge tool supports .NET 10 and .NET 11 from one `tools/` folder.** It targets the
  .NET 10 LTS floor with `RollForward=LatestMajor`, so `dotnet exec` uses the installed .NET 10 or
  .NET 11 runtime and prefers 11 when both are present. `MergeToolTFM` selects both the build output
  and the packed folder, and the merge-tool test suite runs on both runtimes.
- **Roslyn is pinned to `Microsoft.CodeAnalysis` 5.9.0** instead of a floating `5.9.0+` range. A host
  that loads the generators must ship Roslyn 5.9 or later; the .NET 11 SDK ships 5.12.
- **`CodeQueryResultAssertion<T>` is `closed` on the `net11.0` asset (C# 15).** It is the public base
  for the package's sealed assertion chain, not a consumer extension point, so deriving from it in
  another assembly now fails with CS9382 there. Write a custom chained assertion against TUnit's
  `Assertion<T>` instead. The `netstandard2.0` and `net10.0` assets keep the previous `abstract`
  declaration, because the compiler attribute the modifier emits ships only with .NET 11.

### Added

- [docs/wiki/platform/DotNet-11.md](docs/wiki/platform/DotNet-11.md): the support matrix, the .NET 11 features that
  `Purview.BuildSdk` enables for `net11.0` (Runtime Async, C# 15, memory-safety rules, `-mt`), and the
  features that were evaluated and deliberately left off. It records why no union type was required
  and the non-boxing `IUnion`/`HasValue`/`TryGetValue` pattern to use if one is added.
- `dotnet test --results-directory-layout per-module` in the `just test` recipe. A multi-target run
  previously had `net10.0` and `net11.0` overwrite each other's reports.
- `minPublishAgeHours="24"` on the `nuget.org` source, the .NET 11 SDK setting that keeps a
  just-published package out of automatic version selection until it has had a day of review.

### Fixed

- The merge tool's target framework disagreed with itself: the projects built `net11.0` but looked
  for the tool under `bin/<Configuration>/net10.0`, and the packaged targets looked under
  `tools/net10.0`. Both now flow from a single property.
- Removed the redundant `System.Reflection.Metadata` package reference from the build-integration
  tests, clearing NU1510 (the assembly is in the shared framework for `net10.0`/`net11.0`).
- `just benchmark` now passes `--framework net11.0`; `dotnet run` cannot choose a target framework for
  a multi-targeted project.

## [1.3.0] - 2026-11-03

### Changed

- A bare `[Argument]` on an attribute-data model parameter now reads the constructor parameter named after
  the record parameter, camel-cased (`MessageTemplate` → `messageTemplate`), as `ArgumentAttribute` has
  always documented. It previously added no constructor source at all, so the property fell back to reading
  a *named* argument of the same name. A model relying on that should use `[Property]` instead; the naming
  analyzer reports a bare `[Argument]` whose implied parameter does not exist (`ADM0012`).

### Fixed

- `AttributeDataModelGenerator` emitted a local variable named after each property, camel-cased, without
  escaping it. A property whose camel-cased name is a C# keyword (`Event`, `Class`, `Default`) produced
  `var event = …` and did not compile, and a property named `AttributeData` shadowed the parse method's
  parameter. Keyword names are now emitted with `@`, and `AttributeData` gets a distinct local.
- The example generator's `LogAttributeData` mapped `Message` with `[Property]`, but `LogAttribute.Message`
  has a private setter, so the message only ever arrives through the constructor and `[Log("text")]` lost
  it. It now reads the `message` constructor parameter.
- The example generator's mirror of the emitted `GenerateServiceAttribute` declared `Name` with an
  `internal init` setter, unlike the emitted attribute's public setter. Found by `ADM0012`.

- `AttributeDataModelGenerator` populated a `[NestedModel]` parameter by calling the nested model's
  public `FromAttributeData(AttributeData)`, which begins with its own `TargetAttribute` check. When the
  outer attribute was not the nested model's target (directly or through `MatchByInheritance`), the check
  always failed and the nested model was always `Empty`: every value was `default(T)` and no
  `[Property]`/`[Argument]` `DefaultValue` was applied. A level-shortcut attribute such as `[Info("…")]`
  nesting the model of a general `[Log]` attribute lost its message template, event ID and name. Every
  generated model now has a `FromAttributeDataUnchecked(AttributeData)` method holding the argument
  reading without the target check, and nested models call it. The nested model is read from the outer
  attribute's constructor and named arguments, `Exists` is `true`, and a mapped argument the outer
  attribute does not declare takes its `DefaultValue`. The guarded `FromAttributeData` and
  `TryFromAttributeData` overloads keep their check and their behaviour; they now delegate to
  `FromAttributeDataUnchecked` after it.

### Added

- **`AttributeDataModelNamingAnalyzer` (`ADM0011`–`ADM0015`).** The generated code reads names
  case-sensitively and falls back to a default when a name is missing, so a mapping mistake compiled
  cleanly and silently produced default values. The analyzer mirrors the generator's source resolution
  and reports: a mapped name that matches an attribute member only by case (`ADM0011`, warning); a named
  source that is not a public settable member, or a bare `[Argument]` whose implied parameter does not
  exist (`ADM0012`, warning); a `[NestedModel]` property that reads only names the outer attribute lacks
  (`ADM0013`, info); a property named like a generated member, for which generation is now skipped
  (`ADM0014`, error); and a nested model reading a constructor index that names a different parameter on
  the outer attribute (`ADM0015`, warning). `ADM0003` is unchanged.
- **Attribute testing helpers in `Purview.SourceGeneratorFramework.Testing`.** `AttributeDataFactory`
  compiles an attribute application (`FromUsage`, placed on a declaration its `AttributeUsage` allows) or
  a source (`FromSource`) into real `AttributeData`, throwing on compilation errors. `AttributeArguments`
  describes each argument as an `AttributeArgument` — name, ordinal, value, and `AttributeArgumentForm`
  (`Positional`, `NamedParameter`, `Defaulted`, `NamedArgument`) — with case-sensitive lookups and
  value comparison across numeric types, enum members, `typeof` arguments and arrays.
- **`AttributeDataAssertions` in `Purview.SourceGeneratorFramework.Testing.TUnit`:**
  `HasConstructorArgument` by ordinal or parameter name (optionally with the expected form),
  `DoesNotHaveConstructorArgument`, `HasNamedArgument` and `DoesNotHaveNamedArgument`. A failure lists
  every argument of the application.
- The `purview-sourcegenerator-attribute-data-models` skill: mapping methodology, nesting, the `ADM`
  rules and how to test a model. The diagnostics, testing and TUnit skills cover the new rules and helpers.
- `LogEventAttribute` / `InfoEventAttribute` and their models in the example generator: a level-shortcut
  attribute that does not derive from the attribute its nested model targets, with tests that use
  `AttributeDataFactory` and the attribute-argument assertions against the generated models.

## [1.2.2] - 2026-10-27

### Fixed

- `Purview.SourceGeneratorFramework.dll` leaked into application output. The build targets declared
  the framework assembly as copy-to-output content so a dependent component's bin folder could load
  the unmerged generator, but copy-to-output content flows transitively through every
  `ProjectReference` — including `OutputItemType="Analyzer"` / `ReferenceOutputAssembly="false"`
  ones. A library that referenced a generator project as an analyzer, and every application that
  referenced that library by project, received the compiler-host DLL in its build output,
  `deps.json`, publish output, single-file bundle and Blazor WebAssembly `_framework` folder. Native
  AOT and trimmed publishes shipped it, and the WebAssembly AOT compiler failed with "Could not load
  Microsoft.CodeAnalysis". `DeclarePurviewFrameworkOutputCopy` now copies the assembly into the
  component's own output with a plain `Copy` (recorded in `FileWrites`); a dependent component or
  test project that references the component as an assembly still receives it beside its own output
  through assembly-reference resolution, and an analyzer-only reference never does. Package consumers
  were not affected: the package ships only the merged analyzer, which is unchanged.

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
  See [`docs/wiki/authoring/Component-Failures.md`](docs/wiki/authoring/Component-Failures.md).

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

- `docs/wiki/testing/Step-Cache-Tests.md` documents the fresh-compilation default and the opt-out.

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
[1.3.0]: https://github.com/purview-dev/sourcegenerator-framework/compare/v1.2.2...v1.3.0
[1.2.0]: https://github.com/purview-dev/sourcegenerator-framework/compare/v1.1.1...v1.2.0
[1.1.1]: https://github.com/purview-dev/sourcegenerator-framework/compare/v1.1.0...v1.1.1
[1.1.0]: https://github.com/purview-dev/sourcegenerator-framework/compare/v1.0.0...v1.1.0
[1.0.0]: https://github.com/purview-dev/sourcegenerator-framework/releases/tag/v1.0.0
