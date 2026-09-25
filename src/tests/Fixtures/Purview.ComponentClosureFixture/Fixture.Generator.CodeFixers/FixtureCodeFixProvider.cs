using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;

namespace Fixture.Generator.CodeFixers;

/// <summary>
/// Minimal code-fix provider that resolves its diagnostic id from the referenced generator
/// assembly, forcing a real runtime dependency between the two components.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(FixtureCodeFixProvider))]
public sealed class FixtureCodeFixProvider : CodeFixProvider
{
	/// <inheritdoc />
	public override ImmutableArray<string> FixableDiagnosticIds =>
		[global::Fixture.Generator.FixtureComponentDiagnostics.DiagnosticId];

	/// <inheritdoc />
	public override FixAllProvider? GetFixAllProvider() => null;

	/// <inheritdoc />
	public override Task RegisterCodeFixesAsync(CodeFixContext context) => Task.CompletedTask;
}
