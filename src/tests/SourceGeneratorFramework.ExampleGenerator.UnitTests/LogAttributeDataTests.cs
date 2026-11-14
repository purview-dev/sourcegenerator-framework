using Purview.SourceGeneratorFramework.Examples;
using Purview.SourceGeneratorFramework.Generators;
using Purview.SourceGeneratorFramework.Testing;
using Purview.SourceGeneratorFramework.Testing.TUnit;

namespace Purview.SourceGeneratorFramework.ExampleGenerator;

public class LogAttributeDataTests
	: TUnitSourceGeneratorTestBase<AttributeDataModelGenerator, LogAttributeDataTestOptions>
{
	[Test]
	public async Task Generate_InheritedAttributeMapping_DebugDefaultsToDebugLevel(CancellationToken cancellationToken)
	{
		const string source = """
			using Purview.SourceGeneratorFramework.Examples;
			using Purview.SourceGeneratorFramework.Generators;

			namespace Sample;

			[Generate(typeof(LogAttribute), MatchByInheritance = true)]
			public readonly partial record struct LogAttributeData(
				[Property] string? Message,
				[Property] int EventId,
				[Property] string? CategoryName,
				[Property(DefaultValue = LogLevel.Information)] LogLevel Level
			);

			[Generate(typeof(DebugAttribute))]
			public readonly partial record struct DebugAttributeData(
				[NestedModel] LogAttributeData Log,
				[Property(DefaultValue = LogLevel.Debug)] LogLevel Level
			);
			""";

		var result = await GenerateAsync(source, cancellationToken: cancellationToken);

		result.AssertNoGenerationExceptions().AssertNoLogErrors();

		var debugGenerated = await GetGeneratedStringAsync(
			result,
			"DebugAttributeData.AttributeDataModel.g.cs",
			cancellationToken
		);

		await Assert.That(debugGenerated).IsNotNull();
		await Assert.That(debugGenerated).Contains("readonly partial record struct DebugAttributeData");
		await Assert.That(debugGenerated).Contains("global::Sample.LogAttributeData Log");
		await Assert
			.That(debugGenerated)
			.Contains("var log = global::Sample.LogAttributeData.FromAttributeDataUnchecked(attributeData);");
		await Assert
			.That(debugGenerated)
			.Contains(
				"attributeData.GetNamedArgument<global::Purview.SourceGeneratorFramework.Examples.LogLevel>(\"Level\", (global::Purview.SourceGeneratorFramework.Examples.LogLevel)1);"
			);

		var logGenerated = await GetGeneratedStringAsync(
			result,
			"LogAttributeData.AttributeDataModel.g.cs",
			cancellationToken
		);

		await Assert.That(logGenerated).IsNotNull();
		await Assert.That(logGenerated).Contains("InheritsFrom(attributeData.AttributeClass, TargetAttribute)");
		await Assert
			.That(logGenerated)
			.Contains(
				"attributeData.GetNamedArgument<global::Purview.SourceGeneratorFramework.Examples.LogLevel>(\"Level\", (global::Purview.SourceGeneratorFramework.Examples.LogLevel)2);"
			);
	}

	[Test]
	public async Task LogAttributeData_PositionalMessage_IsRead()
	{
		// LogAttribute.Message has a private setter, so the message only ever arrives through the constructor.
		var attribute = AttributeDataFactory.FromUsage("[Log(\"hello\", EventId = 3)]", ExamplesOptions);

		var model = LogAttributeData.FromAttributeData(attribute);

		await Assert.That(model.Message).IsEqualTo("hello");
		await Assert.That(model.EventId).IsEqualTo(3);
		await Assert.That(model.Level).IsEqualTo(LogLevel.Information);
	}

	[Test]
	public async Task DebugAttributeData_PositionalMessage_FlowsIntoNestedModel()
	{
		var attribute = AttributeDataFactory.FromUsage("[Debug(\"hi\")]", ExamplesOptions);

		var model = DebugAttributeData.FromAttributeData(attribute);

		await Assert.That(model.Log.Message).IsEqualTo("hi");
		await Assert.That(model.Level).IsEqualTo(LogLevel.Debug);
	}

	static LogAttributeDataTestOptions ExamplesOptions =>
		new() { AdditionalNamespaces = ["Purview.SourceGeneratorFramework.Examples"] };

	static async Task<string?> GetGeneratedStringAsync(
		DriverRunResult result,
		string fileName,
		CancellationToken cancellationToken
	)
	{
		var tree = result.GetGeneratedTree(fileName);
		return tree is null ? null : (await tree.GetTextAsync(cancellationToken)).ToString();
	}
}

public sealed record LogAttributeDataTestOptions : SourceGeneratorTestOptions
{
	public LogAttributeDataTestOptions()
	{
		AdditionalAssemblyTypes = AdditionalAssemblyTypes.AddRange(
			typeof(TypeIdentity),
			typeof(LogLevel),
			typeof(LogAttribute),
			typeof(DebugAttribute)
		);
	}
}
