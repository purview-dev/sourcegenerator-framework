# Packaging

This page covers how to package a source generator that references
`Purview.SourceGeneratorFramework`, and how the framework package itself is assembled and validated.

## How the framework packages are assembled

`Purview.SourceGeneratorFramework` is dual-role: the built framework assembly ships in `lib/` so
generator projects can compile against it, and the `analyzers/` folder carries self-contained
generator, analyzer, and code-fixer assemblies. The bundled projects are:

- `SourceGeneratorFramework.Generators` — `AttributeDataModelGenerator`, `TypeLibraryGenerator`;
- `SourceGeneratorFramework.Analyzers` — the `PSGFR*` and `TLB*` analyzers;
- `SourceGeneratorFramework.CodeFixers` — the code fix providers.

Each Roslyn component has the framework implementation merged and internalized into its own assembly.
The package deliberately does **not** put `Purview.SourceGeneratorFramework.dll` under
`analyzers/dotnet/cs`. Consequently, generators built against different framework versions do not
ask Roslyn to load competing versions of a same-named runtime dependency.

The shared models and helpers that used to ship as a separate
`Purview.SourceGeneratorFramework.Shared.dll` are compiled directly into the framework assembly
(`SourceGeneratorFramework` links the `SourceGeneratorShared` sources via
`SourceGeneratorShared.Link.targets`). Consumers therefore receive a single
`Purview.SourceGeneratorFramework.dll`, which removes the version-skew hazard that a separately
bundled Shared assembly caused: generator packages that loaded a different Shared version in-process
failed with binary-incompatibility errors (e.g. removed `PurviewTypeLibrary` fields).

These projects are `IsRoslynComponent = true` and are **not** packable on their own; they are packed
into the main package by the `SourceGeneratorFramework` project. They were previously consumed as
analyzer project references, but since they now reference the framework assembly for the shared types
(which would form a project-reference cycle), the `SourceGeneratorFramework` project builds them via
`GetPurviewMergedAnalyzerFile` and packs them under `analyzers/dotnet/cs/` in
`BuildAndPackBundledAnalyzerAssemblies`.

The repo's pack validation (`purview-build.json`) requires the `purview.sourcegeneratorframework`
package to contain, at minimum:

- `lib/netstandard2.0/Purview.SourceGeneratorFramework.dll`;
- `analyzers/dotnet/cs/` versions of the self-contained generators, analyzers, and code fixers;
- `build/Purview.SourceGeneratorFramework.props` and `build/Purview.SourceGeneratorFramework.targets`;
- `tools/net10.0/` versions of the framework-owned merge tool and its runtime files;
- `README.md`, `LICENSE.md`, and `purview-logo-light.png`.

PDBs are delivered only through the `.snupkg`; `*.pdb` files are forbidden inside the `.nupkg`.

## Referencing a generator from a consuming project

Reference the framework privately from a Roslyn component. When the generator project itself is
packed, the framework's build target replaces its output with a self-contained assembly at pack time:

```xml
<PropertyGroup>
  <IsRoslynComponent>true</IsRoslynComponent>
</PropertyGroup>

<ItemGroup>
  <PackageReference
    Include="Purview.SourceGeneratorFramework"
    Version="..."
    PrivateAssets="all" />
</ItemGroup>
```

Use an analyzer project reference from a consuming project so Roslyn receives the generator
assembly:

```xml
<ProjectReference
  Include="..\MyGenerator\MyGenerator.csproj"
  PrivateAssets="all"
  OutputItemType="Analyzer"
  ReferenceOutputAssembly="false"
/>
```

