using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Purview.SourceGeneratorFramework.Analyzers;

/// <summary>
/// Flags raw <c>CodeWriter</c> text emission that writes a C# statement, suggesting a structured
/// statement API such as <c>Return</c>, <c>MethodCall</c>, <c>Throw</c>,
/// <c>Assignment</c>, <c>Using</c>, or <c>Comment</c> instead.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PreferStructuredCodeWriterStatementAnalyzer : DiagnosticAnalyzer
{
	public const string DiagnosticId = "PSGFR19";

	public static readonly DiagnosticDescriptor Rule = new(
		DiagnosticId,
		"Prefer a structured CodeWriter statement API",
		"'{0}' with a statement should use '{1}'",
		"Purview.SourceGeneratorFramework",
		DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		description: "Emitting statement syntax through raw text bypasses the structured, deterministic statement APIs on CodeWriter."
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

		var name = member.Name.Identifier.Text;
		if (name is not ("Write" or "Line" or "Append" or "AppendLine" or "MultiLine"))
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

		if (value is null)
			return;

		var structuredApi = CodeWriterLiteralClassifier.ClassifyStatement(value);
		if (structuredApi is null)
			return;

		context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation(), name, structuredApi));
	}
}
