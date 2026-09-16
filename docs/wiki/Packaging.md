# Packaging

This page covers how to package a source generator that references
`Purview.SourceGeneratorFramework`, and how the framework package itself is assembled and validated.

## How the framework packages are assembled

`Purview.SourceGeneratorFramework` is dual-role: the built framework assembly ships in `lib/` so
consumers can compile generators against it, and the `analyzers/` folder carries the generator +
analyzer assemblies and their runtime dependencies. The bundled projects are:

- `SourceGeneratorFramework.Generators` — `AttributeDataModelGenerator`, `TypeLibraryGenerator`;
- `SourceGeneratorFramework.Analyzers` — the `PSGFR*` and `TLB*` analyzers;
- `SourceGeneratorFramework.CodeFixers` — the code fix providers;
- `SourceGeneratorShared` — shared models and helpers, packed into the package as
  `Purview.SourceGeneratorFramework.Shared.dll`.

These projects are `IsRoslynComponent = true` and are **not** packable on their own; they are packed
into the main package by the `SourceGeneratorFramework` project via analyzer project references
(`OutputItemType="Analyzer"`).

The repo's pack validation (`purview-build.json`) requires the `purview.sourcegeneratorframework`
package to contain, at minimum:

- `lib/netstandard2.0/Purview.SourceGeneratorFramework.dll` and
  `lib/netstandard2.0/Purview.SourceGeneratorFramework.Shared.dll`;
- `analyzers/dotnet/cs/` versions of the framework, generators, analyzers, code fixers, and shared
  assemblies;
- `build/Purview.SourceGeneratorFramework.props` and `build/Purview.SourceGeneratorFramework.targets`;
- `README.md`, `LICENSE.md`, and `purview-logo.png`.

PDBs are delivered only through the `.snupkg`; `*.pdb` files are forbidden inside the `.nupkg`.

## Referencing a generator from a consuming project

Use an analyzer project reference so Roslyn receives both the generator assembly and its framework
runtime dependency:

```xml
<ProjectReference
  Include="..\MyGenerator\MyGenerator.csproj"
  PrivateAssets="all"
  OutputItemType="Analyzer"
  ReferenceOutputAssembly="false"
/>
```

The Purview SDK automatically invokes `GetSourceGeneratorAnalyzerFiles`, which returns both the
generator and its framework dependency without adding either file to the consuming application's
runtime references. Specifying `Targets="GetSourceGeneratorAnalyzerFiles"` explicitly remains
supported but is not required.

### Generators embedded in another package

If the generator assembly is embedded in a different NuGet package, the outer package must make the
framework's compiler-visible properties visible to its consumers. Build assets from
`Purview.SourceGeneratorFramework` are not automatically copied into the outer package. Include a
`.props` file imported by the outer package that declares the property and its
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

Then centrally define:

```xml
<PropertyGroup>
  <RoslynVersion>4.8.0</RoslynVersion>
  <RoslynAnalyserVersion>5.9.0</RoslynAnalyserVersion>
</PropertyGroup>
```

The exact Roslyn baseline is a product-support decision.

## License

This documentation is part of the MIT-licensed `Purview.SourceGeneratorFramework` project.