The Purview SDK automatically invokes `GetSourceGeneratorAnalyzerFiles`, which returns the generator
assembly without adding it to the consuming application's runtime references. By default
(`PurviewMergeSourceGeneratorFrameworkForAnalyzerFiles=true`) the framework returns the **merged,
self-contained** generator from the intermediate `purview-merged/` directory, so consuming projects
and any GASF-based package compile against a generator that carries its own framework implementation
and never needs the loose `Purview.SourceGeneratorFramework.dll`. The generator's bin output is left
unmerged, so a project that references the generator assembly directly (an in-process test harness)
keeps its shared framework type identity, `InternalsVisibleTo` access, and avoids `CS0433`
collisions with the framework library. Specifying `Targets="GetSourceGeneratorAnalyzerFiles"`
explicitly remains supported but is not required.

Set `PurviewEmbedSourceGeneratorFramework` to `false` only for a project that produces the framework
compile-time library itself. Published generator packages must not disable embedding.

Set `PurviewMergeSourceGeneratorFrameworkForAnalyzerFiles` to `false` only when the generator's
analyzer-files consumers must keep the unmerged assembly + loose framework DLL shape (for example a
generator shipped into a single compiler process alongside an incompatible framework version).

### Analyzer consumption contract

A component that references the framework is consumed in three distinct ways, each producing a
different shape:

| Path | Trigger | Output |
| --- | --- | --- |
| `GetSourceGeneratorAnalyzerFiles` (default) | A consuming project references the component as an analyzer | The **merged**, self-contained component, returned from the content-addressed intermediate `purview-merged/<contentTag>/` directory. The component's bin output stays unmerged, so its in-process test harness retains shared framework type identity and `InternalsVisibleTo` access without `CS0433` collisions. |
| `GetSourceGeneratorAnalyzerFiles` (opt-out) | Same, with `PurviewMergeSourceGeneratorFrameworkForAnalyzerFiles=false` | Unmerged component + the loose `Purview.SourceGeneratorFramework.dll` copied from the framework package `lib/`. |
| `GetPurviewMergedAnalyzerFile` | The framework package's own bundled-component pack, or a third-party package embedding the generator | The **merged** component from the intermediate output; the component's bin is never overwritten. |
| `GetPurviewMergedAnalyzerFileForPack` (pack-time, via `TargetsForTfmSpecificContentInPackage`) | Packing a standalone, packable generator project | The **merged**, self-contained DLL (and its PDB) is contributed directly to `analyzers/dotnet/cs`. The component's bin output is never mutated, so its on-disk shape does not depend on whether `build` or `pack` ran last. |

### A component that references another component

A code-fix (or analyzer) component can reference the generator component normally
(`ProjectReference`, `ReferenceOutputAssembly` not `false`) when it needs the generator's internal
diagnostic identity. Both components are `IsRoslynComponent`; only the generator references the
framework.

- The generator component merges at build time. Its **analyzer artifact** is the merged,
  self-contained assembly from `obj/.../purview-merged/<contentTag>/`; its **bin output** stays unmerged and
  references `Purview.SourceGeneratorFramework`.
- Because a package consumer's framework `lib/` asset is not copied to output, the framework
  assembly is declared as copy-to-output content beside the generator's bin output. Content with
  `CopyToOutputDirectory` flows transitively through `ProjectReference`, so the dependent code-fix
  component's bin folder is self-sufficient too — that is what lets Visual Studio load the code-fix
  provider from its own bin without a `FileNotFoundException` for the framework assembly.
- The dependent component's **analyzer closure** (what `GetSourceGeneratorAnalyzerFiles` returns)
  includes the referenced component's analyzer artifact. The referenced component is returned as its
  own merged assembly and is **never** IL-merged into the dependent component, which would duplicate
  its types (including `InternalsVisibleTo`-visible internals) inside a second analyzer in the same
  host.
- Visual Studio's project system resolves an `OutputItemType=Analyzer` project reference to the
  referenced project's default target path — the component's **unmerged** bin assembly — and adds it
  to the compiler's analyzers, which the command-line build does not. `Purview.BuildSdk` removes that
  item before adding the resolved closure, so Roslyn only ever receives the merged artifact.
  Otherwise the unmerged copy drags `Purview.SourceGeneratorFramework` into the compiler host
  (`CS8784 FileNotFoundException: Could not load file or assembly 'Purview.SourceGeneratorFramework'`)
  and the generators are registered twice.

