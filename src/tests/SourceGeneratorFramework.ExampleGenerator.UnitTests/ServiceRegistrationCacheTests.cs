using Purview.SourceGeneratorFramework.Examples;
using Purview.SourceGeneratorFramework.Testing;
using Purview.SourceGeneratorFramework.Testing.TUnit;

namespace Purview.SourceGeneratorFramework.ExampleGenerator;

/// <summary>
/// Proves the <see cref="ServiceRegistrationGenerator"/> pipeline caches correctly stage-by-stage. Other
/// generator projects should mirror this pattern using <c>SourceGeneratorTestRunner.RunIncrementalAsync</c> or
/// <c>GenerateIncrementalAsync</c>.
/// </summary>
public class ServiceRegistrationCacheTests
	: TUnitSourceGeneratorTestBase<ServiceRegistrationGenerator, ServiceRegistrationTestOptions>
{
	const string Source = """
		namespace Test;

		[GenerateService]
		public class MyService { }
		""";

	[Test]
	public async Task FirstRun_AllStagesAreNew(CancellationToken cancellationToken)
	{
		var result = await GenerateIncrementalAsync(
			[new IncrementalRunInput([Source])],
			cancellationToken: cancellationToken
		);

		await Assert.That(result.Runs[0]).AllStepsNew();
	}

	[Test]
	public async Task IdenticalRerun_AllStagesCached(CancellationToken cancellationToken)
	{
		var result = await GenerateIncrementalAsync([Source], cancellationToken: cancellationToken);

		// The generator's own pipeline stages must all be cached or unchanged. (Roslyn's internal
		// ForAttributeWithMetadataName steps can report Modified on rerun because the post-initialization
		// attribute source is regenerated as a new tree.)
		await Assert.That(result.Runs[1]).StepIsCached("GetMSBuildPropertyValue_EmitServiceRegistrationInfo");
		await Assert.That(result.Runs[1]).StepIsCached("GetGenerationConfiguration");
		await Assert.That(result.Runs[1]).StepIsCached("GetGenerationContext_EmptyCapabilities");
		await Assert.That(result.Runs[1]).StepIsCached("ForAttribute_GenerateServiceAttribute");
	}

	[Test]
	public async Task PropertyChange_MarksPropertyStageModified_AttributeStageStaysCached(
		CancellationToken cancellationToken
	)
	{
		var result = await GenerateIncrementalAsync(
			[
				new IncrementalRunInput([Source]),
				new IncrementalRunInput([Source], [(PropertyLibrary.EmitServiceRegistrationInfo, "true")]),
			],
			cancellationToken: cancellationToken
		);

		await Assert.That(result.Runs[1]).StepIsModified("GetMSBuildPropertyValue_EmitServiceRegistrationInfo");
		await Assert.That(result.Runs[1]).StepIsCached("ForAttribute_GenerateServiceAttribute");
	}

	[Test]
	public async Task SourceChange_MarksAttributeStageModified_PropertyStageStaysCached(
		CancellationToken cancellationToken
	)
	{
		const string changedSource = """
			namespace Test;

			[GenerateService(ServiceLifetime.Transient, Name = "Other")]
			public class OtherService { }
			""";

		var result = await GenerateIncrementalAsync(
			[new IncrementalRunInput([Source]), new IncrementalRunInput([changedSource])],
			cancellationToken: cancellationToken
		);

		await Assert.That(result.Runs[1]).StepIsModified("ForAttribute_GenerateServiceAttribute");
		await Assert.That(result.Runs[1]).StepIsCached("GetMSBuildPropertyValue_EmitServiceRegistrationInfo");
	}

	[Test]
	public async Task GenerateIncrementalAsync_CapturesFrameworkLoggingPerRun(CancellationToken cancellationToken)
	{
		// Arrange / Act - two runs over a single shared driver.
		var result = await GenerateIncrementalAsync([Source], cancellationToken: cancellationToken);

		// Assert - the first run executes the pipeline and logs; the entries belong to that run alone.
		await Assert.That(result.Runs[0].LogEntries).IsNotEmpty();
		await Assert
			.That(
				result
					.Runs[0]
					.LogEntries.Any(entry => entry.Message.Contains("generation context", StringComparison.Ordinal))
			)
			.IsTrue();

		// Each run carries only its own entries. If they were accumulated across the sequence instead of
		// drained per run, the second run would also hold everything the first one logged.
		await Assert.That(result.Runs[1].LogEntries.Length).IsLessThanOrEqualTo(result.Runs[0].LogEntries.Length);
	}

	[Test]
	public async Task GenerateIncrementalAsync_GivenLoggingDisabled_CapturesNothing(CancellationToken cancellationToken)
	{
		// Arrange / Act
		var result = await GenerateIncrementalAsync(
			[Source],
			new ServiceRegistrationTestOptions { EnableLogging = false },
			cancellationToken
		);

		// Assert
		await Assert.That(result.Runs[0].LogEntries).IsEmpty();
		await Assert.That(result.Runs[1].LogEntries).IsEmpty();
	}
}
