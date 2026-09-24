using System.Collections.Immutable;
using Purview.SourceGeneratorFramework.Analyzers;
using Purview.SourceGeneratorFramework.Testing;
using Purview.SourceGeneratorFramework.Testing.TUnit;

namespace Purview.SourceGeneratorFramework.CodeFixers;

public sealed class QualifyFrameworkCrefCodeFixProviderTests
	: TUnitCodeFixTestBase<AmbiguousFrameworkCrefAnalyzer, QualifyFrameworkCrefCodeFixProvider>
{
	static CodeFixTestOptions Options =>
		new()
		{
			EquivalenceKey = QualifyFrameworkCrefCodeFixProvider.EquivalenceKey,
			AnalyzerConfigOptions = new Dictionary<string, string>
			{
				["build_property.IsRoslynComponent"] = "true",
			}.ToImmutableDictionary(),
			AdditionalAssemblyTypes = [typeof(CodeWriter), typeof(TypeReference)],
		};

	[Test]
	public async Task BareTypeReferenceCref_IsQualified(CancellationToken cancellationToken)
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

		await Assert.That(result).HasDiagnostic(AmbiguousFrameworkCrefAnalyzer.DiagnosticId);
		await Assert
			.That(result.FixedSource)
			.Contains("<see cref=\"global::Purview.SourceGeneratorFramework.TypeReference\"/>");
	}

	[Test]
	public async Task BareCodeWriterCref_IsQualified(CancellationToken cancellationToken)
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

		await Assert.That(result).HasDiagnostic(AmbiguousFrameworkCrefAnalyzer.DiagnosticId);
		await Assert
			.That(result.FixedSource)
			.Contains("<see cref=\"global::Purview.SourceGeneratorFramework.CodeWriter\"/>");
	}

	[Test]
	public async Task FixAll_QualifiesMultipleCrefs(CancellationToken cancellationToken)
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
			.Contains(AmbiguousFrameworkCrefAnalyzer.DiagnosticId);
		await Assert
			.That(result.FixedSources["Test1.cs"])
			.Contains("<see cref=\"global::Purview.SourceGeneratorFramework.TypeReference\"/>");
		await Assert
			.That(result.FixedSources["Test1.cs"])
			.Contains("<see cref=\"global::Purview.SourceGeneratorFramework.CodeWriter\"/>");
	}
}
