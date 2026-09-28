using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Purview.SourceGeneratorFramework.Analyzers;

namespace Purview.SourceGeneratorFramework.CodeFixers;

/// <summary>
/// Declares a type-library spec non-public in a component whose framework implementation is merged, so
/// the generated marker member does not leave a public signature over an internalized framework type
/// (fixes <c>TLB0021</c>).
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MakeTypeLibrarySpecNonPublicCodeFixProvider))]
public sealed class MakeTypeLibrarySpecNonPublicCodeFixProvider : CodeFixProvider
{
	internal const string EquivalenceKey = "MakeTypeLibrarySpecNonPublic";

	public override ImmutableArray<string> FixableDiagnosticIds =>
		[TypeLibraryValidationAnalyzer.SpecShouldBeNonPublic.Id];

	public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

	public override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
		if (root is null)
			return;

		foreach (var diagnostic in context.Diagnostics)
		{
			var node = root.FindNode(diagnostic.Location.SourceSpan);
			if (node.FirstAncestorOrSelf<TypeDeclarationSyntax>() is not { } typeDeclaration)
				continue;

			context.RegisterCodeFix(
				CodeAction.Create(
					"Make the spec non-public",
					_ => MakeNonPublicAsync(context.Document, typeDeclaration, context.CancellationToken),
					EquivalenceKey
				),
				diagnostic
			);
		}
	}

	static async Task<Document> MakeNonPublicAsync(
		Document document,
		TypeDeclarationSyntax typeDeclaration,
		CancellationToken cancellationToken
	)
	{
		var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
		if (root is null)
			return document;

		// The type declaration is already non-public, so no fix is needed.
		return document.WithSyntaxRoot(
			root.ReplaceNode(typeDeclaration, RoslynComponentFixHelpers.MakeInternal(typeDeclaration))
		);
	}
}
