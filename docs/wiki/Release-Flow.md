# Release Flow

This page documents how the repository builds, tests, packs, and releases
`Purview.SourceGeneratorFramework`.

## Versioning

The current version lives in the repository-root `package.json`:

```json
{
  "name": "purview-sourcegenerator-framework",
  "version": "1.0.0-prerelease.42"
}
```

The version is read by the build tooling (for example `just version` runs
`bun -p "require('./package.json').version"`), and GitHub releases are tagged `v<version>`, e.g.
`v1.0.0-prerelease.42`.

## Workflows

### Pull requests

`.github/workflows/pr.yml` runs on `pull_request` to `main`. It calls the shared
`purview-dev/build` workflow (`purview-build.yml`) with `run-pack: true` and `validate-pack: true`, so
every PR restores, builds, lints, runs tests, packs, and validates the packages.

### Releases

`.github/workflows/release.yml` runs on `push` to `main`. It calls the shared
`purview-dev/build` workflow (`purview-release.yml`) with `release-mode: NuGet`, which builds, tests,
packs, validates, publishes to NuGet, and creates the GitHub release.

## Local pipelines

The `Justfile` wraps the shared `Purview.Build` pipeline (installed as a pinned dotnet tool to
`.tools/purview-build/purview-build`):

| Recipe | Pipeline mode | Purpose |
| --- | --- | --- |
| `just pipeline-pr` | default | Restore, build, lint, tests. |
| `just pipeline-build` | `--Build:RunTests=false --Release:Mode=None` | Build-only pipeline. |
| `just pipeline-tests` | `--Build:RunTests=true --Release:Mode=None` | Build with tests. |
| `just pipeline-release` | `--Release:Mode=NuGet` | Full release: build, test, pack, publish. |
| `just pipeline-local-release` | `--Release:Mode=LocalNuGet` | Build, test, pack, and publish to a local NuGet feed. |

Convenience recipes also exist for building (`just build`), testing (`just test`, `just test-unit`),
packing (`just pack`), benchmarking (`just benchmark`), linting (`just lint-check`/`just lint-fix`),
and cleaning (`just clean`, `just scrub`).

## Pack validation

`purview-build.json` configures pack validation:

- `PackValidation.RequireSymbolPackage` and `RequireSymbolFiles` — every packable package must ship a
  `.snupkg` with symbol files.
- `PackValidation.RequiredContent` — each package must contain its declared assets. For example,
  `purview.sourcegeneratorframework` must contain the `lib/netstandard2.0/` framework assembly and
  shared assembly, the `analyzers/dotnet/cs/` generator/analyzer/code-fixer/shared assemblies, the
  `build/Purview.SourceGeneratorFramework.props` and `.targets` files, `README.md`, `LICENSE.md`, and
  `purview-logo.png`. See [Packaging.md](Packaging.md) for details.
- `PackValidation.ForbiddenContent` — `*.pdb` files are forbidden inside the `.nupkg` (PDBs are
  delivered only through the `.snupkg`).

## Dependency management

`Directory.Packages.props` centralises package versions:

- `Microsoft.CodeAnalysis.CSharp` / `Microsoft.CodeAnalysis.CSharp.Workspaces` — Roslyn 5.x
  (`RoslynCompilerVersion`, currently `[5.9.0,)`).
- `Microsoft.CodeAnalysis.Analyzers` — `RoslynAnalyzersVersion` `[5.9.0,)`.
- `TUnit` / `TUnit.Core` / `TUnit.Assertions` / `TUnit.Mocks` — `[1.67.0,)`.
- `System.Reflection.MetadataLoadContext` — used by the testing package for the metadata-only
  `CompilationResult` view.

Central Package Management is enabled with `CentralPackageTransitivePinningEnabled`.

## License

This documentation is part of the MIT-licensed `Purview.SourceGeneratorFramework` project.