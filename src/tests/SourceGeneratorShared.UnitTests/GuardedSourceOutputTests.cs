using System.Globalization;
using Purview.SourceGeneratorFramework.TestGenerators;

namespace Purview.SourceGeneratorFramework;

/// <summary>
/// Proves the framework's <c>RegisterSourceOutput</c> turns an exception from the emit callback into a
/// diagnostic, rather than letting it escape as <c>CS8785</c>.
/// </summary>
/// <remarks>
/// The distinction matters to a consumer: <c>CS8785</c> means the generator emitted nothing at all for that
/// compilation, so every type it was supposed to produce is also reported missing. A diagnostic leaves the
/// rest of the compilation intact and names the generator that is actually at fault.
/// </remarks>
public class GuardedSourceOutputTests
{
	const string Source = """
		[TestAttribute]
		public partial class MyClass { }
		""";

	const string TestAttributeSource = """
		[System.AttributeUsage(System.AttributeTargets.Class)]
		public sealed class TestAttribute : System.Attribute { }
		""";

	static SourceGeneratorTestOptions CreateOptions() =>
		new SourceGeneratorTestOptions { ThrowOnGenerationException = false }
			.WithAdditionalSources(TestAttributeSource)
			.WithExcludeGeneratedSourceHintNames("TestAttribute");

	[Test]
	public async Task RegisterSourceOutput_GivenAThrowingCallback_ReportsADiagnosticInsteadOfThrowing(
		CancellationToken cancellationToken
	)
	{
		SourceGeneratorTestRunner<ThrowingTestGenerator> runner = new();

		using var result = await runner.RunAsync(Source, CreateOptions(), cancellationToken);

		var failure = result.DriverResult.Diagnostics.SingleOrDefault(diagnostic =>
			diagnostic.Id == ComponentFailure.For<ThrowingTestGenerator>().Id
		);

		await Assert.That(failure).IsNotNull();

		var message = failure!.GetMessage(CultureInfo.InvariantCulture);
		await Assert.That(message).Contains(nameof(ThrowingTestGenerator));
		await Assert.That(message).Contains(nameof(InvalidOperationException));
		await Assert.That(message).Contains(ThrowingTestGenerator.FailureMessage);
	}

	/// <summary>
	/// The exception must not reach the driver, which is what would otherwise become <c>CS8785</c>.
	/// </summary>
	[Test]
	public async Task RegisterSourceOutput_GivenAThrowingCallback_ProducesNoGeneratorException(
		CancellationToken cancellationToken
	)
	{
		SourceGeneratorTestRunner<ThrowingTestGenerator> runner = new();

		using var result = await runner.RunAsync(Source, CreateOptions(), cancellationToken);

		await Assert.That(result.DriverResult.Results.Any(run => run.Exception is not null)).IsFalse();
		await Assert.That(result.DriverResult.Diagnostics.Any(diagnostic => diagnostic.Id == "CS8785")).IsFalse();
	}

	/// <summary>
	/// The id is derived from the generator's own namespace, so a consumer suppressing it is suppressing
	/// something that reads as theirs rather than as the framework's.
	/// </summary>
	[Test]
	public async Task FailureDiagnostic_UsesAnIdDerivedFromTheGeneratorsNamespace()
	{
		var descriptor = ComponentFailure.For<ThrowingTestGenerator>();

		await Assert
			.That(descriptor.Id)
			.IsEqualTo(ComponentFailure.DeriveIdPrefix(typeof(ThrowingTestGenerator)) + "0001");
		await Assert.That(descriptor.Title.ToString(CultureInfo.InvariantCulture)).IsEqualTo("Source generator failed");
	}
}
