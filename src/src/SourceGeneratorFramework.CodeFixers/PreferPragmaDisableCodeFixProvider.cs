using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Purview.SourceGeneratorFramework.Analyzers;

namespace Purview.SourceGeneratorFramework.CodeFixers;

/// <summary>
/// Rewrites a raw <c>#pragma warning disable</c> text emission to <c>CodeWriter.PragmaDisable</c>
/// (<c>PSGFR22</c>). A <c>#pragma warning restore</c> has no single-call equivalent because the
/// structured API owns the restore through <c>OpenPragmasScope</c>, so that half is left unchanged.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(PreferPragmaDisableCodeFixProvider))]
public sealed class PreferPragmaDisableCodeFixProvider : CodeFixProvider
{
	internal const string EquivalenceKey = "UsePragmaDisable";

	const string DisableDirective = "#pragma warning disable";

	public override ImmutableArray<string> FixableDiagnosticIds => ["PSGFR22"];

	public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

	public override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
		var semanticModel = await context
			.Document.GetSemanticModelAsync(context.CancellationToken)
			.ConfigureAwait(false);
		if (root is null || semanticModel is null)
			return;

		foreach (var diagnostic in context.Diagnostics)
		{
			var node = root.FindNode(diagnostic.Location.SourceSpan);
			if (node is not InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax member } invocation)
				continue;

			if (invocation.ArgumentList.Arguments.FirstOrDefault()?.Expression is not ExpressionSyntax expression)
				continue;

			if (
				expression is InterpolatedStringExpressionSyntax
				&& !semanticModel.GetConstantValue(expression, context.CancellationToken).HasValue
			)
				continue;

			if (
				!CodeWriterLiteralClassifier.TryGetLiteralText(
					expression,
					semanticModel,
					context.CancellationToken,
					out var text
				)
				|| text is null
				|| !TryGetCodes(text, out var codes)
			)
				continue;

			var newMemberAccess = SyntaxFactory
				.MemberAccessExpression(
					SyntaxKind.SimpleMemberAccessExpression,
					member.Expression,
					SyntaxFactory.IdentifierName("PragmaDisable")
				)
				.WithTriviaFrom(member);

			var newArguments = SyntaxFactory.ArgumentList(
				SyntaxFactory.SeparatedList(
					codes.Select(code =>
						SyntaxFactory.Argument(
							SyntaxFactory.LiteralExpression(
								SyntaxKind.StringLiteralExpression,
								SyntaxFactory.Literal(code)
							)
						)
					)
				)
			);

			var replacement = SyntaxFactory
				.InvocationExpression(newMemberAccess, newArguments)
				.WithTriviaFrom(invocation);

			context.RegisterCodeFix(
				CodeAction.Create(
					"Use PragmaDisable()",
					_ => Task.FromResult(context.Document.WithSyntaxRoot(root.ReplaceNode(invocation, replacement))),
					EquivalenceKey
				),
				diagnostic
			);
		}
	}

	static bool TryGetCodes(string text, out string[] codes)
	{
		codes = [];

		var trimmed = text.Trim();
		if (!trimmed.StartsWith(DisableDirective, StringComparison.Ordinal))
			return false;

		var rest = trimmed.Substring(DisableDirective.Length).Trim();
		var commentIndex = rest.IndexOf("//", StringComparison.Ordinal);
		if (commentIndex >= 0)
			rest = rest.Substring(0, commentIndex).Trim();

		codes = rest.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
		return codes.Length != 0;
	}
}
