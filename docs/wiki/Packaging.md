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
assembly without adding it to the consuming application's runtime references. The framework returns
the generator unmerged together with the loose `Purview.SourceGeneratorFramework.dll` runtime
dependency so the compiler can load both, and so the generator's own in-process test harness keeps
its shared framework type identity. Specifying `Targets="GetSourceGeneratorAnalyzerFiles"` explicitly
remains supported but is not required.

Set `PurviewEmbedSourceGeneratorFramework` to `false` only for a project that produces the framework
compile-time library itself. Published generator packages must not disable embedding.

Set `PurviewMergeSourceGeneratorFrameworkForAnalyzerFiles` to `true` to opt into a self-contained
**merged** generator from `GetSourceGeneratorAnalyzerFiles` instead of the unmerged assembly + loose
framework DLL. This also keeps GASF-based packages self-contained. Leave it unset (`false`) when the
generator's tests reference the generator assembly directly and rely on `InternalsVisibleTo` grants
or shared framework type identity.

### Analyzer consumption contract

A component that references the framework is consumed in three distinct ways, each producing a
different shape:

| Path | Trigger | Output |
| --- | --- | --- |
| `GetSourceGeneratorAnalyzerFiles` (default) | A consuming project references the component as an analyzer | Unmerged component + the loose `Purview.SourceGeneratorFramework.dll` copied from the framework package `lib/`. Keeps the component's bin unmerged, so its in-process test harness retains shared framework type identity and `InternalsVisibleTo` access. |
| `GetSourceGeneratorAnalyzerFiles` (opt-in) | Same, with `PurviewMergeSourceGeneratorFrameworkForAnalyzerFiles=true` | A **merged**, self-contained component. No loose framework DLL is needed. |
| `GetPurviewMergedAnalyzerFile` | The framework package's own bundled-component pack, or a third-party package embedding the generator | The **merged** component from the intermediate output; the component's bin is never overwritten. |
| `GenerateNuspec` (`EmbedPurviewSourceGeneratorFrameworkForPack`) | Packing a standalone, packable generator project | The generator's bin is replaced by the **merged** self-contained DLL and the loose framework DLL is deleted before the package is written. |

In every path the loose framework DLL is declared as a `SourceGeneratorRuntimeDependency` (statically
from the framework package `lib/` for package consumers, with a target-time fallback for in-repo
`ProjectReference` components) so the SDK copies it beside the generator before Roslyn loads it.

The merge itself (`_PurviewMergeSourceGeneratorFramework`) only writes to the component's
intermediate `purview-merged/` directory; the pack/analyzer targets copy that result where it is
needed. This is what keeps the in-repo test harness working while shipped assemblies stay
self-contained.

### `IsExternalInit` contract

The framework assembly defines `System.Runtime.CompilerServices.IsExternalInit` **publicly** so the
framework's own bundled generators can emit `init`-based attribute types into any consumer
compilation, and so the merge step has a single marker definition to internalize. Consumers
(generator projects) must **not** declare their own `IsExternalInit`: doing so produces a duplicate
type definition against the framework reference.

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

1. **No assembly reference** — every shipped Roslyn component DLL
   (`analyzers/dotnet/cs/*.dll`) has no assembly reference to `Purview.SourceGeneratorFramework`.
   Inspect the metadata directly; do not rely on "the sample compiled".
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
