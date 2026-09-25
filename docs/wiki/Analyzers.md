# Analyzers

The `Purview.SourceGeneratorFramework` package includes the
`Purview.SourceGeneratorFramework.Analyzers` assembly as an analyzer asset, together with the
`Purview.SourceGeneratorFramework.CodeFixers` code fix providers. The diagnostics are enabled
automatically when you reference `Purview.SourceGeneratorFramework` from a source generator project.

## How the analyzers are shipped

The analyzer and code-fix assemblies are built as Roslyn components
(`IsRoslynComponent = true`) and packed into the `Purview.SourceGeneratorFramework` package under
`analyzers/dotnet/cs/`. Because they are not separately packable NuGet packages, they are documented
here rather than in a standalone README.

The analyzers enforce two families of rules:

- **Incremental generator best practice** — `PSGFR11`–`PSGFR33`, covering pipeline design,
  `CodeWriter` usage, Roslyn component discovery, and extension-class conventions.
- **C# 14 extension-member conventions** — `PSGFR34`–`PSGFR38`, plus the associated
  `ReorganizeExtensionClassCodeFixProvider` and `ConvertToExtensionBlockCodeFixProvider`.

## Build-time validation diagnostics

These are MSBuild diagnostics rather than compiler analyzers, so they are not tracked in
`AnalyzerReleases.*.md`:

| Code | Raised by | Summary |
|------|-----------|---------|
| `PSGF0001` | `Purview.BuildSdk` | A Roslyn component did not produce (or did not declare) a source-generator analyzer file. |
| `PSGF0003` | `Purview.SourceGeneratorFramework` | A `PurviewGeneratorVisibleProperty` is not compiler-visible in the declaring project or its `Sdk/build`/`Sdk/buildTransitive` assets, so consumers cannot read `build_property.<Name>`. |
| `PRSGD0005` | `Purview.BuildSdk` | A file in the returned analyzer closure references an assembly that is neither part of the closure nor a compiler-host assembly. |

Opt out with `PurviewSourceGeneratorFrameworkAnalyzerValidation=false` (`PRSGD0005`) or
`PurviewSourceGeneratorFrameworkGeneratorPropertyValidation=false` (`PSGF0003`).

## Rule reference

| Rule | Summary |
|------|---------|
| `PSGFR11` | Prefer `SyntaxProvider.ForAttributeWithMetadataName` over `CreateSyntaxProvider` for attribute-based detection. |
| `PSGFR12` | Use `IIncrementalGenerator` / `RegisterSourceOutput` instead of `ISourceGenerator`. |
| `PSGFR14` | Avoid `RegisterImplementationSourceOutput` unless implementation-only output is required. |
| `PSGFR15` | Pipeline model collection members should use sequence equality (e.g. `EquatableArray<T>`). |
| `PSGFR16` | Prefer the nullable-context `Nullable()`/`MakeNullable()` overload so annotations honour the target compilation. |
| `PSGFR17` | Consume `CodeWriter` scope-returning methods (`...Scope`, `IndentedScope`) with `using`. |
| `PSGFR18` | Prefer structured declaration APIs (`Class`, `Method`, `Property`, `Field`) over raw declaration text. |
| `PSGFR19` | Prefer structured statement APIs (`Return`, `MethodCall`, `Throw`, `Assignment`, `Using`, `Comment`) over raw statement text. |
| `PSGFR20` | Prefer the minimal `CodeWriter` overloads over constructing `*DeclarationOptions` values manually. |
| `PSGFR21` | Prefer `HashDefines`/`HashDefinesScope` for `#if`/`#endif` conditional-compilation directives. |
| `PSGFR22` | Prefer `PragmaDisable`/`OpenPragmasScope` for `#pragma warning` directives. |
| `PSGFR23` | Prefer structured `IfBlock`/`ElseIf`/`Else` over raw `if` block text. |
| `PSGFR24` | `CodeFixProvider` is not marked `[ExportCodeFixProvider]`; Visual Studio will never discover it. |
| `PSGFR25` | `DiagnosticAnalyzer` is not marked `[DiagnosticAnalyzer]`; it will never run. |
| `PSGFR26` | A generator type is not marked `[Generator]`; it will never run. |
| `PSGFR27` | A Roslyn component type is not public; the compiler host cannot instantiate it. |
| `PSGFR28` | `FixableDiagnosticIds` references a diagnostic ID no analyzer in the compilation produces; the fix will never be shown. |
| `PSGFR29` | Do not embed a `CodeWriter` in a string; use `XmlCommentWriter.XmlInlineCode` instead. |
| `PSGFR30` | Prefer `static` lambdas in incremental pipeline methods so the compiler never allocates a closure on the per-item hot path. |
| `PSGFR31` | Prefer `GeneratorAttributeSyntaxContext.TargetSymbol` over `SemanticModel.GetDeclaredSymbol(ctx.TargetNode)`. |
| `PSGFR32` | Avoid `NormalizeWhitespace` when generating source; use an indented text writer such as `CodeWriter`. |
| `PSGFR33` | Pipeline models must not retain Roslyn objects (`ISymbol`, `SyntaxNode`, `Location`, ...); extract the information into value types. |
| `PSGFR34` | Prefer C# 14 `extension(Receiver)` blocks over classic static `this`-parameter extension methods. |
| `PSGFR35` | Extension class name must match the extended type (`{Receiver}Extensions`). |
| `PSGFR36` | Extension classes must be placed in the extended type's namespace under an `Extensions` folder. |
| `PSGFR37` | One extension class per receiver type; split classes that extend multiple types. |
| `PSGFR38` | Extension classes should carry `[EditorBrowsable(EditorBrowsableState.Never)]`. |
| `PSGFR39` | A non-packable Roslyn component that explicitly opts out of the default self-contained analyzer output (`PurviewMergeSourceGeneratorFrameworkForAnalyzerFiles=false`) while embedding the framework, otherwise the package embeds the loose framework DLL under `analyzers/`. |
| `PSGFR40` | In Roslyn components (`IsRoslynComponent=true`), reference SGF types as inline code (`<c>Type</c>`) instead of a `cref`: copied documentation must not depend on cref resolution. |

