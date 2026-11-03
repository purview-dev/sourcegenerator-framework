using Purview.SourceGeneratorFramework.Testing.TUnit;

namespace Purview.SourceGeneratorFramework.Analyzers;

public sealed class AttributeDataModelNamingAnalyzerTests
	: TUnitDiagnosticAnalyzerTestBase<AttributeDataModelNamingAnalyzer>
{
	const string Definitions = """
		using System;
		using Purview.SourceGeneratorFramework.Generators;

		namespace Purview.SourceGeneratorFramework.Generators
		{
			[AttributeUsage(AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
			public sealed class GenerateAttribute : Attribute
			{
				public GenerateAttribute(Type targetAttribute) { }
				public GenerateAttribute(string targetAttributeName) { }
			}

			[AttributeUsage(AttributeTargets.Parameter)]
			public sealed class PropertyAttribute : Attribute
			{
				public PropertyAttribute(object? defaultValue = null) { }
				public string? Name { get; set; }
				public object? DefaultValue { get; set; }
			}

			[AttributeUsage(AttributeTargets.Parameter)]
			public sealed class ArgumentAttribute : Attribute
			{
				public ArgumentAttribute(string? name = null, object? defaultValue = null) { }
				public ArgumentAttribute(int index, object? defaultValue = null) { }
				public string? Name { get; set; }
				public int Index { get; set; } = -1;
			}

			[AttributeUsage(AttributeTargets.Parameter)]
			public sealed class NestedModelAttribute : Attribute { }

			[AttributeUsage(AttributeTargets.Parameter)]
			public sealed class ExcludeAttribute : Attribute { }

			[AttributeUsage(AttributeTargets.Parameter)]
			public sealed class GenericTypeArgumentAttribute : Attribute { }
		}

		namespace Sample
		{
			[AttributeUsage(AttributeTargets.Method)]
			public sealed class LogAttribute : Attribute
			{
				public LogAttribute(int level, string? messageTemplate = null) { }
				public int Level { get; set; }
				public string? MessageTemplate { get; set; }
				public int EventId { get; set; }
				public string? Name { get; set; }
				public string? Category { get; private set; }
			}

			[AttributeUsage(AttributeTargets.Method)]
			public sealed class InfoAttribute : Attribute
			{
				public InfoAttribute() { }
				public InfoAttribute(string? messageTemplate, int eventId = -1) { }
				public string? MessageTemplate { get; set; }
				public int EventId { get; set; }
				public string? Name { get; set; }
			}
		}
		""";

	static string Source(string models) => Definitions + "\nnamespace Sample\n{\n" + models + "\n}\n";

	[Test]
	public async Task CorrectlyNamedModel_ReportsNothing(CancellationToken cancellationToken)
	{
		var source = Source(
			"""
			[Generate(typeof(LogAttribute))]
			public readonly partial record struct LogAttributeData(
				[Argument("level")] [Property] int Level,
				[Argument] [Property] string? MessageTemplate,
				int EventId,
				[Property(Name = "Name")] string? EventName
			);
			"""
		);

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasNoDiagnostics();
	}

	[Test]
	public async Task ConstructorNameDiffersByCase_ReportsMappedNameCaseMismatch(CancellationToken cancellationToken)
	{
		var source = Source(
			"""
			[Generate(typeof(LogAttribute))]
			public readonly partial record struct LogAttributeData([Argument("MessageTemplate")] string? Template);
			"""
		);

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasDiagnostics(1);
		await Assert.That(result).HasDiagnostic(AttributeDataModelNamingAnalyzer.MappedNameCaseMismatch);
	}

	[Test]
	public async Task NamedArgumentDiffersByCase_ReportsMappedNameCaseMismatch(CancellationToken cancellationToken)
	{
		var source = Source(
			"""
			[Generate(typeof(LogAttribute))]
			public readonly partial record struct LogAttributeData([Property(Name = "eventId")] int Id);
			"""
		);

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasDiagnostics(1);
		await Assert.That(result).HasDiagnostic(AttributeDataModelNamingAnalyzer.MappedNameCaseMismatch);
	}

	[Test]
	public async Task NamedArgumentWithoutPublicSetter_ReportsMappedMemberNotFound(CancellationToken cancellationToken)
	{
		var source = Source(
			"""
			[Generate(typeof(LogAttribute))]
			public readonly partial record struct LogAttributeData([Property] string? Category);
			"""
		);

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasDiagnostics(1);
		await Assert.That(result).HasDiagnostic(AttributeDataModelNamingAnalyzer.MappedMemberNotFound);
	}

	[Test]
	public async Task UnannotatedParameterWithNoMatchingProperty_ReportsMappedMemberNotFound(
		CancellationToken cancellationToken
	)
	{
		var source = Source(
			"""
			[Generate(typeof(LogAttribute))]
			public readonly partial record struct LogAttributeData(string? Message);
			"""
		);

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasDiagnostics(1);
		await Assert.That(result).HasDiagnostic(AttributeDataModelNamingAnalyzer.MappedMemberNotFound);
	}

	[Test]
	public async Task BareArgumentWithNoMatchingParameter_ReportsMappedMemberNotFound(
		CancellationToken cancellationToken
	)
	{
		// A bare [Argument] reads the camel-cased constructor parameter "eventId", which LogAttribute lacks.
		var source = Source(
			"""
			[Generate(typeof(LogAttribute))]
			public readonly partial record struct LogAttributeData([Argument] int EventId);
			"""
		);

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasDiagnostics(1);
		await Assert.That(result).HasDiagnostic(AttributeDataModelNamingAnalyzer.MappedMemberNotFound);
	}

	[Test]
	public async Task ExplicitArgumentWithNoMatchingParameter_LeavesItToConstructorMemberNotFound(
		CancellationToken cancellationToken
	)
	{
		var source = Source(
			"""
			[Generate(typeof(LogAttribute))]
			public readonly partial record struct LogAttributeData([Argument("missing")] int EventId);
			"""
		);

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasNoDiagnostics();
	}

	[Test]
	[Arguments("Exists")]
	[Arguments("Empty")]
	[Arguments("TargetAttribute")]
	[Arguments("FromAttributeDataUnchecked")]
	public async Task PropertyNamedLikeGeneratedMember_ReportsConflict(
		string propertyName,
		CancellationToken cancellationToken
	)
	{
		var source = Source(
			$$"""
			[Generate(typeof(LogAttribute))]
			public readonly partial record struct LogAttributeData([Exclude] int Level, bool {{propertyName}});
			"""
		);

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert
			.That(result)
			.HasDiagnostic(AttributeDataModelNamingAnalyzer.PropertyNameConflictsWithGeneratedMember);
	}

	[Test]
	public async Task NestedModel_MemberNotOnOuterAttribute_ReportsInfo(CancellationToken cancellationToken)
	{
		var source = Source(
			"""
			[Generate(typeof(LogAttribute))]
			public readonly partial record struct LogAttributeData(
				[Argument("level")] [Property(DefaultValue = 99)] int Level,
				[Argument("messageTemplate")] [Property] string? MessageTemplate,
				[Property] string? Name
			);

			[Generate(typeof(InfoAttribute))]
			public readonly partial record struct InfoAttributeData([NestedModel] LogAttributeData Log);
			"""
		);

		var result = await AnalyzeAsync(source, cancellationToken);

		// Only Level: InfoAttribute declares messageTemplate and Name.
		await Assert.That(result).HasDiagnostics(1);
		await Assert.That(result).HasDiagnostic(AttributeDataModelNamingAnalyzer.NestedModelMemberNotDeclared);
	}

	[Test]
	public async Task NestedModel_ConstructorIndexNamesDifferentParameter_ReportsMismatch(
		CancellationToken cancellationToken
	)
	{
		// Index 1 is messageTemplate on LogAttribute but eventId on InfoAttribute.
		var source = Source(
			"""
			[Generate(typeof(LogAttribute))]
			public readonly partial record struct LogAttributeData([Argument(1)] string? MessageTemplate);

			[Generate(typeof(InfoAttribute))]
			public readonly partial record struct InfoAttributeData([NestedModel] LogAttributeData Log);
			"""
		);

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasDiagnostics(1);
		await Assert.That(result).HasDiagnostic(AttributeDataModelNamingAnalyzer.NestedModelConstructorIndexMismatch);
	}

	[Test]
	public async Task NestedModel_NameDiffersByCaseOnOuterAttribute_ReportsMappedNameCaseMismatch(
		CancellationToken cancellationToken
	)
	{
		var source = Source(
			"""
			[AttributeUsage(AttributeTargets.Method)]
			public sealed class WarnAttribute : Attribute
			{
				public WarnAttribute(string? MessageTemplate) { }
			}

			[Generate(typeof(LogAttribute))]
			public readonly partial record struct LogAttributeData([Argument("messageTemplate")] string? MessageTemplate);

			[Generate(typeof(WarnAttribute))]
			public readonly partial record struct WarnAttributeData([NestedModel] LogAttributeData Log);
			"""
		);

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasDiagnostics(1);
		await Assert.That(result).HasDiagnostic(AttributeDataModelNamingAnalyzer.MappedNameCaseMismatch);
	}

	[Test]
	public async Task NestedModel_FullyDeclaredOnOuterAttribute_ReportsNothing(CancellationToken cancellationToken)
	{
		var source = Source(
			"""
			[Generate(typeof(LogAttribute))]
			public readonly partial record struct LogAttributeData(
				[Argument("messageTemplate")] [Property] string? MessageTemplate,
				[Property] int EventId,
				[Property] string? Name
			);

			[Generate(typeof(InfoAttribute))]
			public readonly partial record struct InfoAttributeData([NestedModel] LogAttributeData Log);
			"""
		);

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasNoDiagnostics();
	}
}
