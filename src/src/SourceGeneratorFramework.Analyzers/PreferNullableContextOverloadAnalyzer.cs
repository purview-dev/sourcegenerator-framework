using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Purview.SourceGeneratorFramework.Analyzers;

/// <summary>
/// Suggests passing the <c>GenerationSettings</c> or <c>CodeWriter</c> to
/// <c>Nullable()</c>/<c>MakeNullable()</c> so a nullable annotation is emitted only when the target
/// compilation supports nullable.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PreferNullableContextOverloadAnalyzer : DiagnosticAnalyzer
{
	public const string DiagnosticId = "PSGFR16";

	public static readonly DiagnosticDescriptor Rule = new(
		DiagnosticId,
		"Prefer the nullable-context overload",
		"Pass GenerationSettings or CodeWriter to {0}() so the nullable annotation is emitted only when the target compilation supports it",
		"Purview.SourceGeneratorFramework",
		DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		description: "Use Nullable(GenerationSettings)/Nullable(CodeWriter) or MakeNullable(GenerationSettings)/MakeNullable(CodeWriter) when a generation context is available."
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
		var invocation = (InvocationExpressionSyntax)context.Node;
		if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
			return;

		var methodName = memberAccess.Name.Identifier.Text;
		if (methodName is not ("Nullable" or "MakeNullable"))
			return;

		if (invocation.ArgumentList.Arguments.Count != 0)
			return;

		var symbol = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol;
		if (symbol is not IMethodSymbol method)
			return;

		var containingType = method.ContainingType?.ToDisplayString();
		if (
			containingType
			is not ("Purview.SourceGeneratorFramework.TypeReference" or "Purview.SourceGeneratorFramework.TypeIdentity")
		)
			return;

		context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation(), methodName));
	}
}