### Build-time analyzer-closure validation

`Purview.BuildSdk` validates the whole closure returned by `GetSourceGeneratorAnalyzerFiles`: every
file's PE `AssemblyRef` must resolve to another file in the returned set or to a compiler-host
assembly (`Microsoft.CodeAnalysis*`, `System.Composition.*`, `System.*`, `netstandard`, …). A missing
component artifact fails the build with `PRSGD0005`. Extend the permitted set with
`<PurviewAnalyzerClosurePermittedReference Include="..." />` (or the
`PurviewAnalyzerClosurePermittedReferences` property). Opt out with
`PurviewSourceGeneratorFrameworkAnalyzerValidation=false`.

### Generator-read MSBuild properties

A generator that reads `build_property.<Name>` is only correct if `<Name>` is a
`CompilerVisibleProperty` wherever the generator runs. Declare the properties a component reads:

```xml
<ItemGroup>
  <PurviewGeneratorVisibleProperty Include="MyGenerator_Disable" />
</ItemGroup>
```

`PSGF0003` fails the build when a declared property is neither a `CompilerVisibleProperty` in the
project nor declared by the project's own `Sdk/build` or `Sdk/buildTransitive` assets (which is what
consumers receive). Opt out with
`PurviewSourceGeneratorFrameworkGeneratorPropertyValidation=false`.

NuGet imports `buildTransitive` assets for **PackageReference** consumers only. While developing the
package repository itself every project uses `ProjectReference`, so
`Purview.BuildSdk`'s `ImportProjectReferencedBuildTransitiveAssets` target registers the referenced
project's `Sdk/buildTransitive/*.props|*.targets` `CompilerVisibleProperty` items for in-repo
consumers. Opt out with `PurviewImportProjectReferenceBuildTransitive=false`.

In the opt-out path the loose framework DLL is declared as a `SourceGeneratorRuntimeDependency`
(statically from the framework package `lib/` for package consumers, with a target-time fallback for
in-repo `ProjectReference` components) so the SDK copies it beside the generator before Roslyn loads
it. The merged paths never declare it.

The merge itself (`_PurviewMergeSourceGeneratorFramework`) only writes to the component's
intermediate output. `GetSourceGeneratorAnalyzerFiles` returns that result by substituting the merged
path into `TargetPathWithTargetPlatformMoniker` immediately before its body runs, leaving
`GetTargetPath` — which resolves assembly references — pointing at the unmerged bin. This is what
keeps the in-repo test harness working while shipped assemblies stay self-contained.

The merged artifact is **content-addressed**: it lives in
`$(IntermediateOutputPath)purview-merged/<contentTag>/$(TargetFileName)`, where `<contentTag>` is a
SHA-256 of the component, the framework assembly and the merge tool (the merge tool computes it via
its `--tag` mode). The tag is in the *directory*, never the file name, because ILRepack derives the
merged assembly's simple name from the output file name and an analyzer must keep
`<AssemblyName>.dll`.

That gives two properties the earlier fixed-name output could not:

- a rebuild with identical inputs reuses the existing file and **skips the merge**, keeping
  `_PurviewMergeSourceGeneratorFramework` idempotent across the per-consumer (and potentially
  parallel) invocations of `GetSourceGeneratorAnalyzerFiles`; and
- changed inputs select a **new directory**, so the merge never overwrites a merged assembly that a
  compiler host (csc/`VBCSCompiler`/Visual Studio) already has loaded. Overwriting such a file fails
  on Windows with a sharing violation, which surfaced as `MSB3073` (merge tool exit code
  `-532462766`) and left consumers loading a stale or partially written analyzer — the root cause of
  `CS8784 FileNotFoundException: Could not load file or assembly 'Purview.SourceGeneratorFramework'`.

The merge tool merges into a per-process `.staging-<pid>` directory and publishes it with a
non-overwriting directory rename; a concurrent invocation that loses the race simply observes the
published artifact instead of failing.

