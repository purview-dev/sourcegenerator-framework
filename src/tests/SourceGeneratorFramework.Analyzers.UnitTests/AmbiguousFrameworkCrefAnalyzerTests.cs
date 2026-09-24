using System.Collections.Immutable;
using Purview.SourceGeneratorFramework.Testing;
using Purview.SourceGeneratorFramework.Testing.TUnit;

namespace Purview.SourceGeneratorFramework.Analyzers;

public sealed class AmbiguousFrameworkCrefAnalyzerTests
	: TUnitDiagnosticAnalyzerTestBase<AmbiguousFrameworkCrefAnalyzer>
{
	static readonly AnalyzerTestOptions RoslynComponentOptions = new()
	{
		AnalyzerConfigOptions = new Dictionary<string, string>
		{
			["build_property.IsRoslynComponent"] = "true",
		}.ToImmutableDictionary(),
		AdditionalAssemblyTypes =
		[
			typeof(Purview.SourceGeneratorFramework.CodeWriter),
			typeof(Purview.SourceGeneratorFramework.GenerationSettings),
			typeof(Purview.SourceGeneratorFramework.TypeIdentity),
			typeof(Purview.SourceGeneratorFramework.TypeReference),
			typeof(Purview.SourceGeneratorFramework.XmlCommentWriter),
			typeof(Purview.SourceGeneratorFramework.Helpers.IncrementalPipeline),
			typeof(Purview.SourceGeneratorFramework.CodeWriterScopeValidationException),
		],
	};

	[Test]
	public async Task BareSgfCrefs_ReportDiagnosticsForMultipleTypes(CancellationToken cancellationToken)
	{
		const string source = """
			using Purview.SourceGeneratorFramework;
			using Purview.SourceGeneratorFramework.Helpers;

			/// <summary>
			/// Shared <see cref="TypeReference"/> building blocks.
			/// Writes with <see cref="CodeWriter"/>.
			/// Configures <see cref="GenerationSettings"/>.
			/// Tracks <see cref="TypeIdentity"/>.
			/// Emits docs with <see cref="XmlCommentWriter"/>.
			/// Composes providers with <see cref="IncrementalPipeline"/>.
			/// Throws <see cref="CodeWriterScopeValidationException"/> when scope validation fails.
			/// </summary>
			public static class TypeRefs
			{
			}
			""";

		var result = await AnalyzeAsync(source, RoslynComponentOptions, cancellationToken);

		await Assert.That(result).HasDiagnostics(7);
		await Assert.That(result).HasDiagnostic(AmbiguousFrameworkCrefAnalyzer.DiagnosticId);
	}

	[Test]
	public async Task QualifiedSgfCrefs_DoNotReportDiagnostic(CancellationToken cancellationToken)
	{
		const string source = """
			using Purview.SourceGeneratorFramework;

			/// <summary>
			/// Shared <see cref="global::Purview.SourceGeneratorFramework.TypeReference"/> building blocks.
			/// </summary>
			public static class TypeRefs
			{
			}
			""";

		var result = await AnalyzeAsync(source, RoslynComponentOptions, cancellationToken);

		await Assert.That(result).HasNoDiagnostics();
	}

	[Test]
	public async Task NonSgfType_DoesNotReportDiagnostic(CancellationToken cancellationToken)
	{
		const string source = """
			using System;

			/// <summary>
			/// Shared <see cref="String"/> building blocks.
			/// </summary>
			public static class TypeRefs
			{
			}
			""";

		var result = await AnalyzeAsync(source, RoslynComponentOptions, cancellationToken);

		await Assert.That(result).HasNoDiagnostics();
	}

	[Test]
	public async Task ShadowTypeWithSgfName_DoesNotReportDiagnostic(CancellationToken cancellationToken)
	{
		const string source = """
			namespace Example;

			public sealed class TypeReference
			{
			}

			/// <summary>
			/// Shared <see cref="TypeReference"/> building blocks.
			/// </summary>
			public static class TypeRefs
			{
			}
			""";

		var result = await AnalyzeAsync(source, RoslynComponentOptions, cancellationToken);

		await Assert.That(result).HasNoDiagnostics();
	}

	[Test]
	public async Task MissingRoslynComponentOptIn_DoesNotReportDiagnostic(CancellationToken cancellationToken)
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

		var result = await AnalyzeAsync(
			source,
			new AnalyzerTestOptions { AdditionalAssemblyTypes = RoslynComponentOptions.AdditionalAssemblyTypes },
			cancellationToken
		);

		await Assert.That(result).HasNoDiagnostics();
	}

	[Test]
	public async Task RoslynComponentOptOut_DoesNotReportDiagnostic(CancellationToken cancellationToken)
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

		var options = RoslynComponentOptions with
		{
			AnalyzerConfigOptions = new Dictionary<string, string>
			{
				["build_property.IsRoslynComponent"] = "false",
			}.ToImmutableDictionary(),
		};

		var result = await AnalyzeAsync(source, options, cancellationToken);

		await Assert.That(result).HasNoDiagnostics();
	}
}
