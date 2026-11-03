using Purview.SourceGeneratorFramework.Testing.TUnit.Assertions;

namespace Purview.SourceGeneratorFramework;

public class AttributeDataTestingTests
{
	static readonly int[] ExpectedTags = [1, 2];

	const string Attributes = """
		namespace Sample
		{
			public enum Level { Low = 1, High = 2 }

			[System.AttributeUsage(System.AttributeTargets.Method)]
			public sealed class EventAttribute : System.Attribute
			{
				public EventAttribute(string? messageTemplate, Level level = Level.Low, int eventId = -1) { }
				public string? Name { get; set; }
				public System.Type? Payload { get; set; }
				public int[]? Tags { get; set; }
			}

			[System.AttributeUsage(System.AttributeTargets.Parameter)]
			public sealed class TagAttribute : System.Attribute
			{
				public TagAttribute(string name) { }
			}
		}
		""";

	static SourceGeneratorTestOptions Options =>
		new() { AdditionalSources = [Attributes], AdditionalNamespaces = ["Sample"] };

	[Test]
	public async Task FromUsage_MethodOnlyAttribute_IsPlacedOnAMethod()
	{
		var attribute = AttributeDataFactory.FromUsage("[Event(\"text\")]", Options);

		await Assert.That(attribute.AttributeClass!.Name).IsEqualTo("EventAttribute");
		await Assert.That(attribute).HasConstructorArgument(0, "text");
	}

	[Test]
	public async Task FromUsage_ParameterOnlyAttribute_WithoutBrackets_IsPlacedOnAParameter()
	{
		var attribute = AttributeDataFactory.FromUsage("Tag(\"t\")", Options);

		await Assert.That(attribute).HasConstructorArgument("name", "t");
	}

	[Test]
	public async Task FromUsage_MisspeltParameterName_Throws()
	{
		await Assert
			.That(() => AttributeDataFactory.FromUsage("[Event(messageTempl: \"text\")]", Options))
			.Throws<InvalidOperationException>()
			.WithMessageContaining("CS1739", StringComparison.Ordinal);
	}

	[Test]
	public async Task Arguments_DescribeFormOrdinalAndName()
	{
		var attribute = AttributeDataFactory.FromUsage("[Event(\"text\", eventId: 7, Name = \"N\")]", Options);
		var arguments = AttributeArguments.From(attribute);

		await Assert
			.That(arguments.Constructor.Select(a => a.Form))
			.IsEquivalentTo([
				AttributeArgumentForm.Positional,
				AttributeArgumentForm.Defaulted,
				AttributeArgumentForm.NamedParameter,
			]);
		await Assert
			.That(arguments.Constructor.Select(a => a.Name))
			.IsEquivalentTo(["messageTemplate", "level", "eventId"]);
		await Assert.That(arguments.Named.Single().Form).IsEqualTo(AttributeArgumentForm.NamedArgument);
		await Assert
			.That(arguments.ToString())
			.IsEqualTo(
				"[EventAttribute(#0 messageTemplate: \"text\", #1 level: Level.Low (defaulted), #2 eventId: 7 (by name), Name = \"N\")]"
			);
	}

	[Test]
	public async Task Assertions_ConstructorArgumentsByOrdinalNameAndForm()
	{
		var attribute = AttributeDataFactory.FromUsage("[Event(\"text\", eventId: 7)]", Options);

		await Assert.That(attribute).HasConstructorArgument(0, "text");
		await Assert.That(attribute).HasConstructorArgument("eventId", 7, AttributeArgumentForm.NamedParameter);
		await Assert.That(attribute).HasConstructorArgument("level", "Low", AttributeArgumentForm.Defaulted);
		await Assert.That(attribute).DoesNotHaveConstructorArgument("level");
		await Assert.That(attribute).DoesNotHaveNamedArgument("Name");
	}

	[Test]
	public async Task Assertions_ValuesCompareAcrossEnumTypeAndArrayForms()
	{
		var attribute = AttributeDataFactory.FromUsage(
			"[Event(\"text\", Level.High, Payload = typeof(System.String), Tags = new[] { 1, 2 })]",
			Options
		);

		await Assert.That(attribute).HasConstructorArgument("level", 2);
		await Assert.That(attribute).HasConstructorArgument("level", "Level.High");
		await Assert.That(attribute).HasConstructorArgument("level", "Sample.Level.High");
		await Assert.That(attribute).HasConstructorArgument(1, 2L);
		await Assert.That(attribute).HasNamedArgument("Payload", typeof(string));
		await Assert.That(attribute).HasNamedArgument("Payload", "string");
		await Assert.That(attribute).HasNamedArgument("Tags", ExpectedTags);
	}

	[Test]
	public async Task Assertions_WrongValue_FailsListingEveryArgument()
	{
		var attribute = AttributeDataFactory.FromUsage("[Event(\"text\", Name = \"N\")]", Options);

		await Assert
			.That(async () => await Assert.That(attribute).HasNamedArgument("Name", "Other"))
			.Throws<Exception>()
			.WithMessageContaining("Name = \"N\"", StringComparison.Ordinal);
	}

	[Test]
	public async Task Assertions_NameDiffersByCase_Fails()
	{
		var attribute = AttributeDataFactory.FromUsage("[Event(\"text\")]", Options);

		await Assert
			.That(async () => await Assert.That(attribute).HasConstructorArgument("MessageTemplate", "text"))
			.Throws<Exception>()
			.WithMessageContaining("no constructor parameter 'MessageTemplate'", StringComparison.Ordinal);
	}

	[Test]
	public async Task FromSource_ReadsTypeAndMemberAttributes()
	{
		const string source = """
			[System.Obsolete("type")]
			public interface IEvents
			{
				[Event("member")]
				void Raised();
			}
			""";

		var typeAttributes = AttributeDataFactory.FromSource(source, "IEvents", options: Options);
		var memberAttributes = AttributeDataFactory.FromSource(source, "IEvents", "Raised", Options);

		await Assert.That(typeAttributes.Single()).HasConstructorArgument(0, "type");
		await Assert.That(memberAttributes.Single()).HasConstructorArgument("messageTemplate", "member");
	}
}
