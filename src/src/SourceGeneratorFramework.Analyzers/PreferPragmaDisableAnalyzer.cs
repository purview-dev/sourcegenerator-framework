using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Purview.SourceGeneratorFramework.Analyzers;

/// <summary>
/// Flags raw <c>CodeWriter</c> text emission of a <c>#pragma warning disable</c>/<c>restore</c>
/// directive, suggesting <c>PragmaDisable</c> or <c>OpenPragmasScope</c> instead.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PreferPragmaDisableAnalyzer : DiagnosticAnalyzer
{
	public const string DiagnosticId = "PSGFR22";

	public static readonly DiagnosticDescriptor Rule = new(
		DiagnosticId,
		"Prefer PragmaDisable for warning suppression",
		"'{0}' with a pragma warning directive should use 'PragmaDisable' or 'OpenPragmasScope'",
		"Purview.SourceGeneratorFramework",
		DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		description: "Emitting #pragma warning directives through raw text bypasses the structured PragmaDisable/OpenPragmasScope APIs."
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

		if (value is null || !CodeWriterLiteralClassifier.IsPragmaWarningDirective(value))
			return;

		context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation(), name));
	}
}
