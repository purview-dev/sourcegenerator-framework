using Microsoft.CodeAnalysis;
using Purview.SourceGeneratorFramework.Examples;
using Purview.SourceGeneratorFramework.Generators;
using Purview.SourceGeneratorFramework.Testing;
using Purview.SourceGeneratorFramework.Testing.TUnit;
using Purview.SourceGeneratorFramework.Testing.TUnit.Assertions;

namespace Purview.SourceGeneratorFramework.ExampleGenerator;

/// <summary>
/// Runs the generated <see cref="InfoEventAttributeData"/> and <see cref="LogEventAttributeData"/> models against
/// real <see cref="AttributeData"/>. <see cref="InfoEventAttribute"/> does not derive from
/// <see cref="LogEventAttribute"/>, so the <c>[NestedModel]</c> must be read from the outer attribute without the
/// nested model's own target-attribute check.
/// </summary>
public class LogEventAttributeDataTests
	: TUnitSourceGeneratorTestBase<AttributeDataModelGenerator, LogEventAttributeDataTestOptions>
{
	const string Source = """
		using Purview.SourceGeneratorFramework.Examples;

		namespace Sample;

		public interface IEvents
		{
			[InfoEvent("custom {Value}", 42)]
			void Positional(int value);

			[InfoEvent(MessageTemplate = "named {Value}", EventId = 7, Name = "NamedEvent")]
			void Named(int value);

			[InfoEvent("mixed {Value}", Name = "MixedEvent")]
			void Mixed(int value);

			[InfoEvent]
			void Omitted(int value);

			[LogEvent(LogLevel.Warning, "direct {Value}")]
			void Direct(int value);
		}
		""";

	// AttributeDataFactory compiles the source and reads the application, without running a generator.
	static AttributeData AttributeOf(string methodName) =>
		AttributeDataFactory
			.FromSource(Source, "Sample.IEvents", methodName, new LogEventAttributeDataTestOptions())
			.Single();

	[Test]
	public async Task Applications_SupplyArgumentsInTheExpectedForms()
	{
		// Pins the attribute side of the test, so a model failure below is the model's fault.
		await Assert.That(AttributeOf("Positional")).HasConstructorArgument("messageTemplate", "custom {Value}");
		await Assert
			.That(AttributeOf("Positional"))
			.HasConstructorArgument("eventId", 42, AttributeArgumentForm.Positional);
		await Assert.That(AttributeOf("Named")).HasNamedArgument("MessageTemplate", "named {Value}");
		await Assert.That(AttributeOf("Mixed")).DoesNotHaveNamedArgument("EventId");
		await Assert.That(AttributeOf("Direct")).HasConstructorArgument("eventId", -1, AttributeArgumentForm.Defaulted);
	}

	[Test]
	public async Task NestedModel_PositionalArguments_FlowIntoNestedModel()
	{
		var model = InfoEventAttributeData.FromAttributeData(AttributeOf("Positional"));

		await Assert.That(model.Exists).IsTrue();
		await Assert.That(model.Log.Exists).IsTrue();
		await Assert.That(model.Log.MessageTemplate).IsEqualTo("custom {Value}");
		await Assert.That(model.Log.EventId).IsEqualTo(42);
		await Assert.That(model.Log.Name).IsNull();
	}

	[Test]
	public async Task NestedModel_NamedArguments_FlowIntoNestedModel()
	{
		var model = InfoEventAttributeData.FromAttributeData(AttributeOf("Named"));

		await Assert.That(model.Log.MessageTemplate).IsEqualTo("named {Value}");
		await Assert.That(model.Log.EventId).IsEqualTo(7);
		await Assert.That(model.Log.Name).IsEqualTo("NamedEvent");
	}

	[Test]
	public async Task NestedModel_MixedArguments_FlowIntoNestedModel()
	{
		var found = InfoEventAttributeData.TryFromAttributeData(AttributeOf("Mixed"), out var model);

		await Assert.That(found).IsTrue();
		await Assert.That(model.Log.MessageTemplate).IsEqualTo("mixed {Value}");
		await Assert.That(model.Log.Name).IsEqualTo("MixedEvent");
		await Assert.That(model.Log.EventId).IsEqualTo(-1);
	}

	[Test]
	public async Task NestedModel_OmittedArguments_TakeNestedModelDefaults()
	{
		var model = InfoEventAttributeData.FromAttributeData(AttributeOf("Omitted"));

		await Assert.That(model.Log.Exists).IsTrue();
		await Assert.That(model.Log.MessageTemplate).IsNull();
		await Assert.That(model.Log.EventId).IsEqualTo(-1);
		await Assert.That(model.Log.Name).IsNull();
	}

	[Test]
	public async Task NestedModel_ArgumentNotDeclaredOnOuterAttribute_TakesNestedModelDefault()
	{
		// InfoEventAttribute has no level constructor parameter or Level property.
		var model = InfoEventAttributeData.FromAttributeData(AttributeOf("Positional"));

		await Assert.That(model.Log.Level).IsEqualTo(LogLevel.Information);
	}

	[Test]
	public async Task NestedModelTarget_FromAttributeData_StillRejectsOtherAttributeTypes()
	{
		var attribute = AttributeOf("Positional");

		await Assert.That(LogEventAttributeData.FromAttributeData(attribute)).IsEqualTo(LogEventAttributeData.Empty);
		await Assert.That(LogEventAttributeData.TryFromAttributeData(attribute, out _)).IsFalse();
		await Assert
			.That(InfoEventAttributeData.FromAttributeData(AttributeOf("Direct")))
			.IsEqualTo(InfoEventAttributeData.Empty);
	}

	[Test]
	public async Task NestedModelTarget_FromAttributeData_ReadsItsOwnAttribute()
	{
		var model = LogEventAttributeData.FromAttributeData(AttributeOf("Direct"));

		await Assert.That(model.Exists).IsTrue();
		await Assert.That(model.Level).IsEqualTo(LogLevel.Warning);
		await Assert.That(model.MessageTemplate).IsEqualTo("direct {Value}");
		await Assert.That(model.EventId).IsEqualTo(-1);
	}

	[Test]
	public async Task Generate_NestedModel_CallsUncheckedParse(CancellationToken cancellationToken)
	{
		const string source = """
			using Purview.SourceGeneratorFramework.Examples;
			using Purview.SourceGeneratorFramework.Generators;

			namespace Sample;

			[Generate(typeof(LogEventAttribute))]
			public readonly partial record struct LogEventAttributeData(
				[Argument("level")] [Property(DefaultValue = LogLevel.Information)] LogLevel Level,
				[Argument("messageTemplate")] [Property] string? MessageTemplate,
				[Argument("eventId")] [Property(DefaultValue = -1)] int EventId,
				[Property] string? Name
			);

			[Generate(typeof(InfoEventAttribute))]
			public readonly partial record struct InfoEventAttributeData([NestedModel] LogEventAttributeData Log);
			""";

		var result = await GenerateAsync(source, cancellationToken: cancellationToken);

		result.AssertNoGenerationExceptions().AssertNoLogErrors();

		var infoGenerated = await GetGeneratedStringAsync(
			result,
			"InfoEventAttributeData.AttributeDataModel.g.cs",
			cancellationToken
		);

		await Assert
			.That(infoGenerated)
			.Contains("var log = global::Sample.LogEventAttributeData.FromAttributeDataUnchecked(attributeData);");
		await Assert.That(infoGenerated).DoesNotContain("LogEventAttributeData.FromAttributeData(attributeData)");

		var logGenerated = await GetGeneratedStringAsync(
			result,
			"LogEventAttributeData.AttributeDataModel.g.cs",
			cancellationToken
		);

		await Assert.That(logGenerated).Contains("public static LogEventAttributeData FromAttributeDataUnchecked(");
		await Assert.That(logGenerated).Contains("return FromAttributeDataUnchecked(attributeData);");
		await Assert.That(logGenerated).Contains("attribute = FromAttributeDataUnchecked(attributeData);");
	}

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

public sealed record LogEventAttributeDataTestOptions : SourceGeneratorTestOptions
{
	public LogEventAttributeDataTestOptions()
	{
		AdditionalAssemblyTypes = AdditionalAssemblyTypes.AddRange(
			typeof(TypeIdentity),
			typeof(LogLevel),
			typeof(LogEventAttribute),
			typeof(InfoEventAttribute)
		);
	}
}
