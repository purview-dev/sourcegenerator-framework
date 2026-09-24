using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Purview.SourceGeneratorFramework.Analyzers;

namespace Purview.SourceGeneratorFramework.CodeFixers;

/// <summary>
/// Qualifies SGF XML documentation cref targets with their <c>global::</c> name.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(QualifyFrameworkCrefCodeFixProvider))]
public sealed class QualifyFrameworkCrefCodeFixProvider : CodeFixProvider
{
	internal const string EquivalenceKey = "QualifyFrameworkCref";

	public override ImmutableArray<string> FixableDiagnosticIds => [AmbiguousFrameworkCrefAnalyzer.DiagnosticId];

	public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

	public override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
		if (root is null)
			return;

		foreach (var diagnostic in context.Diagnostics)
		{
			if (
				!diagnostic.Properties.TryGetValue(
					AmbiguousFrameworkCrefAnalyzer.QualifiedTypePropertyName,
					out var qualifiedTypeName
				) || string.IsNullOrWhiteSpace(qualifiedTypeName)
			)
				continue;

			var resolvedQualifiedTypeName = qualifiedTypeName!;

			var node = root.FindNode(
				diagnostic.Location.SourceSpan,
				getInnermostNodeForTie: true,
				findInsideTrivia: true
			);
			var crefAttribute =
				node as XmlCrefAttributeSyntax
				?? node.AncestorsAndSelf().OfType<XmlCrefAttributeSyntax>().FirstOrDefault();
			if (crefAttribute is null)
				continue;

			context.RegisterCodeFix(
				CodeAction.Create(
					"Qualify SGF cref with global::",
					_ => QualifyCrefAsync(context.Document, root, crefAttribute, resolvedQualifiedTypeName),
					EquivalenceKey
				),
				diagnostic
			);
		}
	}

	static Task<Document> QualifyCrefAsync(
		Document document,
		SyntaxNode root,
		XmlCrefAttributeSyntax crefAttribute,
		string qualifiedTypeName
	)
	{
		var replacement = crefAttribute
			.WithCref(SyntaxFactory.TypeCref(SyntaxFactory.ParseTypeName(qualifiedTypeName)))
			.WithTriviaFrom(crefAttribute);

		var newRoot = root.ReplaceNode(crefAttribute, replacement);
		return Task.FromResult(document.WithSyntaxRoot(newRoot));
	}
}
