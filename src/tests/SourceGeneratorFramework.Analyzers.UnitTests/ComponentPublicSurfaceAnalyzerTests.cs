namespace Purview.SourceGeneratorFramework.Analyzers;

public sealed class ComponentPublicSurfaceAnalyzerTests
{
	/// <summary>
	/// The merge runs for a Roslyn component that embeds the framework and is either packable or
	/// returns a merged analyzer artifact.
	/// </summary>
	static readonly Dictionary<string, string> MergedComponentProperties = new()
	{
		["build_property.IsRoslynComponent"] = "true",
		["build_property.PurviewEmbedSourceGeneratorFramework"] = "true",
		["build_property.IsPackable"] = "true",
	};

	const string ExposingSurfaceSource = """
		namespace Fixture.Component
		{
			public sealed class Consumer
			{
				public Purview.SourceGeneratorFramework.TypeIdentity Identity => default;
			}
		}
		""";

	[Test]
	public async Task GivenPublicMemberExposingFrameworkType_ReportsDiagnostic(CancellationToken cancellationToken)
	{
		// Arrange
		// Act
		var diagnostics = await new ComponentPublicSurfaceAnalyzer().GetAnalyzerDiagnosticsAsync(
			ExposingSurfaceSource,
			MergedComponentProperties,
			referenceSourceGeneratorFramework: true,
			cancellationToken
		);

		// Assert
		await Assert
			.That(diagnostics.Select(static diagnostic => diagnostic.Id).ToArray())
			.Contains(ComponentPublicSurfaceAnalyzer.DiagnosticId);
	}

	[Test]
	public async Task GivenPublicGenericConstraintExposingFrameworkType_ReportsDiagnostic(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source = """
			namespace Fixture.Component
			{
				public sealed class Constrained<T>
					where T : Purview.SourceGeneratorFramework.IGenerationCapabilities
				{
				}
			}
			""";

		// Act
		var diagnostics = await new ComponentPublicSurfaceAnalyzer().GetAnalyzerDiagnosticsAsync(
			source,
			MergedComponentProperties,
			referenceSourceGeneratorFramework: true,
			cancellationToken
		);

		// Assert
		await Assert
			.That(diagnostics.Select(static diagnostic => diagnostic.Id).ToArray())
			.Contains(ComponentPublicSurfaceAnalyzer.DiagnosticId);
	}

	[Test]
	public async Task GivenFrameworkTypeUsedOnlyInsideMethodBody_DoesNotReportDiagnostic(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source = """
			namespace Fixture.Component
			{
				public sealed class Consumer
				{
					public string Describe()
					{
						var identity = new Purview.SourceGeneratorFramework.TypeIdentity("Name", "Namespace");
						return identity.Name;
					}
				}
			}
			""";

		// Act
		var diagnostics = await new ComponentPublicSurfaceAnalyzer().GetAnalyzerDiagnosticsAsync(
			source,
			MergedComponentProperties,
			referenceSourceGeneratorFramework: true,
			cancellationToken
		);

		// Assert
		await Assert
			.That(diagnostics.Any(static diagnostic => diagnostic.Id == ComponentPublicSurfaceAnalyzer.DiagnosticId))
			.IsFalse();
	}

	[Test]
	public async Task GivenPublicNestedTypeInsideInternalType_DoesNotReportDiagnostic(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source = """
			namespace Fixture.Component
			{
				internal static class InternalContainer
				{
					public sealed class Nested
					{
						public Purview.SourceGeneratorFramework.TypeIdentity Identity => default;
					}
				}
			}
			""";

		// Act
		var diagnostics = await new ComponentPublicSurfaceAnalyzer().GetAnalyzerDiagnosticsAsync(
			source,
			MergedComponentProperties,
			referenceSourceGeneratorFramework: true,
			cancellationToken
		);

		// Assert
		await Assert
			.That(diagnostics.Any(static diagnostic => diagnostic.Id == ComponentPublicSurfaceAnalyzer.DiagnosticId))
			.IsFalse();
	}

	[Test]
	public async Task GivenGeneratedTypeExposingFrameworkType_DoesNotReportDiagnostic(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		// The merge internalizes generator-emitted type libraries and attribute sets itself, so the
		// author has nothing to fix.
		const string source = """
			using System.Runtime.CompilerServices;

			namespace Fixture.Component
			{
				[CompilerGenerated]
				public static class GeneratedTypeLibrary
				{
					public static Purview.SourceGeneratorFramework.TypeIdentity Identity => default;
				}
			}
			""";

		// Act
		var diagnostics = await new ComponentPublicSurfaceAnalyzer().GetAnalyzerDiagnosticsAsync(
			source,
			MergedComponentProperties,
			referenceSourceGeneratorFramework: true,
			cancellationToken
		);

		// Assert
		await Assert
			.That(diagnostics.Any(static diagnostic => diagnostic.Id == ComponentPublicSurfaceAnalyzer.DiagnosticId))
			.IsFalse();
	}

	[Test]
	public async Task GivenEmbeddingDisabled_DoesNotReportDiagnostic(CancellationToken cancellationToken)
	{
		// Arrange
		Dictionary<string, string> properties = new(MergedComponentProperties)
		{
			["build_property.PurviewEmbedSourceGeneratorFramework"] = "false",
		};

		// Act
		var diagnostics = await new ComponentPublicSurfaceAnalyzer().GetAnalyzerDiagnosticsAsync(
			ExposingSurfaceSource,
			properties,
			referenceSourceGeneratorFramework: true,
			cancellationToken
		);

		// Assert
		await Assert
			.That(diagnostics.Any(static diagnostic => diagnostic.Id == ComponentPublicSurfaceAnalyzer.DiagnosticId))
			.IsFalse();
	}

	[Test]
	public async Task GivenNotARoslynComponent_DoesNotReportDiagnostic(CancellationToken cancellationToken)
	{
		// Arrange
		Dictionary<string, string> properties = new(MergedComponentProperties)
		{
			["build_property.IsRoslynComponent"] = "false",
		};

		// Act
		var diagnostics = await new ComponentPublicSurfaceAnalyzer().GetAnalyzerDiagnosticsAsync(
			ExposingSurfaceSource,
			properties,
			referenceSourceGeneratorFramework: true,
			cancellationToken
		);

		// Assert
		await Assert
			.That(diagnostics.Any(static diagnostic => diagnostic.Id == ComponentPublicSurfaceAnalyzer.DiagnosticId))
			.IsFalse();
	}

	[Test]
	public async Task GivenNoFrameworkReference_DoesNotReportDiagnostic(CancellationToken cancellationToken)
	{
		// Act
		var diagnostics = await new ComponentPublicSurfaceAnalyzer().GetAnalyzerDiagnosticsAsync(
			"public sealed class Consumer { }",
			MergedComponentProperties,
			referenceSourceGeneratorFramework: false,
			cancellationToken
		);

		// Assert
		await Assert
			.That(diagnostics.Any(static diagnostic => diagnostic.Id == ComponentPublicSurfaceAnalyzer.DiagnosticId))
			.IsFalse();
	}
}
