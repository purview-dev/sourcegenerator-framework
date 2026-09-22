namespace Purview.SourceGeneratorFramework.Analyzers;

public sealed class SelfContainedGeneratorAnalyzerTests
{
	static readonly Dictionary<string, string> DefaultProperties = new()
	{
		["build_property.IsRoslynComponent"] = "true",
		["build_property.IsPackable"] = "false",
		["build_property.PurviewEmbedSourceGeneratorFramework"] = "true",
		["build_property.PurviewMergeSourceGeneratorFrameworkForAnalyzerFiles"] = "false",
		["build_property.PurviewSourceGeneratorFrameworkAnalyzerValidation"] = "true",
	};

	[Test]
	public async Task GivenNonSelfContainedRoslynComponent_ReportsError(CancellationToken cancellationToken)
	{
		// Arrange
		SelfContainedGeneratorAnalyzer analyzer = new();

		// Act
		var diagnostics = await analyzer.GetAnalyzerDiagnosticsAsync(
			"class C { }",
			DefaultProperties,
			referenceSourceGeneratorFramework: true,
			cancellationToken
		);

		// Assert
		await Assert
			.That(diagnostics.Select(static d => d.Id).ToArray())
			.Contains(SelfContainedGeneratorAnalyzer.DiagnosticId);
	}

	[Test]
	public async Task GivenNotRoslynComponent_DoesNotReport(CancellationToken cancellationToken)
	{
		// Arrange
		Dictionary<string, string> properties = new(DefaultProperties)
		{
			["build_property.IsRoslynComponent"] = "false",
		};

		// Act
		var diagnostics = await new SelfContainedGeneratorAnalyzer().GetAnalyzerDiagnosticsAsync(
			"class C { }",
			properties,
			referenceSourceGeneratorFramework: true,
			cancellationToken
		);

		// Assert
		await Assert.That(diagnostics.Any(static d => d.Id == SelfContainedGeneratorAnalyzer.DiagnosticId)).IsFalse();
	}

	[Test]
	public async Task GivenNoFrameworkReference_DoesNotReport(CancellationToken cancellationToken)
	{
		// Act
		var diagnostics = await new SelfContainedGeneratorAnalyzer().GetAnalyzerDiagnosticsAsync(
			"class C { }",
			DefaultProperties,
			referenceSourceGeneratorFramework: false,
			cancellationToken
		);

		// Assert
		await Assert.That(diagnostics.Any(static d => d.Id == SelfContainedGeneratorAnalyzer.DiagnosticId)).IsFalse();
	}

	[Test]
	public async Task GivenPackableGenerator_DoesNotReport(CancellationToken cancellationToken)
	{
		// Arrange
		Dictionary<string, string> properties = new(DefaultProperties) { ["build_property.IsPackable"] = "true" };

		// Act
		var diagnostics = await new SelfContainedGeneratorAnalyzer().GetAnalyzerDiagnosticsAsync(
			"class C { }",
			properties,
			referenceSourceGeneratorFramework: true,
			cancellationToken
		);

		// Assert
		await Assert.That(diagnostics.Any(static d => d.Id == SelfContainedGeneratorAnalyzer.DiagnosticId)).IsFalse();
	}

	[Test]
	public async Task GivenMergedAnalyzerFiles_DoesNotReport(CancellationToken cancellationToken)
	{
		// Arrange
		Dictionary<string, string> properties = new(DefaultProperties)
		{
			["build_property.PurviewMergeSourceGeneratorFrameworkForAnalyzerFiles"] = "true",
		};

		// Act
		var diagnostics = await new SelfContainedGeneratorAnalyzer().GetAnalyzerDiagnosticsAsync(
			"class C { }",
			properties,
			referenceSourceGeneratorFramework: true,
			cancellationToken
		);

		// Assert
		await Assert.That(diagnostics.Any(static d => d.Id == SelfContainedGeneratorAnalyzer.DiagnosticId)).IsFalse();
	}

	[Test]
	public async Task GivenValidationOptOut_DoesNotReport(CancellationToken cancellationToken)
	{
		// Arrange
		Dictionary<string, string> properties = new(DefaultProperties)
		{
			["build_property.PurviewSourceGeneratorFrameworkAnalyzerValidation"] = "false",
		};

		// Act
		var diagnostics = await new SelfContainedGeneratorAnalyzer().GetAnalyzerDiagnosticsAsync(
			"class C { }",
			properties,
			referenceSourceGeneratorFramework: true,
			cancellationToken
		);

		// Assert
		await Assert.That(diagnostics.Any(static d => d.Id == SelfContainedGeneratorAnalyzer.DiagnosticId)).IsFalse();
	}

	[Test]
	public async Task GivenEmbeddingDisabled_DoesNotReport(CancellationToken cancellationToken)
	{
		// Arrange
		Dictionary<string, string> properties = new(DefaultProperties)
		{
			["build_property.PurviewEmbedSourceGeneratorFramework"] = "false",
		};

		// Act
		var diagnostics = await new SelfContainedGeneratorAnalyzer().GetAnalyzerDiagnosticsAsync(
			"class C { }",
			properties,
			referenceSourceGeneratorFramework: true,
			cancellationToken
		);

		// Assert
		await Assert.That(diagnostics.Any(static d => d.Id == SelfContainedGeneratorAnalyzer.DiagnosticId)).IsFalse();
	}
}
