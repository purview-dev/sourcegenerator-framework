# Testing with TUnit

`Purview.SourceGeneratorFramework.Testing.TUnit` is the TUnit integration for testing incremental C#
source generators built with `Purview.SourceGeneratorFramework`.

## Installation

```bash
dotnet add package Purview.SourceGeneratorFramework.Testing.TUnit
```

## What's included

- **`TUnitSourceGeneratorTestBase<TGenerator>`** — ready-made base class for TUnit tests. It wires
  generator log output to `TestContext.Current.OutputWriter`.
- **Custom TUnit assertions** for inspecting `DriverRunResult` instances directly in TUnit tests.
- **MSBuild `.props`** — automatically adds `global using` directives for
  `Purview.SourceGeneratorFramework.Testing.TUnit` and
  `Purview.SourceGeneratorFramework.Testing.TUnit.Assertions`.

## Usage

Reference the package from a TUnit test project:

```xml
<ItemGroup>
  <PackageReference Include="TUnit" />
  <PackageReference Include="Purview.SourceGeneratorFramework.Testing.TUnit" />
</ItemGroup>
```

Derive your test class from `TUnitSourceGeneratorTestBase<TGenerator>` and use the inherited
`GenerateAsync` method:

```csharp
using Purview.SourceGeneratorFramework.Testing.TUnit;

public class MyGeneratorTests : TUnitSourceGeneratorTestBase<MyGenerator>
{
    [Test]
    public async Task GeneratesExpectedSource()
    {
        var source = """
            [MyNamespace.MyAttribute]
            public partial class MyClass { }
            """;

        var result = await GenerateAsync(source);

        result.AssertNoCompilationErrors();
        var generated = result.AssertSingleGeneratedSource();

        await Assert.That(generated).Contains("public static partial class MyClass");
    }
}
```

The base class also provides access to the underlying `SourceGeneratorTestRunner<TGenerator>` behavior
through `GenerateAsync`.

## Using generated types in the TUnit project

If test source files use generated attributes or other generated declarations while the tests also
derive from `TUnitSourceGeneratorTestBase<TGenerator>`, reference the generator project both as an
analyzer and as a normal assembly:

```xml
<ItemGroup>
  <!-- Generates declarations used by classes in this TUnit project. -->
  <ProjectReference
    Include="..\..\src\MyGenerator\MyGenerator.csproj"
    PrivateAssets="all"
    OutputItemType="Analyzer"
    ReferenceOutputAssembly="false"
  />

  <!-- Makes MyGenerator available as TGenerator. -->
  <ProjectReference
    Include="..\..\src\MyGenerator\MyGenerator.csproj"
    PrivateAssets="all"
    ReferenceOutputAssembly="true"
  />
</ItemGroup>
```

For example, the analyzer reference allows a test fixture to use `[MyGeneratedAttribute]`, while the
normal reference allows the test class to derive from `TUnitSourceGeneratorTestBase<MyGenerator>`. Do
not add `OutputItemType="Analyzer"` to the normal reference.

For multi-target TUnit projects, the normal reference means the generator's Roslyn dependencies
participate in reference resolution for every target. Build the generator against the Roslyn version
that supports its API usage; this framework is built against Roslyn 5.0, which ships `net8.0` and
`net9.0` package assets, so a .NET 8–10 test matrix still loads it. Compiler hosts that consume the
generator as an analyzer must be Roslyn 5.0 or later (`.NET 10` SDK / Visual Studio 2026). Do not
force a newer `System.Collections.Immutable` version through central package management.

## Running a packaged (merged) generator in tests

A component that ships as a self-contained analyzer must **not** be added as a compile-time
`<Reference>`. Its assembly carries the framework implementation merged into itself, and — because
ILRepack cannot internalize a framework type that reaches the component's public API surface — some
packages expose framework types publicly. Referencing such an assembly from a test project that also
loads the real `Purview.SourceGeneratorFramework.dll` (which the Testing packages do) makes every
framework type ambiguous (`CS0433`). From framework `1.0.0-prerelease.51` the merge tool guarantees a
merged component exposes no framework types apart from its own Roslyn entry points, but a merged
component remains an analyzer artifact and should still be consumed out of band.

