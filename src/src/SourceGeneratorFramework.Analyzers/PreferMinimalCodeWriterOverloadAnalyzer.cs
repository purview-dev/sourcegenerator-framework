using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Purview.SourceGeneratorFramework.Analyzers;

/// <summary>
/// Flags a structured <c>CodeWriter</c> declaration call that constructs a <c>*DeclarationOptions</c>
/// value using only its primary-constructor arguments, suggesting the minimal overload that takes those
/// arguments directly.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PreferMinimalCodeWriterOverloadAnalyzer : DiagnosticAnalyzer
{
	public const string DiagnosticId = "PSGFR20";

	public static readonly DiagnosticDescriptor Rule = new(
		DiagnosticId,
		"Prefer the minimal CodeWriter overload",
		"'{0}' should use the minimal overload '{1}'",
		"Purview.SourceGeneratorFramework",
		DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		description: "Constructing a declaration options value from only its primary-constructor arguments can be replaced by the minimal overload that takes those arguments directly."
	);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

	public override void Initialize(AnalysisContext context)
	{
		if (context is null)
			throw new ArgumentNullException(nameof(context));

		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
	}

	static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
	{
		if (
			context.Node
			is not InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax member } invocation
		)
			return;

		if (invocation.ArgumentList.Arguments.Count != 1)
			return;

		var argumentExpression = invocation.ArgumentList.Arguments[0].Expression;
		if (argumentExpression is not (ObjectCreationExpressionSyntax or ImplicitObjectCreationExpressionSyntax))
			return;

		// An object-initializer carries configuration the minimal overload cannot express.
		if (HasObjectInitializer(argumentExpression))
			return;

		var semanticModel = context.SemanticModel;
		if (semanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol is not IMethodSymbol method)
			return;

		if (method.ContainingType?.ToDisplayString() != "Purview.SourceGeneratorFramework.CodeWriter")
			return;

		if (
			semanticModel.GetSymbolInfo(argumentExpression, context.CancellationToken).Symbol
			is not IMethodSymbol constructor
		)
			return;

		var constructorParameters = constructor.Parameters;
		if (constructorParameters.Length == 0)
			return;

		var methodName = member.Name.Identifier.Text;
		var suggestion = FindMinimalOverload(method, methodName, constructorParameters);
		if (suggestion is null)
			return;

		context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation(), methodName, suggestion));
	}

	static string? FindMinimalOverload(
		IMethodSymbol invoked,
		string methodName,
		ImmutableArray<IParameterSymbol> constructorParameters
	)
	{
		foreach (var candidate in invoked.ContainingType.GetMembers(methodName).OfType<IMethodSymbol>())
		{
			if (
				candidate.Parameters.Length != constructorParameters.Length
				&& !(
					candidate.Parameters.Length == constructorParameters.Length + 1
					&& IsConfigureParameter(candidate.Parameters[candidate.Parameters.Length - 1])
				)
			)
				continue;

			var matches = true;
			for (var index = 0; index < constructorParameters.Length; index++)
			{
				if (
					!SymbolEqualityComparer.Default.Equals(
						candidate.Parameters[index].Type,
						constructorParameters[index].Type
					)
				)
				{
					matches = false;
					break;
				}
			}

			if (!matches)
				continue;

			var arguments = string.Join(", ", constructorParameters.Select(static parameter => parameter.Name));
			return $"{methodName}({arguments})";
		}

		return null;
	}

	static bool IsConfigureParameter(IParameterSymbol parameter) =>
		parameter.Type is INamedTypeSymbol { TypeArguments.Length: 2 } named
		&& named.Name == "Func"
		&& named.ContainingNamespace?.Name == "System";

	static bool HasObjectInitializer(SyntaxNode expression) =>
		expression switch
		{
			ObjectCreationExpressionSyntax { Initializer.Expressions.Count: > 0 } => true,
			ImplicitObjectCreationExpressionSyntax { Initializer.Expressions.Count: > 0 } => true,
			_ => false,
		};
}
