using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Purview.SourceGeneratorFramework.CodeFixers;

/// <summary>
/// Turns an undisposed test-harness result declaration into a <c>using</c> declaration, so the compiled
/// assembly and its load contexts are released when the test method returns.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DisposeTestResultCodeFixProvider))]
public sealed class DisposeTestResultCodeFixProvider : CodeFixProvider
{
	internal const string EquivalenceKey = "DisposeTestResult";

	public override ImmutableArray<string> FixableDiagnosticIds => ["PSGFR42"];

	public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

	public override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
		if (root is null)
			return;

		foreach (var diagnostic in context.Diagnostics)
		{
			var node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
			if (node.FirstAncestorOrSelf<LocalDeclarationStatementSyntax>() is not { } declaration)
				continue;

			// A multi-declarator statement cannot become a using declaration without splitting it.
			if (declaration.Declaration.Variables.Count != 1 || !declaration.UsingKeyword.IsKind(SyntaxKind.None))
				continue;

			context.RegisterCodeFix(
				CodeAction.Create(
					"Consume the result with a using declaration",
					_ => AddUsingAsync(context.Document, root, declaration),
					EquivalenceKey
				),
				diagnostic
			);
		}
	}

	static Task<Document> AddUsingAsync(Document document, SyntaxNode root, LocalDeclarationStatementSyntax declaration)
	{
		// The using keyword takes the statement's leading trivia so the indentation is preserved.
		var usingToken = SyntaxFactory
			.Token(SyntaxKind.UsingKeyword)
			.WithLeadingTrivia(declaration.GetLeadingTrivia())
			.WithTrailingTrivia(SyntaxFactory.Space);

		var updated = declaration.WithLeadingTrivia(SyntaxFactory.TriviaList()).WithUsingKeyword(usingToken);

		return Task.FromResult(document.WithSyntaxRoot(root.ReplaceNode(declaration, updated)));
	}
}