Packaging needs the stable `<AssemblyName>.dll` name, so the pack paths
(`GetPurviewMergedAnalyzerFile`, `GetPurviewMergedAnalyzerFileForPack`) copy the content-addressed
artifact into `$(IntermediateOutputPath)purview-pack/` and pack that plainly named staging copy.

### Framework type internalization in merged components

ILRepack's `Internalize` is best-effort: a framework type that reaches the merged component's public
API surface stays public, and the types the framework's own generators emit into the component (the
`Purview.SourceGeneratorFramework.Generators` attribute set, generated type libraries, marker
attributes) are not part of the merged framework assembly at all, so ILRepack never sees them. Either
gap leaks framework types out of what must be a self-contained analyzer, and any project that loads
that analyzer alongside the real framework assembly fails with `CS0433` ambiguity for every leaked
type.

The merge tool therefore runs a deterministic internalization pass over the merged output
(`FrameworkTypeInternalizer`) after `ILRepack` finishes:

- every type in a framework-owned namespace (`Purview.SourceGeneratorFramework` and its children,
  including the generated `Generators` attribute set) becomes non-public;
- `Microsoft.CodeAnalysis.EmbeddedAttribute` (the framework-emitted marker) becomes non-public;
- **Roslyn component entry points are never internalized** — a generator, analyzer, code fix
  provider, or refactoring provider that is reachable from the framework namespace stays public,
  because Roslyn only instantiates public components (PSGFR27). This is what keeps the framework's
  own bundled analyzers working after the pass;
- the pass reports the merge result: leftover public framework types fail the merge (exit code `5`),
  and public component members whose signature exposes a framework type are logged as warnings so the
  component author can make them (or their declaring type) non-public.

The component's own generated types (the type library, attribute data models) keep their accessibility
in the component assembly: TLB0015 requires a hand-written partial to be declared
`public static partial` so it can merge with the generated library, and in-repo consumers such as code
fixers and sibling assemblies compile against it. Self-containment is therefore enforced at the merge
boundary rather than by rewriting generated accessibility. See [Type-Library.md](Type-Library.md).

> A merged component is an analyzer artifact and must never be referenced as a compile-time
> dependency. Tests that need to run a *packaged* generator load it out of band — see
> [Testing-TUnit.md](Testing-TUnit.md).


The bundled `SelfContainedGeneratorAnalyzer` (PSGFR39) runs on every project that references the
framework and errors when a **non-packable** Roslyn component explicitly opts out of the default
self-contained analyzer output. Such a component, if embedded into a package through the GASF-based
pack, forces the loose `Purview.SourceGeneratorFramework.dll` under `analyzers/`, reintroducing the
shared-version hazard.

The analyzer reads the following compiler-visible properties:
`IsRoslynComponent`, `IsPackable`, `PurviewEmbedSourceGeneratorFramework`,
`PurviewMergeSourceGeneratorFrameworkForAnalyzerFiles`, and
`PurviewSourceGeneratorFrameworkAnalyzerValidation`. It does not report when the component:

- is not a Roslyn component, or does not reference the framework;
- disables embedding (`PurviewEmbedSourceGeneratorFramework=false`);
- is packable (`IsPackable=true`) — its own `GenerateNuspec` merge makes the package self-contained;
- keeps the default merged GASF output (`PurviewMergeSourceGeneratorFrameworkForAnalyzerFiles=true`);
- explicitly opts out (`PurviewSourceGeneratorFrameworkAnalyzerValidation=false`).

Set `PurviewSourceGeneratorFrameworkAnalyzerValidation=false` on a component that is shipped
self-contained via `GetPurviewMergedAnalyzerFile`, or that is only consumed in-repo and never packed.
Packaging an embedded generator through the raw GASF path without one of the self-contained
arrangements is an error.

### `IsExternalInit` contract

