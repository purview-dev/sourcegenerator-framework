# Recipes for SourceGeneratorFramework. Run `just` for the list.
#
# The shared recipes come from .build/just/, copied verbatim from the purview-buildsdk-justfile
# skill. Change them there, not here; this file holds what belongs to this repository.

set quiet

# Let a definition here override a module's default variable or no-op hook.
set allow-duplicate-variables
set allow-duplicate-recipes

import ".build/just/index.just"

solution_file := "src/SourceGeneratorFramework.slnx"

benchmark := "src/src/SourceGeneratorFramework.Benchmarks/SourceGeneratorFramework.Benchmarks.csproj"

# The Benchmarks project deliberately opts out of Purview.BuildSdk, because BenchmarkDotNet generates
# child projects under it, so a solution-wide run of an SDK target fails with MSB4057. Point the skill
# targets at the main package project instead: they act on the repository root and run once either way.
skills_project := "src/src/SourceGeneratorFramework/SourceGeneratorFramework.csproj"

[private]
default:
    just --list

# Kept local rather than taken from dotnet.just: --results-directory-layout per-module stops a
# multi-target test run (net10.0 and net11.0) writing reports with the same relative file name and
# overwriting one another. See docs/wiki/platform/DotNet-11.md.
[doc('Run the tests matching a tree-node filter (default: every test)')]
[group('Build and Test')]
test filter=test_filter *args:
    echo "==> Testing {{ BLUE }}{{ solution_file }}{{ NORMAL }} ({{ YELLOW }}{{ build_configuration }}{{ NORMAL }}) with filter {{ GREEN }}{{ filter }}{{ NORMAL }}"
    dotnet test --project "{{ solution_file }}" --configuration "{{ build_configuration }}" --treenode-filter "{{ filter }}" --results-directory-layout per-module --ignore-exit-code 8 {{ args }}

# Run the unit tests only
[group('Build and Test')]
test-unit *args:
    just test "/*/*/*/*[Category=Unit]" {{ args }}

# Run the BenchmarkDotNet benchmarks for the project
[group('Build and Test')]
benchmark *args:
    echo "==> Running benchmarks for {{ BLUE }}{{ solution_file }}{{ NORMAL }}"
    dotnet run --project "{{ benchmark }}" --configuration Release --framework net11.0 --no-build {{ args }}

# Clean the solution in both Release and Debug
[group('Build and Test')]
clean-all *args:
    echo "==> Cleaning {{ BLUE }}{{ solution_file }}{{ NORMAL }} in Release and Debug"
    dotnet clean "{{ solution_file }}" --configuration Release {{ args }}
    dotnet clean "{{ solution_file }}" --configuration Debug {{ args }}

# Kept local rather than taken from purview-build-nuget.just: formatting runs first, so a local
# release does not fail its lint check on code that only needed reformatting.
[doc('Run the release pipeline, publishing to a local NuGet feed, after formatting the tree')]
[group('Pipeline')]
pipeline-local-release *args: ensure-tools lint-fix
    echo "==> Running the local release pipeline"
    {{ pipeline_tool }} --Release:Mode=LocalNuGet {{ args }}

# Validate the skills authored in skills/ (PRSKL0005), as the pack-time check does
[group('Agent Skills')]
skills-validate *args:
    dotnet msbuild "{{ skills_project }}" -t:ValidatePurviewSkills -v:n {{ args }}
