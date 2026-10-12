using Purview.SourceGeneratorFramework.Analyzers;
using Purview.SourceGeneratorFramework.Testing;
using Purview.SourceGeneratorFramework.Testing.TUnit;

namespace Purview.SourceGeneratorFramework.CodeFixers;

public sealed class DisposeTestResultCodeFixProviderTests
	: TUnitCodeFixTestBase<UndisposedTestResultAnalyzer, DisposeTestResultCodeFixProvider>
{
	const string Harness = """
		using Purview.SourceGeneratorFramework;
		using Purview.SourceGeneratorFramework.Testing;

		static class Runner
		{
			public static DriverRunResult Run(SourceGeneratorTestOptions options) => null!;
		}
		""";

	static CodeFixTestOptions Options =>
		new()
		{
			EquivalenceKey = DisposeTestResultCodeFixProvider.EquivalenceKey,
			AdditionalAssemblyTypes = [typeof(DriverRunResult), typeof(SourceGeneratorTestOptions)],
			IncludeDefaultNamespaces = false,
		};

	[Test]
	public async Task UndisposedResult_AddsUsingDeclaration(CancellationToken cancellationToken)
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
		var result = await ApplyCodeFixAsync(source, Options, cancellationToken);

		// Assert
		await Assert.That(result).HasDiagnostic(UndisposedTestResultAnalyzer.DiagnosticId);
		await Assert
			.That(result.FixedSource)
			.Contains("using var result = Runner.Run(new SourceGeneratorTestOptions().Compile());");
	}

	[Test]
	public async Task UndisposedResultInHelper_AddsUsingDeclaration(CancellationToken cancellationToken)
	{
		// Arrange - the helper projects a value out of the result and leaks the result itself.
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
		var result = await ApplyCodeFixAsync(source, Options, cancellationToken);

		// Assert
		await Assert.That(result.FixedSource).Contains("using var result = Runner.Run(");
	}
}
