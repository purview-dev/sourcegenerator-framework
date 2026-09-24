using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Purview.SourceGeneratorFramework.Analyzers;

namespace Purview.SourceGeneratorFramework.CodeFixers;

/// <summary>
/// Replaces an SGF XML documentation cref (<c>&lt;see cref="TypeReference"/&gt;</c>) with inline code
/// (<c>&lt;c&gt;TypeReference&lt;/c&gt;</c>). Inline code carries the type as text, so the documentation
/// stays valid when the framework's generators copy it into generated files whose namespace and using
/// set differ from the author's source.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(PreferInlineCodeForFrameworkCrefCodeFixProvider))]
public sealed class PreferInlineCodeForFrameworkCrefCodeFixProvider : CodeFixProvider
{
	internal const string EquivalenceKey = "PreferInlineCodeForSgfType";

	public override ImmutableArray<string> FixableDiagnosticIds => [UnqualifiedFrameworkCrefAnalyzer.DiagnosticId];

	// A single-pass fix-all: the cref element is replaced in one rewrite, so no fix is reapplied against
	// a document whose spans have already shifted.
	public override FixAllProvider GetFixAllProvider() => InlineCodeFixAllProvider.Instance;

	public override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
		if (root is null)
			return;

		foreach (var diagnostic in context.Diagnostics)
		{
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

			var inlineText = crefAttribute.Cref.ToString();
			var crefSpan = crefAttribute.Span;
			context.RegisterCodeFix(
				CodeAction.Create(
					$"Use <c>{inlineText}</c>",
					cancellationToken => UseInlineCodeAsync(context.Document, crefSpan, inlineText, cancellationToken),
					EquivalenceKey
				),
				diagnostic
			);
		}
	}

	static async Task<Document> UseInlineCodeAsync(
		Document document,
		TextSpan crefSpan,
		string inlineText,
		CancellationToken cancellationToken
	)
	{
		// Resolve the cref against the current document: the batch fixer applies fixes one after
		// another (latest span first), so a root captured while registering would be stale.
		var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
		if (root is null)
			return document;

		var node = root.FindNode(crefSpan, getInnermostNodeForTie: true, findInsideTrivia: true);
		var crefAttribute =
			node as XmlCrefAttributeSyntax ?? node.AncestorsAndSelf().OfType<XmlCrefAttributeSyntax>().FirstOrDefault();
		if (crefAttribute is null)
			return document;

		// The cref attribute lives on the <see>/<seealso> element; inline code replaces the element.
		var owner =
			crefAttribute
				.AncestorsAndSelf()
				.FirstOrDefault(static ancestor => ancestor is XmlElementSyntax or XmlEmptyElementSyntax)
			as XmlNodeSyntax;
		if (owner is null)
			return document;

		return document.WithSyntaxRoot(root.ReplaceNode(owner, BuildInlineCodeElement(owner, inlineText)));
	}

	static XmlElementSyntax BuildInlineCodeElement(XmlNodeSyntax owner, string inlineText)
	{
		var name = SyntaxFactory.XmlName(SyntaxFactory.Identifier("c"));

		// `<see cref="X">shown</see>` keeps the author's text; an empty element shows the type name.
		var content =
			owner is XmlElementSyntax element && element.Content.Any(static node => !IsWhitespace(node))
				? element.Content
				: SyntaxFactory.SingletonList<XmlNodeSyntax>(SyntaxFactory.XmlText(inlineText));

		return SyntaxFactory.XmlElement(name, content).WithTriviaFrom(owner);
	}

	static bool IsWhitespace(XmlNodeSyntax node) =>
		node is XmlTextSyntax text && text.TextTokens.All(static token => string.IsNullOrWhiteSpace(token.ValueText));

	/// <summary>
	/// Rewrites every reported SGF cref in a document in a single pass.
	/// </summary>
	sealed class InlineCodeFixAllProvider : FixAllProvider
	{
		public static readonly InlineCodeFixAllProvider Instance = new();

		public override Task<CodeAction?> GetFixAsync(FixAllContext fixAllContext)
		{
			var document = fixAllContext.Document ?? fixAllContext.Project.Documents.FirstOrDefault();
			if (document is null)
				return Task.FromResult<CodeAction?>(null);

			return Task.FromResult<CodeAction?>(
				CodeAction.Create(
					"Use inline code for SGF cref references",
					cancellationToken => FixAllAsync(document, fixAllContext, cancellationToken),
					EquivalenceKey
				)
			);
		}

		static async Task<Document> FixAllAsync(
			Document document,
			FixAllContext fixAllContext,
			CancellationToken cancellationToken
		)
		{
			var diagnostics = await fixAllContext.GetDocumentDiagnosticsAsync(document).ConfigureAwait(false);
			if (diagnostics.IsEmpty)
				return document;

			var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
			if (root is null)
				return document;

			List<XmlNodeSyntax> owners = [];
			foreach (var diagnostic in diagnostics)
			{
				var node = root.FindNode(
					diagnostic.Location.SourceSpan,
					getInnermostNodeForTie: true,
					findInsideTrivia: true
				);
				var owner =
					node.AncestorsAndSelf()
						.FirstOrDefault(static ancestor => ancestor is XmlElementSyntax or XmlEmptyElementSyntax)
					as XmlNodeSyntax;

				if (owner is not null)
					owners.Add(owner);
			}

			if (owners.Count == 0)
				return document;

			return document.WithSyntaxRoot(
				root.ReplaceNodes(
					owners,
					static (original, _) => BuildInlineCodeElement(original, GetCrefText(original))
				)
			);
		}

		static string GetCrefText(XmlNodeSyntax owner) =>
			owner.DescendantNodesAndSelf().OfType<XmlCrefAttributeSyntax>().FirstOrDefault()?.Cref.ToString()
			?? string.Empty;
	}
}
