using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Purview.SourceGeneratorFramework.Analyzers;

/// <summary>
/// Errors when a Roslyn component (source generator, diagnostic analyzer, or code fix provider)
/// references <c>Purview.SourceGeneratorFramework</c> but is not configured to produce a
/// self-contained analyzer output. Packing such a component under <c>analyzers/dotnet/cs</c> forces
/// the loose <c>Purview.SourceGeneratorFramework.dll</c> into the package, reintroducing the
/// shared-version hazard where two generators with different framework versions compete in the
/// compiler process.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SelfContainedGeneratorAnalyzer : DiagnosticAnalyzer
{
	public const string DiagnosticId = "PSGFR39";

	internal const string FrameworkAssemblyName = "Purview.SourceGeneratorFramework";
	internal const string EmbedProperty = "PurviewEmbedSourceGeneratorFramework";
	internal const string MergeAnalyzerFilesProperty = "PurviewMergeSourceGeneratorFrameworkForAnalyzerFiles";
	internal const string AnalyzerValidationProperty = "PurviewSourceGeneratorFrameworkAnalyzerValidation";

	static readonly DiagnosticDescriptor Rule = new(
		DiagnosticId,
		"Roslyn component must produce a self-contained analyzer",
		"This Roslyn component references {0} but is not configured to produce a self-contained analyzer. If it is packed (directly or embedded in another package) the package will ship the loose {0}.dll under analyzers/, reintroducing the shared-version hazard. Set {1}=true, or pack it self-contained via GetPurviewMergedAnalyzerFile (see Packaging.md). If this component is only used in-repo or is shipped via GetPurviewMergedAnalyzerFile, set {2}=false.",
		"Purview.SourceGeneratorFramework",
		DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		description: "Detects Roslyn components that reference Purview.SourceGeneratorFramework without producing a self-contained analyzer output, which would force the loose framework DLL to be shipped under analyzers/ in any package that embeds them.",
		customTags: ["CompilationEnd"]
	);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

	public override void Initialize(AnalysisContext context)
	{
		if (context is null)
			throw new ArgumentNullException(nameof(context));
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterCompilationAction(context =>
		{
			var options = context.Options.AnalyzerConfigOptionsProvider;
			var tree = context.Compilation.SyntaxTrees.FirstOrDefault();
			var analyzerOptions = tree is not null ? options.GetOptions(tree) : options.GlobalOptions;

			if (!IsExplicitlyTrue(analyzerOptions, "IsRoslynComponent"))
				return;

			if (!ReferencesSourceGeneratorFramework(context.Compilation))
				return;

			// Merge disabled: only the framework compile-time library opts out of embedding.
			if (IsExplicitlyFalse(analyzerOptions, EmbedProperty))
				return;

			// A packable generator merges at GenerateNuspec and deletes the loose DLL itself.
			if (IsExplicitlyTrue(analyzerOptions, "IsPackable"))
				return;

			// GetSourceGeneratorAnalyzerFiles returns a merged, self-contained component.
			if (IsExplicitlyTrue(analyzerOptions, MergeAnalyzerFilesProperty))
				return;

			// Explicit acknowledgement that the component is never packed, or is shipped
			// self-contained via GetPurviewMergedAnalyzerFile.
			if (IsExplicitlyFalse(analyzerOptions, AnalyzerValidationProperty))
				return;

			context.ReportDiagnostic(
				Diagnostic.Create(
					Rule,
					tree?.GetRoot().GetLocation() ?? Location.None,
					FrameworkAssemblyName,
					MergeAnalyzerFilesProperty,
					AnalyzerValidationProperty
				)
			);
		});
	}

	static bool ReferencesSourceGeneratorFramework(Compilation compilation) =>
		compilation
			.References.Select(compilation.GetAssemblyOrModuleSymbol)
			.OfType<IAssemblySymbol>()
			.Any(static assembly =>
				string.Equals(assembly.Identity.Name, FrameworkAssemblyName, StringComparison.OrdinalIgnoreCase)
			);

	static bool IsExplicitlyTrue(AnalyzerConfigOptions options, string propertyName) =>
		options.TryGetValue("build_property." + propertyName, out var value)
		&& string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);

	static bool IsExplicitlyFalse(AnalyzerConfigOptions options, string propertyName) =>
		options.TryGetValue("build_property." + propertyName, out var value)
		&& string.Equals(value, "false", StringComparison.OrdinalIgnoreCase);
}