To register a packaged generator as an additional generator/analyzer:

1. copy the analyzer DLL from the package beside the test binaries, without referencing it:

```xml
<PackageReference Include="My.Generator.Package" GeneratePathProperty="true" />

<ItemGroup>
  <None
    Include="$(PkgMy_Generator_Package)\analyzers\dotnet\cs\My.Generator.dll"
    Link="My.Generator.dll"
    CopyToOutputDirectory="PreserveNewest"
    Visible="false" />
</ItemGroup>
```

2. resolve the types out of band and pass them through the options:

```csharp
var assembly = Assembly.LoadFrom(Path.Combine(AppContext.BaseDirectory, "My.Generator.dll"));

options.AdditionalGeneratorTypes =
    [.. options.AdditionalGeneratorTypes, assembly.GetType("My.Namespace.MyGenerator", throwOnError: true)!];
options.AnalyzerTypes = [assembly.GetType("My.Namespace.MyAnalyzer", throwOnError: true)!];
```

`SourceGeneratorTestRunner` instantiates the supplied types with `Activator.CreateInstance`, so this is
equivalent to `typeof(...)` without the compile-time reference. Two consequences: the loaded
generator's framework copy owns its own logging registry and CodeWriter scope validation (do not
assert on its `LogEntries`, and leave `ValidateCodeWriterScopes` off for that run), and the loaded
assembly must be built against a Roslyn version compatible with the test host.

For a component in the same repository, prefer a project reference to the component project: it
resolves the **unmerged** bin output plus the loose framework DLL, so framework types keep a single
identity and `typeof(...)`, `InternalsVisibleTo`, and every assertion API keep working.

## Which base class and method

| Roslyn type | Base class | Method |
|---|---|---|
| Generator | `TUnitSourceGeneratorTestBase<TGenerator>` | `GenerateAsync(source, options, ct)` |
| Diagnostic analyzer | `TUnitDiagnosticAnalyzerTestBase<TAnalyzer>` | `AnalyzeAsync(source, options, ct)` |
| Code fix (single) | `TUnitCodeFixTestBase<TAnalyzer, TCodeFix>` | `ApplyCodeFixAsync(source, options, ct)` |
| Code fix (fix-all) | `TUnitCodeFixTestBase<TAnalyzer, TCodeFix>` | `ApplyFixAllAsync(sources, options, ct)` |
| Refactoring | `TUnitRefactoringTestBase<TRefactoring>` | `RefactorAsync(source, options, ct)` |

For cache tests, `TUnitSourceGeneratorTestBase` also exposes `GenerateIncrementalAsync(...)`.

## Easy starting point: derived options

Derive a `SourceGeneratorTestOptions` record that seeds namespaces and additional assemblies, then
pass it to every test:

```csharp
public sealed record MyTestOptions : SourceGeneratorTestOptions
{
    public MyTestOptions()
    {
        AdditionalNamespaces = AdditionalNamespaces.Add("My.Namespace");
        AdditionalAssemblyTypes = AdditionalAssemblyTypes.AddRange(typeof(SomeDependencyType), typeof(TypeIdentity));
        DisableSourceGeneratorPropertyName = "DisableMyGenerator";
    }
}

public class MyGeneratorTests : TUnitSourceGeneratorTestBase<MyGenerator, MyTestOptions> { ... }
```

Use `options.Compile()` for `CompileToAssembly`, and the
`OnBeforeRun`/`OnBeforeRunAsync`/`OnAfterRun` hooks for per-run customisation. Code-fix/refactoring
tests select actions with `EquivalenceKey` or `CodeActionIndex` (and
`RefactorTestOptions.NodeSelector`/`Span`).

## Assertion extensions

All assertion extensions are under `Purview.SourceGeneratorFramework.Testing.TUnit.Assertions`
(globally imported). `await Assert.That(...)` is terminal and returns the value:

