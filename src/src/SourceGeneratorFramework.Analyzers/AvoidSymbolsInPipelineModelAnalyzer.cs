using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Purview.SourceGeneratorFramework.Analyzers;

/// <summary>
/// Flags incremental generator pipeline model members that retain Roslyn objects (<c>ISymbol</c>,
/// <c>SyntaxNode</c>, <c>Location</c>, <c>AttributeData</c>, <c>SemanticModel</c>, <c>Compilation</c>, ...)
/// or collections thereof. Such objects are never equatable between runs, so they break incremental
/// caching and can root old compilations.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AvoidSymbolsInPipelineModelAnalyzer : DiagnosticAnalyzer
{
	public const string DiagnosticId = "PSGFR33";

	public static readonly DiagnosticDescriptor Rule = new(
		DiagnosticId,
		"Pipeline model retains a Roslyn object",
		"Pipeline value '{0}' uses '{1}', which is never equatable between runs and can root old compilations. Extract the information you need into strings or value types so incremental caching works.",
		"Purview.SourceGeneratorFramework",
		DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		description: "Pipeline models must not retain ISymbol, SyntaxNode, Location, or other Roslyn objects; extract the information needed into an equatable representation (strings usually work well).",
		customTags: WellKnownDiagnosticTags.CompilationEnd
	);

	static readonly string[] NonEquatableRoslynTypes =
	[
		"Microsoft.CodeAnalysis.ISymbol",
		"Microsoft.CodeAnalysis.SyntaxNode",
		"Microsoft.CodeAnalysis.SyntaxToken",
		"Microsoft.CodeAnalysis.SyntaxTrivia",
		"Microsoft.CodeAnalysis.Location",
		"Microsoft.CodeAnalysis.AttributeData",
		"Microsoft.CodeAnalysis.SemanticModel",
		"Microsoft.CodeAnalysis.Compilation",
		"Microsoft.CodeAnalysis.SyntaxTree",
		"Microsoft.CodeAnalysis.SyntaxReference",
		"Microsoft.CodeAnalysis.Text.SourceText",
		"Microsoft.CodeAnalysis.GeneratorAttributeSyntaxContext",
	];

	static readonly string[] ProviderMetadataNames =
	[
		"Microsoft.CodeAnalysis.IncrementalValuesProvider`1",
		"Microsoft.CodeAnalysis.IncrementalValueProvider`1",
	];

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

	public override void Initialize(AnalysisContext context)
	{
		if (context is null)
			throw new ArgumentNullException(nameof(context));

		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();

		// A pipeline stage is flagged where the Roslyn object enters it, which a named model type never
		// sees: ForAttributeWithMetadataName returning the symbol, or a Select projecting one.
		// AnalyzeModelType only walks a model's own members, so neither shape is reachable from it.
		//
		// The findings are collected during the compilation and reported at the end rather than from the
		// operation action itself: Rule carries WellKnownDiagnosticTags.CompilationEnd, and Roslyn drops a
		// diagnostic with that tag when it is reported from anywhere but a compilation-end action.
		context.RegisterCompilationStartAction(static startContext =>
		{
			// Resolved once and compared by symbol identity. Matching on a display string is guesswork:
			// the format of an open generic's name is not something to rely on.
			var providerTypesBuilder = ImmutableArray.CreateBuilder<INamedTypeSymbol>(ProviderMetadataNames.Length);
			foreach (var metadataName in ProviderMetadataNames)
			{
				if (startContext.Compilation.GetTypeByMetadataName(metadataName) is { } providerType)
					providerTypesBuilder.Add(providerType);
			}

			if (providerTypesBuilder.Count == 0)
				return;

			var providerTypes = providerTypesBuilder.ToImmutable();

			ConcurrentBag<Diagnostic> providerDiagnostics = [];

			startContext.RegisterOperationAction(
				operationContext => AnalyzeProviderValueType(operationContext, providerTypes, providerDiagnostics),
				OperationKind.Invocation
			);

			startContext.RegisterCompilationEndAction(endContext =>
			{
				foreach (var diagnostic in providerDiagnostics)
					endContext.ReportDiagnostic(diagnostic);
			});
		});

		context.RegisterCompilationAction(static context =>
		{
			var compilation = context.Compilation;
			var pipelineTypes = PipelineModelDiscovery.ResolvePipelineTypes(compilation);
			var collectionTypes = PipelineModelDiscovery.ResolveCollectionTypes(compilation);

			if (pipelineTypes.IsEmpty)
				return;

			var modelTypes = PipelineModelDiscovery.CollectPipelineModelTypes(
				compilation,
				pipelineTypes,
				collectionTypes
			);

			foreach (var modelType in modelTypes)
			{
				AnalyzeModelType(context, modelType);
			}
		});
	}

	/// <summary>
	/// Flags an invocation that produces an <c>IncrementalValue(s)Provider&lt;T&gt;</c> whose <c>T</c> retains a
	/// Roslyn object, which is the value the driver caches between runs.
	/// </summary>
	/// <remarks>
	/// Only the call that *introduces* the retained type is reported: if the receiver already carried it,
	/// the earlier link in the chain is the real site and reporting both would say the same thing twice.
	/// </remarks>
	static void AnalyzeProviderValueType(
		OperationAnalysisContext context,
		ImmutableArray<INamedTypeSymbol> providerTypes,
		ConcurrentBag<Diagnostic> diagnostics
	)
	{
		var invocation = (IInvocationOperation)context.Operation;

		var producedValueType = GetProviderValueType(invocation.Type, providerTypes);
		if (producedValueType is null)
			return;

		var retainedType = FindRetainedRoslynType(producedValueType);
		if (retainedType is null)
			return;

		// An extension method's receiver is its first argument; an instance method's is Instance.
		var receiver =
			invocation.Instance
			?? (
				invocation.TargetMethod.IsExtensionMethod && invocation.Arguments.Length > 0
					? invocation.Arguments[0].Value
					: null
			);

		// Only an earlier *invocation* that already carried the retained type is reported instead of this
		// one; otherwise the chain would be reported twice for the same mistake. A receiver that is not an
		// invocation — context.CompilationProvider, say — has no earlier call to report, so this call is
		// the site.
		if (
			receiver is IInvocationOperation
			&& GetProviderValueType(receiver.Type, providerTypes) is { } receivedValueType
			&& FindRetainedRoslynType(receivedValueType) is not null
		)
		{
			return;
		}

		diagnostics.Add(
			Diagnostic.Create(
				Rule,
				invocation.Syntax.GetLocation(),
				invocation.TargetMethod.Name,
				retainedType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)
			)
		);
	}

	/// <summary>
	/// Returns the <c>T</c> of an <c>IncrementalValueProvider&lt;T&gt;</c> or
	/// <c>IncrementalValuesProvider&lt;T&gt;</c>, or <see langword="null"/> for any other type.
	/// </summary>
	static ITypeSymbol? GetProviderValueType(ITypeSymbol? typeSymbol, ImmutableArray<INamedTypeSymbol> providerTypes)
	{
		if (typeSymbol is not INamedTypeSymbol { IsGenericType: true, TypeArguments.Length: 1 } named)
			return null;

		foreach (var providerType in providerTypes)
		{
			if (SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, providerType))
				return named.TypeArguments[0];
		}

		return null;
	}

	static void AnalyzeModelType(CompilationAnalysisContext context, INamedTypeSymbol modelType)
	{
		foreach (var member in modelType.GetMembers())
		{
			if (member.IsImplicitlyDeclared)
				continue;

			var memberType = member switch
			{
				IFieldSymbol field => field.Type,
				IPropertySymbol property => property.Type,
				_ => null,
			};

			if (memberType is null)
				continue;

			var retainedType = FindRetainedRoslynType(memberType);
			if (retainedType is null)
				continue;

			var location = PipelineModelDiscovery.GetMemberLocation(member);
			if (location is null)
				continue;

			context.ReportDiagnostic(
				Diagnostic.Create(
					Rule,
					location,
					member.Name,
					retainedType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)
				)
			);
		}
	}

	/// <summary>
	/// Returns the first non-equatable Roslyn type reachable from <paramref name="typeSymbol"/>, unwrapping
	/// arrays and generic collections.
	/// </summary>
	static INamedTypeSymbol? FindRetainedRoslynType(ITypeSymbol typeSymbol) =>
		typeSymbol switch
		{
			INamedTypeSymbol named => IsNonEquatableRoslynType(named) ? named : FindRetainedElementType(named),
			IArrayTypeSymbol array => FindRetainedRoslynType(array.ElementType),
			_ => null,
		};

	static INamedTypeSymbol? FindRetainedElementType(INamedTypeSymbol named)
	{
		if (!named.IsGenericType)
			return null;

		foreach (var typeArgument in named.TypeArguments)
		{
			if (FindRetainedRoslynType(typeArgument) is { } retained)
				return retained;
		}

		return null;
	}

	/// <summary>
	/// Determines whether <paramref name="typeSymbol"/> is, derives from, or implements one of the
	/// non-equatable Roslyn types.
	/// </summary>
	/// <remarks>
	/// The inheritance walk is load-bearing. Real pipeline code names the derived types — a transform
	/// returns <c>INamedTypeSymbol</c>, not <c>ISymbol</c>, and a field holds a <c>CSharpCompilation</c>,
	/// not a <c>Compilation</c>. Matching only the exact names in <see cref="NonEquatableRoslynTypes"/>
	/// would miss every realistic case.
	/// </remarks>
	static bool IsNonEquatableRoslynType(INamedTypeSymbol typeSymbol)
	{
		for (ITypeSymbol? current = typeSymbol; current is not null; current = current.BaseType)
		{
			if (MatchesNonEquatableName(current))
				return true;
		}

		foreach (var implemented in typeSymbol.AllInterfaces)
		{
			if (MatchesNonEquatableName(implemented))
				return true;
		}

		return false;
	}

	static bool MatchesNonEquatableName(ITypeSymbol typeSymbol)
	{
		var displayName = typeSymbol.ToDisplayString();
		foreach (var nonEquatableType in NonEquatableRoslynTypes)
		{
			if (displayName == nonEquatableType)
				return true;
		}

		return false;
	}
}