The framework assembly defines `System.Runtime.CompilerServices.IsExternalInit` **publicly** so the
framework's own bundled generators can emit `init`-based attribute types into any consumer
compilation, and so the merge step has a single marker definition to internalize. Consumers
(generator projects) should not declare their own `IsExternalInit`.

A generator-local marker gives calls to the framework's `init` setters a different required custom
modifier identity from the setter definitions. Older merge-tool versions passed both identities to
ILRepack, which could emit `Method reference is used with definition return type / parameter`
warnings while rewriting the component.

For compatibility with generators that still receive a local marker from legacy source or build
tooling, the merge tool normalizes those required modifiers to the framework marker in a temporary
copy before merging. The generator's bin output is not changed, and the shipped self-contained
analyzer contains one internalized `IsExternalInit` definition. Removing the redundant marker from
the generator project remains the preferred configuration.

### Generators embedded in another package

If the generator assembly is embedded in a different NuGet package, pack the generator's **merged,
self-contained** assembly so the outer package does not ship a loose `Purview.SourceGeneratorFramework.dll`.
Call the framework's `GetPurviewMergedAnalyzerFile` target on the generator project (which merges the
framework implementation into the generator's intermediate output without touching its bin) and add
the returned file under `analyzers/dotnet/cs`, disabling the SDK's default GASF-based analyzer packing:

```xml
<PropertyGroup>
  <PackProjectReferencedSourceGenerators>false</PackProjectReferencedSourceGenerators>
  <TargetsForTfmSpecificContentInPackage>$(TargetsForTfmSpecificContentInPackage);PackMyGenerator</TargetsForTfmSpecificContentInPackage>
</PropertyGroup>

<Target Name="PackMyGenerator">
  <MSBuild
    Projects="../MyGenerator/MyGenerator.csproj"
    Targets="GetPurviewMergedAnalyzerFile"
    Properties="Configuration=$(Configuration)"
    RemoveProperties="TargetFramework;TargetFrameworks;RuntimeIdentifier;SelfContained"
  >
    <Output TaskParameter="TargetOutputs" ItemName="_MyGeneratorMerged" />
  </MSBuild>
  <ItemGroup>
    <TfmSpecificPackageFile Include="@(_MyGeneratorMerged)">
      <PackagePath>analyzers/dotnet/cs/</PackagePath>
    </TfmSpecificPackageFile>
  </ItemGroup>
</Target>
```

