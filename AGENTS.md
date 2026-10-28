# Agent Instructions

This repository contains custom build rules, analysers, and source generators. Agents working in this codebase should follow the guidelines below.

## Warnings and Suggestions

Do **not** suppress or mute compiler, analyser, or build warnings by adding `<NoWarn>` entries, `#pragma warning disable`, or similar directives in code or project files without explicit user direction. Warnings and suggestions are the responsibility of the developer/user to evaluate and mute. If a warning is raised, surface it to the user and let them decide whether to suppress it.

## Documentation and Samples

Any API change must update the corresponding documentation, including XML doc comments and `docs/wiki/` pages such as `docs/wiki/Code-Writer.md`. Samples (the `SourceGeneratorFramework.ExampleGenerator` reference implementation and benchmarks) must use the current best-practice APIs: the minimal-parameter `CodeWriter` overloads, structured statements (`MethodCall`, `Return`, `Assignment`, `Throw`, `Comment`, `NetConditionalReturn`) rather than raw text, and the current method names. Add or update a sample whenever an API addition or change warrants a demonstrable example, and cover it with unit tests.

## Installed skills (`.agents/skills/`)

`Purview.BuildSdk` guidance — SDK configuration, project layout and detection, testing, skill
installation and skill tests — is published by the repository that owns it, and installed here by the
skills CLI. The installed skills are **committed**: `.agents/skills/**` and the CLI's
`skills-lock.json` (source, skill path and content hash of every entry) are both in source, so a clone
has them without running anything.

| Skill | Source | Load before |
| --- | --- | --- |
| `purview-buildsdk-*` (8 skills) | `purview-dev/build-sdk` | build/SDK configuration, project layout and detection, testing, skill installation and skill tests |

Skills are installed into the universal `.agents/skills/` location **only**. Never install copies
into `.claude/` or any other per-agent folder, and never commit a `CLAUDE.md`: this repository is not
agent-specific, and `AGENTS.md` is its one instruction file. `/.claude/` is gitignored for that
reason. Pass `-a universal`, which writes `.agents/skills/` and nothing else:

```bash
# add or update every skill from a source
npx -y skills@1 add purview-dev/build-sdk -s '*' -a universal -y

# restore the committed set from skills-lock.json
npx -y skills@1 experimental_install
```

Commit the changed `.agents/skills/**` and `skills-lock.json` together, in their own `chore` commit.
Do not edit anything under `.agents/skills/` by hand — the CLI is its only writer, and a hand edit no
longer matches the lockfile's hash. This repository's *own* skills are never installed here; they
live in `skills/` (below), and a second copy would drift. `Purview.BuildSdk`'s `InstallPurviewSkills`
target delegates to the same CLI; `src/Directory.Build.props` sets `PurviewSkillsAgents=universal` so
it is restricted the same way rather than defaulting to every agent (`*`).

`src/tests/Repository.UnitTests/InstalledSkillTests.cs` keeps this honest: every `skills-lock.json`
entry has its `.agents/skills/<name>/SKILL.md`, every installed folder is in the lockfile, no
authored `skills/` skill is copied into `.agents/skills/`, no `.claude/skills/` copy exists, and no
`CLAUDE.md` is present.

## Authoring skills

Authored skills live in the repository-root `skills/` folder. **Nothing packs them into a NuGet
package.** `Purview.BuildSdk` 1.0.6 removed skill packaging outright — the skills CLI clones the
*git* repository and only discovers a root `skills/` folder, so that is the one delivery route.
`PurviewSkillsToPack`, the `IncludeRepositorySkillsInPackage` shim in `src/Directory.Build.targets`,
and the `.agents/**/*` entries in `.config/purview-build.json` went with it. Read them at their
authored path, `skills/<name>/SKILL.md`.

Consumers install them with:

```bash
npx skills add purview-dev/sourcegenerator-framework
```

| Skill | Answers |
| --- | --- |
| `skills/purview-sourcegenerator-codewriter/` | emitting code: `CodeWriter`, `XmlCommentWriter`, scope safety, migrating a legacy string emitter |
| `skills/purview-sourcegenerator-pipeline/` | composing the incremental pipeline: `IncrementalPipeline`, `GenerationContext`, `GeneratorResult`, disable properties |
| `skills/purview-sourcegenerator-testing/` | the runner layer, `CodeQuery`, options, stage-by-stage cache tests |
| `skills/purview-sourcegenerator-tunit-tests/` | which TUnit base class and method, and the assertion extensions |
| `skills/purview-sourcegenerator-packaging/` | the self-contained merged analyzer, embedding a generator in another package, Roslyn compatibility |
| `skills/purview-sourcegenerator-diagnostics/` | what a `PSGFR`/`PSGF`/`PRSGD` diagnostic means, and why a component is silently not running |

Every skill carries the `purview-sourcegenerator` prefix. Skills install into one flat directory per
agent, shared with every skill a developer has installed from anywhere, so an unprefixed name —
`source-generator-testing`, `tunit-test-authoring` — collides and the loser is whichever installed
second.

To add a skill: create `skills/<name>/SKILL.md` with spec-legal frontmatter, and nothing else. There
is no per-project registration any more.

`src/tests/Repository.UnitTests/SkillTests.cs` validates every skill on every test run, through
`Purview.Skills.Testing`'s rule set (the Agent Skills specification: frontmatter fields, name and
description constraints, bundled links, body size). The `[InheritsTests]` attribute on that class is
load-bearing — TUnit does not walk base types during discovery, so without it the run is green and
nothing is checked. That project pins `net10.0` rather than taking the repository's testing set,
because it asserts something about the repository rather than about the runtime's reach.

A relative link from one `SKILL.md` into another skill's folder is a rule violation: only a skill's
own folder is installed with it, so such a link works here and nowhere else. Name the other skill in
prose instead, and keep deep detail in the skill's own `references/`.