- `HasGeneratedMethod` / `HasGeneratedMethodReturnType` / `HasGeneratedClass` / `HasGeneratedProperty` /
  `HasGeneratedField` / `HasGeneratedSyntaxTree` — return the syntax node;
  `HasGeneratedMethod(name, TypeReference[])` matches parameter types. `HasGeneratedClass(name, arity)`
  (or a `TypeIdentity` with arity) matches a generic type by its type-parameter count, so
  `new TypeIdentity("ResourceDefinition", ns, arity: 1)` finds `ResourceDefinition<T>` without matching
  the non-generic `ResourceDefinition`.
- `HasFixedMethod` — same for code-fix and refactoring results.
- `HasPropertyOfType` / `HasFieldOfType` / `HasMethodOfType` / `HasConstructorOfType` /
  `HasAttributeOfType` / `HasNestedType` — chain from a scoped `CodeQueryResult<T>` (for example the
  result of `HasGeneratedClass`) and return the matched member. The node-producing assertions move the
  chain onto the matched node, so you can append node-inspection assertions with `.And`:
  ```csharp
  var method = await Assert.That(query)
      .HasGeneratedClass("Service")
      .And.HasNestedType("Builder")
      .And.WithAccessibility(Accessibility.Private)
      .And.HasMethodOfType("Build", []);
  ```
- `WithAccessibility` / `WithGetterAccessibility` / `WithSetterAccessibility` / `WithBaseType` /
  `WithGenericTypeParameter(s)` / `IsInNamespace` / `IsInGlobalNamespace` — node-inspection assertions
  that keep the matched node on the chain. Accessibility resolves C# defaults (an unmodified nested
  type is `Private`, a top-level type `Internal`, interface/enum members `Public`, and an accessor with
  no modifier inherits its property's accessibility).
- `HasDiagnostic` / `HasDiagnostics` / `HasNoDiagnostics` / `DoesNotHaveDiagnostic` /
  `HasNoErrorDiagnostics`.
- `HasSymbol(TypeIdentity)` / `HasSymbol("Namespace.Type")`.
- `GeneratesCode(expected)` / `ContainsGeneratedCode(expected)` (whitespace-flattened).

The `CodeQuery` assertions operate on a `CodeQuery` directly, so they accept a query from any test
result — `result.Generated()` for generated code, `result.Output()` for the whole compilation, or
`result.FixedCode()` for fixed/refactored code. Convenience overloads on the test result types query
the generated (or fixed) code for you.

To assert a nullable expected type, use the test-only `query.MakeNullable(...)` extension: it resolves
the annotation against the query's compilation and, unlike `TypeReference.Nullable()` /
`TypeIdentity.MakeNullable()`, does not trigger the `PSGFR16` context-overload suggestion (tests have
no generation context to pass).

```csharp
var query = result.Generated();
MethodDeclarationSyntax method = await Assert.That(query).HasGeneratedMethod("DoWork", [intType, nullableInt]);
await Assert.That(query).HasGeneratedSyntaxTree("Service.g.cs");
await Assert.That(result.FixedCode()).HasFixedMethod("DoWork");   // code-fix / refactor results

// Scoped member chaining:
CodeQueryResult<ClassDeclarationSyntax> attributeClass = await Assert.That(query).HasGeneratedClass(hostKitAttribute);
await Assert.That(attributeClass).HasPropertyOfType("Name", query.MakeNullable(TypeLibrary.System.String));
```

## Incremental cache tests

`GenerateIncrementalAsync` proves the pipeline caches stage-by-stage (first run `New`, identical rerun
`Cached`/`Unchanged`, targeted changes mark only the affected stage `Modified`). A reference
implementation (`ServiceRegistrationCacheTests`) lives in the `Purview.SourceGeneratorFramework` source
repository's example generator tests; replicate it in your own project with your own stage names. See
[Step-Cache-Tests.md](Step-Cache-Tests.md) for the full walkthrough.

## License

This documentation is part of the MIT-licensed `Purview.SourceGeneratorFramework` project.