## Type-library and attribute-model diagnostics

The bundled generators carry their own diagnostic families, reported by the
`TypeLibraryValidationAnalyzer` (`TLB0001`–`TLB0019`) and the attribute-data-model validation
analyzers. These are documented on their feature pages:

- [Type-Library.md](Type-Library.md#validation)
- [Attribute-Data-Models.md](Attribute-Data-Models.md)

## Code fixes

Code fix providers ship in the `Purview.SourceGeneratorFramework.CodeFixers` assembly and cover the
analyzer rules above, including:

- `AddGeneratorAttributeCodeFixProvider` — adds the missing `[Generator]` attribute (`PSGFR26`).
- `AddDiagnosticAnalyzerAttributeCodeFixProvider` — adds `[DiagnosticAnalyzer]` (`PSGFR25`).
- `AddExportCodeFixProviderAttributeCodeFixProvider` — adds `[ExportCodeFixProvider]` (`PSGFR24`).
- `MakeRoslynComponentPublicCodeFixProvider` — makes the component type public (`PSGFR27`).
- `RemoveOrphanedFixableDiagnosticIdCodeFixProvider` — removes unused fixable diagnostic IDs (`PSGFR28`).
- `PreferTargetSymbolCodeFixProvider` — switches to `TargetSymbol` (`PSGFR31`).
- `PreferStaticLambdaCodeFixProvider` — makes pipeline lambdas `static` (`PSGFR30`).
- `PreferNullableContextOverloadCodeFixProvider` — adds the generation context to `Nullable()` /
  `MakeNullable()` calls, including project-wide "Fix all" support (`PSGFR16`).
- `PipelineModelReferenceEqualityCollectionCodeFixProvider` — wraps collection members for sequence
  equality (`PSGFR15`).
- `PreferStructuredCodeWriterIfBlockCodeFixProvider` — rewrites raw `if`/`else if`/`else` block text
  to the structured `IfBlock`/`ElseIf`/`Else` APIs (`PSGFR23`).
- `CodeWriterToStringCodeFixProvider` — replaces embedded `CodeWriter` string interpolation (`PSGFR29`).
- `PreferInlineCodeForFrameworkCrefCodeFixProvider` — rewrites SGF XML doc `cref` targets to inline code (`<c>Type</c>`) (`PSGFR40`).
- `AttributeDataModelSymbolPropertyCodeFixProvider` — fixes attribute-data-model symbol properties.
- `ReorganizeExtensionClassCodeFixProvider` — renames (`PSGFR35`), splits multi-receiver classes
  (`PSGFR37`), moves the class under `Extensions/{ReceiverNamespace}/`, and updates referencing files
  (`PSGFR36`).
- `ConvertToExtensionBlockCodeFixProvider` — converts classic methods to C# 14 `extension` blocks
  (`PSGFR34`).
- `AddExtensionClassMetadataCodeFixProvider` — adds `[EditorBrowsable(EditorBrowsableState.Never)]`
  to extension classes (`PSGFR38`).
- Type-library fixes — `TypeLibraryMemberAccessibilityCodeFixProvider`,
  `TypeLibraryMarkerDefaultInitializerCodeFixProvider`, `MakeTypeLibrarySpecPartialCodeFixProvider`,
  `RenameTypeLibrarySpecCodeFixProvider`, and `TypeLibraryMemberTypeCodeFixProvider`.

See [Guide.md](Guide.md#19-extension-class-conventions) for the extension-class conventions the
`PSGFR34`–`PSGFR38` rules enforce.

## Roslyn component discovery

The compiler host only loads a source generator, diagnostic analyzer, or code fix provider when three
conditions hold. Missing any one means the component is **silently ignored**:

1. **The type is public** (`PSGFR27`).
2. **The type is decorated** — `[Generator]` (`PSGFR26`), `[DiagnosticAnalyzer]` (`PSGFR25`), or
   `[ExportCodeFixProvider]` (`PSGFR24`).
3. **The assembly is loaded as an analyzer** — packed under `analyzers/dotnet/cs/` in a package, or
   referenced with `OutputItemType="Analyzer"` in a project reference.

A code fix provider also only appears when the diagnostic ID in `FixableDiagnosticIds` is actually
produced by an analyzer loaded alongside it (`PSGFR28`). Visual Studio MEF-composes fix providers when
the analyzer set loads, so after adding or updating a fixer assembly you must restart Visual Studio or
reload the project for the fixes to appear.

## License

This documentation is part of the MIT-licensed `Purview.SourceGeneratorFramework` project.
