using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Purview.SourceGeneratorFramework.Analyzers;

/// <summary>
/// Flags unqualified XML documentation cref references to SGF public types in Roslyn components.
/// Qualifying these cref targets avoids the duplicate-framework ambiguity that can arise when the
/// same SGF type is visible through multiple assembly identities.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AmbiguousFrameworkCrefAnalyzer : DiagnosticAnalyzer
{
	public const string DiagnosticId = "PSGFR40";
	internal const string QualifiedTypePropertyName = "QualifiedTypeName";
	const string FrameworkAssemblyName = "Purview.SourceGeneratorFramework";

	static readonly DiagnosticDescriptor Rule = new(
		DiagnosticId,
		"Qualify SGF cref with global::",
		"XML documentation cref '{0}' refers to SGF type '{1}'; qualify the cref with 'global::'",
		"Purview.SourceGeneratorFramework",
		DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		description: "Detects unqualified XML documentation cref references to Purview.SourceGeneratorFramework public types in Roslyn components so they can be rewritten to a fully qualified global:: name."
	);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

	public override void Initialize(AnalysisContext context)
	{
		if (context is null)
			throw new ArgumentNullException(nameof(context));

		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterCompilationStartAction(static compilationStartContext =>
		{
			var analyzerOptions = compilationStartContext.Options.AnalyzerConfigOptionsProvider.GlobalOptions;

			if (!IsExplicitlyTrue(analyzerOptions, "IsRoslynComponent"))
				return;

			var frameworkAssembly = ResolveFrameworkAssembly(compilationStartContext.Compilation);
			if (frameworkAssembly is null)
				return;

			var publicFrameworkTypes = CollectPublicFrameworkTypes(frameworkAssembly.GlobalNamespace);
			if (publicFrameworkTypes.IsEmpty)
				return;

			compilationStartContext.RegisterSemanticModelAction(context =>
				AnalyzeSemanticModel(context, publicFrameworkTypes)
			);
		});
	}

	static void AnalyzeSemanticModel(
		SemanticModelAnalysisContext context,
		ImmutableDictionary<string, INamedTypeSymbol> publicFrameworkTypes
	)
	{
		var semanticModel = context.SemanticModel;
		var root = semanticModel.SyntaxTree.GetRoot(context.CancellationToken);

		foreach (var trivia in root.DescendantTrivia(descendIntoTrivia: true))
		{
			if (
				!trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)
				&& !trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia)
			)
			{
				continue;
			}

			foreach (var cref in trivia.GetStructure()?.DescendantNodesAndSelf().OfType<XmlCrefAttributeSyntax>() ?? [])
			{
				if (IsAlreadyQualified(cref.Cref))
					continue;

				if (!TryGetUnqualifiedTypeName(cref.Cref, out var shortTypeName))
					continue;

				if (!publicFrameworkTypes.TryGetValue(shortTypeName, out var frameworkType))
					continue;

				if (!ReferencesFrameworkType(semanticModel, cref.Cref, frameworkType, context.CancellationToken))
					continue;

				var qualifiedTypeName = frameworkType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
				var diagnostic = Diagnostic.Create(
					Rule,
					cref.GetLocation(),
					ImmutableDictionary<string, string?>.Empty.Add(QualifiedTypePropertyName, qualifiedTypeName),
					cref.Cref.ToString(),
					qualifiedTypeName.Substring("global::".Length)
				);

				context.ReportDiagnostic(diagnostic);
			}
		}
	}

	static bool IsAlreadyQualified(CrefSyntax cref) =>
		cref switch
		{
			TypeCrefSyntax { Type: QualifiedNameSyntax or AliasQualifiedNameSyntax } => true,
			NameMemberCrefSyntax { Name: QualifiedNameSyntax or AliasQualifiedNameSyntax } => true,
			_ => false,
		};

	static bool ReferencesFrameworkType(
		SemanticModel semanticModel,
		CrefSyntax cref,
		INamedTypeSymbol frameworkType,
		CancellationToken cancellationToken
	)
	{
		var symbolInfo = semanticModel.GetSymbolInfo(cref, cancellationToken);
		if (SymbolMatchesFrameworkType(symbolInfo.Symbol, frameworkType))
			return true;

		foreach (var candidate in symbolInfo.CandidateSymbols)
		{
			if (SymbolMatchesFrameworkType(candidate, frameworkType))
				return true;
		}

		return false;
	}

	static bool SymbolMatchesFrameworkType(ISymbol? symbol, INamedTypeSymbol frameworkType)
	{
		if (symbol is null)
			return false;

		var containingType = symbol switch
		{
			INamedTypeSymbol namedType => namedType,
			IMethodSymbol method => method.ContainingType,
			IPropertySymbol property => property.ContainingType,
			IFieldSymbol field => field.ContainingType,
			IEventSymbol @event => @event.ContainingType,
			_ => null,
		};

		return containingType is not null
			&& SymbolEqualityComparer.Default.Equals(
				containingType.OriginalDefinition,
				frameworkType.OriginalDefinition
			);
	}

	static bool TryGetUnqualifiedTypeName(CrefSyntax cref, out string typeName)
	{
		typeName = string.Empty;

		if (cref is TypeCrefSyntax { Type: IdentifierNameSyntax identifierName })
		{
			typeName = identifierName.Identifier.ValueText;
			return true;
		}

		if (cref is TypeCrefSyntax { Type: GenericNameSyntax genericName })
		{
			typeName = genericName.Identifier.ValueText;
			return true;
		}

		if (cref is NameMemberCrefSyntax { Name: IdentifierNameSyntax identifierMemberName })
		{
			typeName = identifierMemberName.Identifier.ValueText;
			return true;
		}

		if (cref is NameMemberCrefSyntax { Name: GenericNameSyntax genericMemberName })
		{
			typeName = genericMemberName.Identifier.ValueText;
			return true;
		}

		return false;
	}

	static IAssemblySymbol? ResolveFrameworkAssembly(Compilation compilation) =>
		compilation
			.References.Select(compilation.GetAssemblyOrModuleSymbol)
			.OfType<IAssemblySymbol>()
			.FirstOrDefault(static assembly =>
				string.Equals(assembly.Identity.Name, FrameworkAssemblyName, StringComparison.Ordinal)
			);

	static ImmutableDictionary<string, INamedTypeSymbol> CollectPublicFrameworkTypes(INamespaceSymbol rootNamespace)
	{
		var builder = ImmutableDictionary.CreateBuilder<string, INamedTypeSymbol>(StringComparer.Ordinal);
		CollectPublicFrameworkTypes(rootNamespace, builder);
		return builder.ToImmutable();
	}

	static void CollectPublicFrameworkTypes(
		INamespaceOrTypeSymbol container,
		ImmutableDictionary<string, INamedTypeSymbol>.Builder builder
	)
	{
		foreach (var member in container.GetMembers())
		{
			if (member is INamespaceSymbol namespaceSymbol)
			{
				CollectPublicFrameworkTypes(namespaceSymbol, builder);
				continue;
			}

			if (member is not INamedTypeSymbol namedType)
				continue;

			if (RoslynComponentDiscovery.IsEffectivelyPublic(namedType) && !builder.ContainsKey(namedType.Name))
				builder.Add(namedType.Name, namedType);

			CollectPublicFrameworkTypes(namedType, builder);
		}
	}

	static bool IsExplicitlyTrue(AnalyzerConfigOptions options, string propertyName) =>
		options.TryGetValue("build_property." + propertyName, out var value)
		&& string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
}
