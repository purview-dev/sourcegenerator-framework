# Agent Instructions

This repository contains custom build rules, analysers, and source generators. Agents working in this codebase should follow the guidelines below.

## Warnings and Suggestions

Do **not** suppress or mute compiler, analyser, or build warnings by adding `<NoWarn>` entries, `#pragma warning disable`, or similar directives in code or project files without explicit user direction. Warnings and suggestions are the responsibility of the developer/user to evaluate and mute. If a warning is raised, surface it to the user and let them decide whether to suppress it.

## Documentation and Samples

Any API change must update the corresponding documentation, including XML doc comments and `docs/wiki/` pages such as `docs/wiki/Code-Writer.md`. Samples (the `SourceGeneratorFramework.ExampleGenerator` reference implementation and benchmarks) must use the current best-practice APIs: the minimal-parameter `CodeWriter` overloads, structured statements (`MethodCall`, `Return`, `Assignment`, `Throw`, `Comment`, `NetConditionalReturn`) rather than raw text, and the current method names. Add or update a sample whenever an API addition or change warrants a demonstrable example, and cover it with unit tests.

## Agent skills

Authored skills live in the repository-root `skills/` folder — **not** in `.agents/skills/`, which is the
gitignored mirror that NuGet packages write into on build. The skills CLI (`npx skills`) only discovers
skills at a root `skills/` folder, and a gitignored folder is absent from a git clone, so the authored copy
has to be tracked at the root. One copy serves both routes: the git clone for the CLI, and the package for
the existing mirror.

Install them into your agent's own directories with:

```bash
npx skills add purview-dev/sourcegenerator-framework
```

### Skills shipped with a package

| Skill | Package | Declared in |
| --- | --- | --- |
| `source-generator-codewriter-modernization` | `Purview.SourceGeneratorFramework` | `src/src/SourceGeneratorFramework/SourceGeneratorFramework.csproj` |
| `source-generator-testing` | `Purview.SourceGeneratorFramework.Testing` | `src/src/SourceGeneratorFramework.Testing/SourceGeneratorFramework.Testing.csproj` |
| `tunit-test-authoring` | `Purview.SourceGeneratorFramework.Testing.TUnit` | `src/src/SourceGeneratorFramework.Testing.TUnit/SourceGeneratorFramework.Testing.TUnit.csproj` |

Packing is performed by the `IncludeRepositorySkillsInPackage` target in `src/Directory.Build.targets`. That
is an **interim shim**: `global.json` pins `Purview.BuildSdk` 1.0.5, which has no SDK-side equivalent yet. It
implements the same `PurviewSkillsToPack` property the newer SDK uses, so adopting it means setting
`PurviewPackRepositorySkills=true` on the same projects and deleting the shim.

To add a skill: create `skills/<name>/SKILL.md`, then add the name to the owning project's
`PurviewSkillsToPack`. A skill not named there is installable from git but ships in no package.
