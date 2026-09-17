---
agent: ask
description: "Ensure the Purview.BuildSdk applies analyzer/source-generator best-practice defaults to packable projects, so consumers of Purview.SourceGeneratorFramework (and any Roslyn component) get correct packaging without per-project overrides."
---

You are working on the **Purview.BuildSdk** project (the `Purview.BuildSdk` package that projects import via `<Project Sdk="Purview.BuildSdk">`). Apply this checklist to its `Sdk.props` / `Sdk.targets` so that **packable** analyzer and source-generator projects get Roslyn best-practice defaults automatically.

## Context

The `Purview.SourceGeneratorFramework` guidance requires every analyzer/generator project to set these properties or the shipped analyzer asset is broken or unoptimised. Consumers should not have to set them per project. The SDK is the right place to default them.

## Task

For projects the SDK classifies as analyzers/source generators (`IsRoslynComponent`, or projects that produce `analyzers/dotnet/cs` assets) and that are packable, ensure the following are the **defaults** (still overridable by the project):

1. `TargetFramework=netstandard2.0` unless the project explicitly overrides it.
2. `LangVersion=latest` and `Nullable=enable`.
3. `IncludeBuildOutput=false` (so the library isn't packed as a `lib/` asset) and the analyzer/generator DLL packed into `analyzers/dotnet/cs`:
   ```xml
   <None Include="$(TargetPath)" Pack="true" PackagePath="analyzers/dotnet/cs" Visible="false" />
   ```
4. `EnforceExtendedAnalyzerRules=true` and `TreatWarningsAsErrors=true`.
5. Roslyn development dependencies (`Microsoft.CodeAnalysis.*`, `Microsoft.CodeAnalysis.Analyzers`) referenced with `PrivateAssets="all"`.

## Requirements

- Do not break existing projects that already set these explicitly; the SDK defaults must be overridable.
- Handle the distinction between packable and non-packable projects — only packable analyzer/generator projects get the analyzer-asset packaging.
- Ensure the defaults apply at the right evaluation point in the SDK (props vs targets) so project files can still override with normal `PropertyGroup` values.
- Verify with a consumer project that references a generator through the SDK that the packed `.nupkg` contains `analyzers/dotnet/cs/<assembly>.dll` and the `lib/` output is empty.

## Background

See the `source-generator-codewriter-modernization` skill's "Recommended project configuration" for the canonical generator project shape this SDK should default to.