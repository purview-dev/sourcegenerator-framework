using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Purview.SourceGeneratorFramework.Analyzers;

/// <summary>
/// Flags block- and scope-opening <c>CodeWriter</c> methods whose header writes a conditional
/// statement, suggesting the structured <c>IfBlock</c>, <c>ElseIf</c>, or <c>Else</c> API instead.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PreferStructuredCodeWriterIfBlockAnalyzer : DiagnosticAnalyzer
{
	public const string DiagnosticId = "PSGFR23";

	public static readonly DiagnosticDescriptor Rule = new(
		DiagnosticId,
		"Prefer the structured CodeWriter conditional API",
		"'{0}' with a conditional block should use '{1}'",
		"Purview.SourceGeneratorFramework",
		DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		description: "Emitting an if, else if, or else block through a generic block method bypasses the structured IfBlock, ElseIf, and Else APIs on CodeWriter."
	);

	static readonly string[] BlockMethods =
	[
		"OpenBlockScope",
		"OpenDelimitedBlockScope",
		"OpenBlock",
		"OpenDelimitedBlock",
		"Block",
		"DelimitedBlock",
	];

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

		var name = member.Name.Identifier.Text;
		if (Array.IndexOf(BlockMethods, name) < 0)
			return;

		if (invocation.ArgumentList.Arguments.FirstOrDefault()?.Expression is not ExpressionSyntax expression)
			return;

		if (
			context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol
			is not IMethodSymbol method
		)
			return;

		if (method.ContainingType?.ToDisplayString() != "Purview.SourceGeneratorFramework.CodeWriter")
			return;

		if (
			!CodeWriterLiteralClassifier.TryGetLiteralText(
				expression,
				context.SemanticModel,
				context.CancellationToken,
				out var value
			)
		)
			return;

		var structuredApi = CodeWriterLiteralClassifier.ClassifyBlockHeader(
			value,
			isScopeForm: name.EndsWith("Scope", StringComparison.Ordinal)
		);
		if (structuredApi is null)
			return;

		context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation(), name, structuredApi));
	}
}
