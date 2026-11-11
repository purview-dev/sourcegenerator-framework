using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Purview.SourceGeneratorFramework.Analyzers;

/// <summary>
/// Flags types that implement a generator interface without the <c>[Generator]</c> attribute, so
/// the generator is silently never executed.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MissingGeneratorAttributeAnalyzer : DiagnosticAnalyzer
{
	public const string DiagnosticId = "PSGFR26";

	public static readonly DiagnosticDescriptor Rule = new(
		DiagnosticId,
		"Source generator is not marked [Generator]",
		"Type '{0}' implements {1} but is not marked [Generator]; the generator will never run",
		"Purview.SourceGeneratorFramework",
		DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		description: "Source generators must implement IIncrementalGenerator or ISourceGenerator AND be decorated with [Generator]. Without the attribute the generator is silently ignored."
	);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

	public override void Initialize(AnalysisContext context)
	{
		if (context is null)
			throw new ArgumentNullException(nameof(context));

		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterCompilationStartAction(context =>
		{
			var incrementalGeneratorType = context.Compilation.GetTypeByMetadataName(
				"Microsoft.CodeAnalysis.IIncrementalGenerator"
			);
			var legacyGeneratorType = context.Compilation.GetTypeByMetadataName(
				"Microsoft.CodeAnalysis.ISourceGenerator"
			);
			var generatorAttributeType = context.Compilation.GetTypeByMetadataName(
				"Microsoft.CodeAnalysis.GeneratorAttribute"
			);

			context.RegisterSymbolAction(
				context =>
					AnalyzeNamedType(context, incrementalGeneratorType, legacyGeneratorType, generatorAttributeType),
				SymbolKind.NamedType
			);
		});
	}

	static void AnalyzeNamedType(
		SymbolAnalysisContext context,
		INamedTypeSymbol? incrementalGeneratorType,
		INamedTypeSymbol? legacyGeneratorType,
		INamedTypeSymbol? generatorAttributeType
	)
	{
		if (context.Symbol is not INamedTypeSymbol type)
			return;

		var implementsIncremental = RoslynComponentDiscovery.IsSourceGenerator(
			type,
			incrementalGeneratorType,
			legacyGeneratorType
		);
		if (!implementsIncremental)
			return;

		if (RoslynComponentDiscovery.HasAttribute(type, generatorAttributeType))
			return;

		var interfaceName = type.AllInterfaces.FirstOrDefault(i =>
				SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, incrementalGeneratorType)
				|| SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, legacyGeneratorType)
			)
			?.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)
			?? "a generator interface";

		context.ReportDiagnostic(
			Diagnostic.Create(
				Rule,
				type.Locations.FirstOrDefault(static loc => loc.IsInSource) ?? Location.None,
				type.Name,
				interfaceName
			)
		);
	}
}
