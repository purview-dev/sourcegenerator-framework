using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Purview.SourceGeneratorFramework.CodeFixers;

/// <summary>
/// Rewrites a structured <c>CodeWriter</c> declaration call whose only argument is a bare
/// <c>*DeclarationOptions</c> construction to the minimal overload that takes those primary-constructor
/// arguments directly (<c>PSGFR20</c>).
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(PreferMinimalCodeWriterOverloadCodeFixProvider))]
public sealed class PreferMinimalCodeWriterOverloadCodeFixProvider : CodeFixProvider
{
	internal const string EquivalenceKey = "UseMinimalOverload";

	public override ImmutableArray<string> FixableDiagnosticIds => ["PSGFR20"];

	public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

	public override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
		if (root is null)
			return;

		foreach (var diagnostic in context.Diagnostics)
		{
			var node = root.FindNode(diagnostic.Location.SourceSpan);
			if (node is not InvocationExpressionSyntax invocation)
				continue;

			if (invocation.ArgumentList.Arguments.Count != 1)
				continue;

			var arguments = invocation.ArgumentList.Arguments[0].Expression switch
			{
				ObjectCreationExpressionSyntax { Initializer: null } creation => creation.ArgumentList,
				ImplicitObjectCreationExpressionSyntax { Initializer: null } creation => creation.ArgumentList,
				_ => null,
			};
			if (arguments is null || arguments.Arguments.Count == 0)
				continue;

			var newArgumentList = arguments.WithTriviaFrom(invocation.ArgumentList);
			var replacement = invocation.WithArgumentList(newArgumentList);

			context.RegisterCodeFix(
				CodeAction.Create(
					"Use the minimal overload",
					_ => Task.FromResult(context.Document.WithSyntaxRoot(root.ReplaceNode(invocation, replacement))),
					EquivalenceKey
				),
				diagnostic
			);
		}
	}
}
