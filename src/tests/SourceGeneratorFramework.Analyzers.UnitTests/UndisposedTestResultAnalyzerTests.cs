using Microsoft.CodeAnalysis;
using Purview.SourceGeneratorFramework.Testing;
using Purview.SourceGeneratorFramework.Testing.TUnit;

namespace Purview.SourceGeneratorFramework.Analyzers;

public sealed class UndisposedTestResultAnalyzerTests : TUnitDiagnosticAnalyzerTestBase<UndisposedTestResultAnalyzer>
{
	// A stand-in for the runner surface: the analyzer keys off the produced type, not the method name, so a
	// consumer's own wrapper is covered in exactly the same way the shipped runners are.
	const string Harness = """
		using Purview.SourceGeneratorFramework;
		using Purview.SourceGeneratorFramework.Testing;

		static class Runner
		{
			public static DriverRunResult Run(SourceGeneratorTestOptions options) => null!;
		}
		""";

	static AnalyzerTestOptions Options =>
		new()
		{
			AdditionalAssemblyTypes = [typeof(DriverRunResult), typeof(SourceGeneratorTestOptions)],
			IncludeDefaultNamespaces = false,
		};

	[Test]
	public async Task UsingDeclaration_DoesNotReportDiagnostic(CancellationToken cancellationToken)
	{
		// Arrange
		const string source =
			Harness
			+ """

				class Tests
				{
					public void Run()
					{
						using var result = Runner.Run(new SourceGeneratorTestOptions().Compile());
						System.Console.WriteLine(result.GetSource());
					}
				}
				""";

		// Act
		var result = await AnalyzeAsync(source, Options, cancellationToken);

		// Assert
		await Assert.That(result).HasNoDiagnostics();
	}

	[Test]
	public async Task PlainLocal_ReportsDiagnostic(CancellationToken cancellationToken)
	{
		// Arrange
		const string source =
			Harness
			+ """

				class Tests
				{
					public void Run()
					{
						var result = Runner.Run(new SourceGeneratorTestOptions().Compile());
						System.Console.WriteLine(result.GetSource());
					}
				}
				""";

		// Act
		var result = await AnalyzeAsync(source, Options, cancellationToken);

		// Assert
		await Assert.That(result).HasDiagnostics(1);
		await Assert.That(result).HasDiagnostic(UndisposedTestResultAnalyzer.DiagnosticId);
	}

	[Test]
	public async Task ReturnedLocal_DoesNotReportDiagnostic(CancellationToken cancellationToken)
	{
		// Arrange - the caller becomes the owner, so the helper must not dispose it.
		const string source =
			Harness
			+ """

				class Tests
				{
					public DriverRunResult Build()
					{
						var result = Runner.Run(new SourceGeneratorTestOptions().Compile());
						return result;
					}
				}
				""";

		// Act
		var result = await AnalyzeAsync(source, Options, cancellationToken);

		// Assert
		await Assert.That(result).HasNoDiagnostics();
	}

	[Test]
	public async Task HelperReturningAProjection_ReportsDiagnostic(CancellationToken cancellationToken)
	{
		// Arrange - the result itself never leaves the helper, so the helper owns it.
		const string source =
			Harness
			+ """

				class Tests
				{
					public string BuildDefinition()
					{
						var result = Runner.Run(new SourceGeneratorTestOptions().Compile());
						return result.GetSource();
					}
				}
				""";

		// Act
		var result = await AnalyzeAsync(source, Options, cancellationToken);

		// Assert
		await Assert.That(result).HasDiagnostics(1);
		await Assert.That(result).HasDiagnostic(UndisposedTestResultAnalyzer.DiagnosticId);
	}

	[Test]
	public async Task ExplicitDispose_DoesNotReportDiagnostic(CancellationToken cancellationToken)
	{
		// Arrange
		const string source =
			Harness
			+ """

				class Tests
				{
					public void Run()
					{
						var result = Runner.Run(new SourceGeneratorTestOptions().Compile());
						System.Console.WriteLine(result.GetSource());
						result.Dispose();
					}
				}
				""";

		// Act
		var result = await AnalyzeAsync(source, Options, cancellationToken);

		// Assert
		await Assert.That(result).HasNoDiagnostics();
	}

	[Test]
	public async Task DiscardedResult_ReportsDiagnostic(CancellationToken cancellationToken)
	{
		// Arrange
		const string source =
			Harness
			+ """

				class Tests
				{
					public void Run()
					{
						_ = Runner.Run(new SourceGeneratorTestOptions().Compile());
					}
				}
				""";

		// Act
		var result = await AnalyzeAsync(source, Options, cancellationToken);

		// Assert
		await Assert.That(result).HasDiagnostics(1);
		await Assert.That(result).HasDiagnostic(UndisposedTestResultAnalyzer.DiagnosticId);
	}

	[Test]
	public async Task CompileToAssemblyRun_ReportsAsWarning(CancellationToken cancellationToken)
	{
		// Arrange - an emitted assembly is the case that actually costs memory.
		const string source =
			Harness
			+ """

				class Tests
				{
					public void Run()
					{
						var result = Runner.Run(new SourceGeneratorTestOptions { CompileToAssembly = true });
						System.Console.WriteLine(result.GetSource());
					}
				}
				""";

		// Act
		var result = await AnalyzeAsync(source, Options, cancellationToken);

		// Assert
		await Assert.That(result.Diagnostics.Single().Severity).IsEqualTo(DiagnosticSeverity.Warning);
	}

	[Test]
	public async Task RunWithoutCompile_ReportsAsInformation(CancellationToken cancellationToken)
	{
		// Arrange - nothing was emitted, so the leak is cheap and the rule stays informational.
		const string source =
			Harness
			+ """

				class Tests
				{
					public void Run()
					{
						var result = Runner.Run(new SourceGeneratorTestOptions());
						System.Console.WriteLine(result.GetSource());
					}
				}
				""";

		// Act
		var result = await AnalyzeAsync(source, Options, cancellationToken);

		// Assert
		await Assert.That(result.Diagnostics.Single().Severity).IsEqualTo(DiagnosticSeverity.Info);
	}

	[Test]
	public async Task NonHarnessDisposable_DoesNotReportDiagnostic(CancellationToken cancellationToken)
	{
		// Arrange - the rule is scoped to the Testing namespace, not to disposables in general.
		const string source = """
			class Thing : System.IDisposable
			{
				public void Dispose() { }

				public static Thing Make() => new Thing();
			}

			class Tests
			{
				public void Run()
				{
					var thing = Thing.Make();
					System.Console.WriteLine(thing.ToString());
				}
			}
			""";

		// Act
		var result = await AnalyzeAsync(source, Options, cancellationToken);

		// Assert
		await Assert.That(result).HasNoDiagnostics();
	}
}
