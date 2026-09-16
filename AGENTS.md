# Agent Instructions

This repository contains custom build rules, analysers, and source generators. Agents working in this codebase should follow the guidelines below.

## Warnings and Suggestions

Do **not** suppress or mute compiler, analyser, or build warnings by adding `<NoWarn>` entries, `#pragma warning disable`, or similar directives in code or project files without explicit user direction. Warnings and suggestions are the responsibility of the developer/user to evaluate and mute. If a warning is raised, surface it to the user and let them decide whether to suppress it.

## Documentation and Samples

Any API change must update the corresponding documentation, including XML doc comments and `docs/wiki/` pages such as `docs/wiki/Code-Writer.md`. Samples (the `SourceGeneratorFramework.ExampleGenerator` reference implementation and benchmarks) must use the current best-practice APIs: the minimal-parameter `CodeWriter` overloads, structured statements (`MethodCall`, `Return`, `Assignment`, `Throw`, `Comment`, `NetConditionalReturn`) rather than raw text, and the current method names. Add or update a sample whenever an API addition or change warrants a demonstrable example, and cover it with unit tests.