If the generator assembly is also embedded at compile time for the outer package's consumers, the
outer package must make the framework's compiler-visible properties visible to those consumers. Build
assets from `Purview.SourceGeneratorFramework` are not automatically copied into the outer package.
Include a `.props` file imported by the outer package that declares the property and its
`CompilerVisibleProperty` entry (see
[Code-Writer.md](Code-Writer.md#generators-embedded-in-another-package)), and pack it using the outer
package's ID so NuGet imports it automatically:

```xml
<None
  Include="Sdk\Sdk.props"
  Pack="true"
  PackagePath="buildTransitive\$(PackageId).props"
  Visible="false"
/>
```

## Roslyn version compatibility

The most important packaging rule is:

> **The version of `Microsoft.CodeAnalysis.*` used to compile your analyzer/generator establishes a
> minimum compiler-host API requirement.**

The consumer's `<TargetFramework>` does not determine analyzer compatibility. Analyzer/generator code
executes inside a compiler/IDE host. Microsoft's published baseline for the framework's Roslyn
generation is:

| Roslyn package | Minimum Visual Studio | Language / .NET generation |
| ---: | --- | --- |
| 4.8 | VS 2022 17.8 | C# 12 / .NET 8 |
| 4.12 | VS 2022 17.12 | C# 13 / .NET 9 |
| 5.0 | VS 2026 18.0 | C# 14 / .NET 10 |

> **This framework is built against Roslyn 5.0.** The generator, analyzer, and testing assemblies in
> `Purview.SourceGeneratorFramework*` are compiled against `Microsoft.CodeAnalysis` 5.x, so compiler
> hosts that load them must be Roslyn 5.0 or later (`.NET 10` SDK / Visual Studio 2026 18.0). The
> testing packages multi-target `net8.0`–`net10.0`; Roslyn 5.x ships `net8.0`/`net9.0` package assets,
> so those test targets still load the test runner.

See [Guide.md](Guide.md) sections 14–18 for the full discussion of Roslyn versioning, multi-version
packaging strategies, and the recommended generator project configuration.

## Recommended generator project configuration

A broadly-compatible generator project might start with:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>

    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>

    <IsPackable>true</IsPackable>
    <IncludeBuildOutput>false</IncludeBuildOutput>

    <EnforceExtendedAnalyzerRules>true</EnforceExtendedAnalyzerRules>

    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>

  <ItemGroup>

    <PackageReference
        Include="Microsoft.CodeAnalysis.CSharp"
        Version="$(RoslynVersion)"
        PrivateAssets="all" />

    <PackageReference
        Include="Microsoft.CodeAnalysis.Analyzers"
        Version="$(RoslynAnalyserVersion)"
        PrivateAssets="all" />

    <PackageReference
        Include="Purview.SourceGeneratorFramework"
        Version="$(SourceGeneratorFrameworkVersion)"
        PrivateAssets="all" />

  </ItemGroup>

  <ItemGroup>

    <None
        Include="$(TargetPath)"
        Pack="true"
        PackagePath="analysers/dotnet/cs"
        Visible="false" />

  </ItemGroup>

</Project>
```

The resulting generator package contains the generator DLL under `analyzers/dotnet/cs`; it does not
contain a loose `Purview.SourceGeneratorFramework.dll`. Package validation should inspect both the
ZIP entries and the generator's assembly references to enforce that invariant.

Then centrally define:

```xml
<PropertyGroup>
  <RoslynVersion>4.8.0</RoslynVersion>
  <RoslynAnalyserVersion>5.9.0</RoslynAnalyserVersion>
</PropertyGroup>
```

The exact Roslyn baseline is a product-support decision.

## Release gates

The following checks are the acceptance criteria for the self-contained packaging:

1. **No assembly reference and no public framework types** — every shipped Roslyn component DLL
   (`analyzers/dotnet/cs/*.dll`) has no assembly reference to `Purview.SourceGeneratorFramework` and
   exposes **no public type** in a framework-owned namespace (`Purview.SourceGeneratorFramework` and
   its children, including the generated `Generators` attribute set, plus the framework-emitted
   `Microsoft.CodeAnalysis.EmbeddedAttribute`). The only exception is the component's own Roslyn
   component entry points, which must stay public because Roslyn only instantiates public components.
   Inspect the metadata directly; do not rely on "the sample compiled". The merge tool fails with exit
   code `5` when a merge leaves public framework types behind, and logs a warning naming any public
   member that still exposes a framework type.
2. **No loose framework DLL in packages** — no `.nupkg` contains
   `Purview.SourceGeneratorFramework.dll` under `analyzers/`, and `*.pdb` files are forbidden in the
   `.nupkg` (symbols ship only through the `.snupkg`).
3. **Merged entry points survive** — each merged DLL still exposes its `IIncrementalGenerator`,
   `DiagnosticAnalyzer`, or `CodeFixProvider` implementations.
4. **Installed-package consumer test** — a generator project that references the framework package
   by `PackageReference` (with the framework's `build/` targets auto-imported) packs a single
   self-contained DLL under `analyzers/dotnet/cs`, and a consumer that installs that package builds
   with the generator producing output.
5. **Two-version coexistence test** — build Generator A against the current framework and
   Generator B against an intentionally binary-incompatible framework version (for example a v2
   that adds a `CodeWriter` member Generator B calls). Install both packages into one consumer and
   build with both package-reference orders. Both generators must run, each against its own embedded
   framework copy. Under the old shared-DLL model one generator fails with `MissingMethodException`
   (load-order dependent); with self-contained packaging both succeed.

## License

This documentation is part of the MIT-licensed `Purview.SourceGeneratorFramework` project.
