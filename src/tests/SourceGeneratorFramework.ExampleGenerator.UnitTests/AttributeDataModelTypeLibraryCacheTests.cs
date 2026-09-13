using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Purview.SourceGeneratorFramework.Generators;
using Purview.SourceGeneratorFramework.Testing;
using Purview.SourceGeneratorFramework.Testing.TUnit;
using StepReason = Microsoft.CodeAnalysis.IncrementalStepRunReason;

namespace Purview.SourceGeneratorFramework.ExampleGenerator;

/// <summary>
/// Proves the multi-generator TypeLibrary + attribute-data-model pipeline caches correctly stage-by-stage:
/// an identical rerun keeps every framework stage <c>Cached</c>/<c>Unchanged</c>, a model edit marks only
/// <c>GetAttributeDataTargets</c> <c>Modified</c>, and a spec edit marks only
/// <c>GetTypeLibrarySpecClassNames</c> <c>Modified</c>.
/// </summary>
public class AttributeDataModelTypeLibraryCacheTests
	: TUnitSourceGeneratorTestBase<AttributeDataModelGenerator, AttributeDataModelTypeLibraryTestOptions>
{
	const string Source = """
		using Purview.SourceGeneratorFramework;
		using Purview.SourceGeneratorFramework.Generators;

		namespace Test
		{
			[GenerateTypeLibrary]
			static partial class TypeLibrarySpec
			{
				[TypeRef("Aspire.Hosting.AspireC4", GenerateFullNameConst = true)]
				static readonly TypeIdentity SeverityAttribute = default;
			}

			[Generate(TypeLibrary.Aspire.Hosting.AspireC4.SeverityAttributeFullName)]
			public readonly partial record struct SeverityAttributeData(
				[Argument(IsEnum = true, Name = "severity", DefaultValue = "Inherit")] string Severity
			);
		}
		""";

	const string ChangedModelSource = """
		using Purview.SourceGeneratorFramework;
		using Purview.SourceGeneratorFramework.Generators;

		namespace Test
		{
			[GenerateTypeLibrary]
			static partial class TypeLibrarySpec
			{
				[TypeRef("Aspire.Hosting.AspireC4", GenerateFullNameConst = true)]
				static readonly TypeIdentity SeverityAttribute = default;
			}

			[Generate(TypeLibrary.Aspire.Hosting.AspireC4.SeverityAttributeFullName)]
			public readonly partial record struct SeverityAttributeData(
				[Argument(IsEnum = true, Name = "severity", DefaultValue = "Inherit")] string Severity,
				[Property(IsEnum = true, DefaultValue = "Inherit")] string Level
			);
		}
		""";

	const string ChangedSpecSource = """
		using Purview.SourceGeneratorFramework;
		using Purview.SourceGeneratorFramework.Generators;

		namespace Test
		{
			[GenerateTypeLibrary]
			static partial class TypeLibrarySpec
			{
				[TypeRef("Aspire.Hosting.AspireC4", GenerateFullNameConst = true)]
				static readonly TypeIdentity SeverityAttribute = default;
			}

			[GenerateTypeLibrary(ClassName = "MyLibrary")]
			static partial class MyLibrarySpec
			{
				[TypeRef("Test")]
				static readonly TypeIdentity Something = default;
			}

			[Generate(TypeLibrary.Aspire.Hosting.AspireC4.SeverityAttributeFullName)]
			public readonly partial record struct SeverityAttributeData(
				[Argument(IsEnum = true, Name = "severity", DefaultValue = "Inherit")] string Severity
			);
		}
		""";

	static ImmutableDictionary<string, ImmutableArray<StepReason>> StepReasons(IncrementalCacheRun run)
	{
		var builder = ImmutableDictionary.CreateBuilder<string, ImmutableArray<StepReason>>();
		foreach (var pair in run.Steps)
			builder[pair.Key] = [.. pair.Value.SelectMany(step => step.Outputs.Select(static output => output.Reason))];
		return builder.ToImmutable();
	}

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
	public async Task IdenticalRerun_AllFrameworkStagesCached(CancellationToken cancellationToken)
	{
		var result = await GenerateIncrementalAsync([Source], cancellationToken: cancellationToken);

		// The value-equatable framework stages of both generators must all be cached or unchanged. (Roslyn's
		// internal ForAttributeWithMetadataName steps can report Modified on rerun because the post-initialization
		// attribute source is regenerated as a new tree.)
		string[] frameworkStages =
		[
			"GetAttributeDataTargets",
			"GetTypeLibrarySpecClassNames",
			"GetTypeLibraryTargets",
			"GetFrameworkTypeLibraryTree",
			"GetGenerationConfiguration",
			"GetGenerationContext_EmptyCapabilities",
		];
		var reasons = StepReasons(result.Runs[1]);
		await Assert
			.That(
				frameworkStages.All(stage =>
					reasons.TryGetValue(stage, out var stageReasons)
					&& stageReasons.All(reason => reason is StepReason.Cached or StepReason.Unchanged)
				)
			)
			.IsTrue();
	}

	[Test]
	public async Task ModelChange_MarksAttributeStageModified_SpecClassNamesStageStaysCached(
		CancellationToken cancellationToken
	)
	{
		var result = await GenerateIncrementalAsync(
			[new IncrementalRunInput([Source]), new IncrementalRunInput([ChangedModelSource])],
			cancellationToken: cancellationToken
		);

		await Assert.That(result.Runs[1]).StepIsModified("GetAttributeDataTargets");
		await Assert.That(result.Runs[1]).StepIsCached("GetTypeLibrarySpecClassNames");
		await Assert.That(result.Runs[1]).StepIsCached("GetTypeLibraryTargets");
	}

	[Test]
	public async Task SpecChange_MarksSpecClassNamesStageModified_AttributeStageStaysCached(
		CancellationToken cancellationToken
	)
	{
		var result = await GenerateIncrementalAsync(
			[new IncrementalRunInput([Source]), new IncrementalRunInput([ChangedSpecSource])],
			cancellationToken: cancellationToken
		);

		await Assert.That(result.Runs[1]).StepIsModified("GetTypeLibrarySpecClassNames");
		await Assert.That(result.Runs[1]).StepIsCached("GetAttributeDataTargets");
	}
}
