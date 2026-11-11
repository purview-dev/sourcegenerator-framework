using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Purview.SourceGeneratorFramework.CodeFixers;

/// <summary>
/// Removes a <c>FixableDiagnosticIds</c> entry that no analyzer in the compilation produces
/// (fixes <c>PSGFR28</c>).
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RemoveOrphanedFixableDiagnosticIdCodeFixProvider))]
public sealed class RemoveOrphanedFixableDiagnosticIdCodeFixProvider : CodeFixProvider
{
	internal const string EquivalenceKey = "RemoveOrphanedDiagnostic";

	public override ImmutableArray<string> FixableDiagnosticIds => ["PSGFR28"];

	public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

	public override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
		if (root is null)
			return;

		foreach (var diagnostic in context.Diagnostics)
		{
			var node = root.FindNode(diagnostic.Location.SourceSpan);
			var literal = node.DescendantNodesAndSelf().OfType<LiteralExpressionSyntax>().FirstOrDefault()
				?? node.FirstAncestorOrSelf<LiteralExpressionSyntax>();
			if (literal is null)
				continue;

			context.RegisterCodeFix(
				CodeAction.Create(
					"Remove unused diagnostic ID",
					_ => RemoveOrphanedIdAsync(context.Document, literal, context.CancellationToken),
					EquivalenceKey
				),
				diagnostic
			);
		}
	}

	static async Task<Document> RemoveOrphanedIdAsync(
		Document document,
		LiteralExpressionSyntax literal,
		CancellationToken cancellationToken
	)
	{
		var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
		if (root is null)
			return document;

		SyntaxNode removable = literal.Parent is ExpressionElementSyntax element ? element : literal;
		var updatedRoot = root.RemoveNode(removable, SyntaxRemoveOptions.KeepNoTrivia);

		return updatedRoot is null ? document : document.WithSyntaxRoot(updatedRoot);
	}
}
