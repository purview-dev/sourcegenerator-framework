using System.Collections.Immutable;
using Purview.SourceGeneratorFramework.Analyzers;
using Purview.SourceGeneratorFramework.Testing;
using Purview.SourceGeneratorFramework.Testing.TUnit;

namespace Purview.SourceGeneratorFramework.CodeFixers;

public sealed class PreferInlineCodeForFrameworkCrefCodeFixProviderTests
	: TUnitCodeFixTestBase<UnqualifiedFrameworkCrefAnalyzer, PreferInlineCodeForFrameworkCrefCodeFixProvider>
{
	static CodeFixTestOptions Options =>
		new()
		{
			EquivalenceKey = PreferInlineCodeForFrameworkCrefCodeFixProvider.EquivalenceKey,
			AnalyzerConfigOptions = new Dictionary<string, string>
			{
				["build_property.IsRoslynComponent"] = "true",
			}.ToImmutableDictionary(),
			AdditionalAssemblyTypes = [typeof(CodeWriter), typeof(TypeReference)],
		};

	[Test]
	public async Task BareTypeReferenceCref_BecomesInlineCode(CancellationToken cancellationToken)
	{
		const string source = """
			using Purview.SourceGeneratorFramework;

			/// <summary>
			/// Shared <see cref="TypeReference"/> building blocks.
			/// </summary>
			public static class TypeRefs
			{
			}
			""";

		var result = await ApplyCodeFixAsync(source, Options, cancellationToken);

		await Assert.That(result).HasDiagnostic(UnqualifiedFrameworkCrefAnalyzer.DiagnosticId);
		await Assert.That(result.FixedSource).Contains("<c>TypeReference</c>");
	}

	[Test]
	public async Task BareCodeWriterCref_BecomesInlineCode(CancellationToken cancellationToken)
	{
		const string source = """
			using Purview.SourceGeneratorFramework;

			/// <summary>
			/// Writes with <see cref="CodeWriter"/>.
			/// </summary>
			public static class TypeRefs
			{
			}
			""";

		var result = await ApplyCodeFixAsync(source, Options, cancellationToken);

		await Assert.That(result).HasDiagnostic(UnqualifiedFrameworkCrefAnalyzer.DiagnosticId);
		await Assert.That(result.FixedSource).Contains("<c>CodeWriter</c>");
	}

	[Test]
	public async Task CrefWithContent_KeepsAuthorText(CancellationToken cancellationToken)
	{
		const string source = """
			using Purview.SourceGeneratorFramework;

			/// <summary>
			/// Shared <see cref="TypeReference">the reference value</see> building blocks.
			/// </summary>
			public static class TypeRefs
			{
			}
			""";

		var result = await ApplyCodeFixAsync(source, Options, cancellationToken);

		await Assert.That(result.FixedSource).Contains("<c>the reference value</c>");
	}

	[Test]
	public async Task FixAll_ConvertsMultipleCrefs(CancellationToken cancellationToken)
	{
		const string source = """
			using Purview.SourceGeneratorFramework;

			/// <summary>
			/// Shared <see cref="TypeReference"/> building blocks.
			/// Writes with <see cref="CodeWriter"/>.
			/// </summary>
			public static class TypeRefs
			{
			}
			""";

		var result = await ApplyFixAllAsync(source, Options, cancellationToken);

		await Assert
			.That(result.Diagnostics.Select(static d => d.Id).ToArray())
			.Contains(UnqualifiedFrameworkCrefAnalyzer.DiagnosticId);
		await Assert.That(result.Diagnostics).Count().IsEqualTo(2);
		await Assert.That(result.FixedSources["Test1.cs"]).Contains("<c>TypeReference</c>");
		await Assert.That(result.FixedSources["Test1.cs"]).Contains("<c>CodeWriter</c>");
	}
}
