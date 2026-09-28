using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Purview.SourceGeneratorFramework.Analyzers;

namespace Purview.SourceGeneratorFramework.CodeFixers;

/// <summary>
/// Makes the member (or the declaring type) that exposes a framework type non-public, so the merged
/// analyzer does not carry a public signature over an internalized framework type (fixes
/// <c>PSGFR41</c>).
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MakeComponentSurfaceNonPublicCodeFixProvider))]
public sealed class MakeComponentSurfaceNonPublicCodeFixProvider : CodeFixProvider
{
	internal const string MemberEquivalenceKey = "MakeMemberNonPublic";
	internal const string TypeEquivalenceKey = "MakeTypeNonPublic";

	public override ImmutableArray<string> FixableDiagnosticIds => [ComponentPublicSurfaceAnalyzer.DiagnosticId];

	public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

	public override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
		if (root is null)
			return;

		foreach (var diagnostic in context.Diagnostics)
		{
			var node = root.FindNode(diagnostic.Location.SourceSpan);
			if (node.FirstAncestorOrSelf<MemberDeclarationSyntax>() is not { } declaration)
				continue;

			// A base type, interface or generic constraint finding points at the type itself.
			if (declaration is BaseTypeDeclarationSyntax)
			{
				context.RegisterCodeFix(
					CodeAction.Create(
						"Make the type non-public",
						_ => MakeNonPublicAsync(context.Document, declaration, context.CancellationToken),
						TypeEquivalenceKey
					),
					diagnostic
				);
				continue;
			}

			context.RegisterCodeFix(
				CodeAction.Create(
					"Make the member non-public",
					_ => MakeNonPublicAsync(context.Document, declaration, context.CancellationToken),
					MemberEquivalenceKey
				),
				diagnostic
			);

			if (
				declaration.FirstAncestorOrSelf<MemberDeclarationSyntax>(static node =>
					node is BaseTypeDeclarationSyntax
				) is
				{ } containingType
			)
			{
				context.RegisterCodeFix(
					CodeAction.Create(
						"Make the containing type non-public",
						_ => MakeNonPublicAsync(context.Document, containingType, context.CancellationToken),
						TypeEquivalenceKey
					),
					diagnostic
				);
			}
		}
	}

	static async Task<Document> MakeNonPublicAsync(
		Document document,
		MemberDeclarationSyntax declaration,
		CancellationToken cancellationToken
	)
	{
		var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
		if (root is null)
			return document;

		// The declaration may have been removed or replaced by another code fix, so find the current node.
		return document.WithSyntaxRoot(
			root.ReplaceNode(declaration, RoslynComponentFixHelpers.MakeInternal(declaration))
		);
	}
}
