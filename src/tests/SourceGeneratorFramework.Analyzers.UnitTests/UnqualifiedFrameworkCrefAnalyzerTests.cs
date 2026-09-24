using System.Collections.Immutable;
using System.Globalization;
using Purview.SourceGeneratorFramework.Testing;
using Purview.SourceGeneratorFramework.Testing.TUnit;

namespace Purview.SourceGeneratorFramework.Analyzers;

public sealed class UnqualifiedFrameworkCrefAnalyzerTests
	: TUnitDiagnosticAnalyzerTestBase<UnqualifiedFrameworkCrefAnalyzer>
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
	public async Task BareSgfCrefs_ReportInlineCodeGuidanceForMultipleTypes(CancellationToken cancellationToken)
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
		await Assert.That(result).HasDiagnostic(UnqualifiedFrameworkCrefAnalyzer.DiagnosticId);
	}

	[Test]
	public async Task BareSgfCref_MessageRecommendsInlineCode(CancellationToken cancellationToken)
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

		var result = await AnalyzeAsync(source, RoslynComponentOptions, cancellationToken);

		var diagnostic = result.Diagnostics.Single();
		await Assert.That(diagnostic.GetMessage(CultureInfo.InvariantCulture)).Contains("<c>TypeReference</c>");
	}

	[Test]
	public async Task FrameworkQualifiedSgfCref_DoesNotReportDiagnostic(CancellationToken cancellationToken)
	{
		const string source = """
			/// <summary>
			/// Shared <see cref="Purview.SourceGeneratorFramework.TypeReference"/> building blocks.
			/// </summary>
			public static class TypeRefs
			{
			}
			""";

		var result = await AnalyzeAsync(source, RoslynComponentOptions, cancellationToken);

		await Assert.That(result).HasNoDiagnostics();
	}

	[Test]
	public async Task MemberCref_DoesNotReportDiagnostic(CancellationToken cancellationToken)
	{
		const string source = """
			using Purview.SourceGeneratorFramework;

			/// <summary>
			/// Writes with <see cref="CodeWriter.Write"/>.
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
