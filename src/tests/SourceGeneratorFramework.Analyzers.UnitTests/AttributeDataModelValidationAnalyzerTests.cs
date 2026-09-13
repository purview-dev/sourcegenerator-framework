using Microsoft.CodeAnalysis;
using Purview.SourceGeneratorFramework.Testing;
using Purview.SourceGeneratorFramework.Testing.TUnit;

namespace Purview.SourceGeneratorFramework.Analyzers;

public sealed class AttributeDataModelValidationAnalyzerTests
	: TUnitDiagnosticAnalyzerTestBase<AttributeDataModelValidationAnalyzer>
{
	const string AttributeDefinition = """
		using System;
		using Microsoft.CodeAnalysis;
		using Purview.SourceGeneratorFramework;
		using Purview.SourceGeneratorFramework.Generators;

		namespace Purview.SourceGeneratorFramework.Generators
		{
			[AttributeUsage(AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
			public sealed class GenerateAttribute : Attribute
			{
				public GenerateAttribute(Type targetAttribute) { }
				public GenerateAttribute(string targetAttributeName) { }
				public bool MatchByInheritance { get; set; }
				public bool AutoDiscover { get; set; }
			}

			[AttributeUsage(AttributeTargets.Parameter)]
			public sealed class PropertyAttribute : Attribute
			{
				public PropertyAttribute(object? defaultValue = null) { }
				public string? Name { get; set; }
				public object? DefaultValue { get; set; }
				public bool IsEnum { get; set; }
			}

			[AttributeUsage(AttributeTargets.Parameter)]
			public sealed class ArgumentAttribute : Attribute
			{
				public ArgumentAttribute(string? name = null, object? defaultValue = null) { }
				public ArgumentAttribute(int index, object? defaultValue = null) { }
				public string? Name { get; set; }
				public int Index { get; set; } = -1;
				public object? DefaultValue { get; set; }
				public bool IsEnum { get; set; }
			}

			[AttributeUsage(AttributeTargets.Parameter)]
			public sealed class NestedModelAttribute : Attribute { }

			[AttributeUsage(AttributeTargets.Parameter)]
			public sealed class ExcludeAttribute : Attribute { }

			[AttributeUsage(AttributeTargets.Parameter)]
			public sealed class GenericTypeArgumentAttribute : Attribute
			{
				public GenericTypeArgumentAttribute() { }
				public GenericTypeArgumentAttribute(int index) { }
				public GenericTypeArgumentAttribute(string name) { }
				public string? Name { get; set; }
				public int Index { get; set; } = -1;
			}

			public sealed class TestAttribute : Attribute
			{
				public TestAttribute(string mode) { }
				public string? Mode { get; set; }
			}
		}

		namespace Purview.SourceGeneratorFramework
		{
			public readonly record struct TypeIdentity;
		}
		""";

	[Test]
	public async Task Generate_WithNoTarget_ReportsTargetAttributeNotResolved(CancellationToken cancellationToken)
	{
		var source =
			AttributeDefinition
			+ """
				[Generate]
				public readonly record struct MyModel(bool Enabled);
				""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasDiagnostics(1);
		await Assert.That(result).HasDiagnostic(AttributeDataModelValidationAnalyzer.TargetAttributeNotResolved.Id);
	}

	[Test]
	public async Task Generate_TypeLibraryConstTarget_DoesNotReportTargetAttributeNotResolved(
		CancellationToken cancellationToken
	)
	{
		var source =
			AttributeDefinition
			+ """
				namespace Purview.SourceGeneratorFramework.Generators
				{
					[global::System.AttributeUsage(global::System.AttributeTargets.Class)]
					public sealed class GenerateTypeLibraryAttribute : global::System.Attribute
					{
						public string? ClassName { get; set; }
					}

					[global::System.AttributeUsage(global::System.AttributeTargets.Field)]
					public sealed class TypeRefAttribute : global::System.Attribute
					{
						public TypeRefAttribute(string @namespace) { }
					}
				}

				namespace Aspire.Hosting.AspireC4
				{
					public enum LikeC4Severity { Inherit, Info }

					[global::System.AttributeUsage(global::System.AttributeTargets.All)]
					public sealed class SeverityAttribute : global::System.Attribute
					{
						public SeverityAttribute(LikeC4Severity severity) { }
						public LikeC4Severity Severity { get; set; }
					}
				}

				namespace Test
				{
					[GenerateTypeLibrary]
					static partial class TypeLibrarySpec
					{
						[TypeRef("Aspire.Hosting.AspireC4")]
						static readonly TypeIdentity SeverityAttribute = default;
					}

					[Generate(TypeLibrary.Aspire.Hosting.AspireC4.SeverityAttributeFullName, AutoDiscover = true)]
					public readonly partial record struct SeverityAttributeData;
				}
				""";

		var result = await AnalyzeAsync(source, cancellationToken);

		// The TypeLibrary class is emitted by TypeLibraryGenerator's main pipeline, so the const reference is
		// unresolved in this compilation; the analyzer reassembles the target from the member-access expression
		// and resolves it to the source-declared SeverityAttribute, so ADM0001/ADM0007 must not fire.
		await Assert.That(result).HasNoDiagnostics();
	}

	[Test]
	public async Task Generate_TypeLibraryConstTarget_UnresolvableType_ReportsTargetAttributeNotResolved(
		CancellationToken cancellationToken
	)
	{
		var source =
			AttributeDefinition
			+ """
				namespace Purview.SourceGeneratorFramework.Generators
				{
					[global::System.AttributeUsage(global::System.AttributeTargets.Class)]
					public sealed class GenerateTypeLibraryAttribute : global::System.Attribute
					{
						public string? ClassName { get; set; }
					}

					[global::System.AttributeUsage(global::System.AttributeTargets.Field)]
					public sealed class TypeRefAttribute : global::System.Attribute
					{
						public TypeRefAttribute(string @namespace) { }
					}
				}

				namespace Test
				{
					[GenerateTypeLibrary]
					static partial class TypeLibrarySpec
					{
						[TypeRef("Aspire.Hosting.AspireC4")]
						static readonly TypeIdentity Something = default;
					}

					[Generate(TypeLibrary.Aspire.Hosting.AspireC4.NonexistentAttributeFullName)]
					public readonly partial record struct SeverityAttributeData;
				}
				""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasDiagnostics(1);
		await Assert.That(result).HasDiagnostic(AttributeDataModelValidationAnalyzer.TargetAttributeNotResolved.Id);
	}

	[Test]
	public async Task ArrayProperty_ReportsPropertyTypeNotSupported(CancellationToken cancellationToken)
	{
		var source =
			AttributeDefinition
			+ """
				[Generate(typeof(TestAttribute))]
				public readonly record struct MyModel(
					string? Mode,
					int[] Values
				);
				""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasDiagnostics(1);
		await Assert.That(result).HasDiagnostic(AttributeDataModelValidationAnalyzer.PropertyTypeNotSupported.Id);
	}

	[Test]
	public async Task ArgumentWithMissingName_ReportsConstructorMemberNotFound(CancellationToken cancellationToken)
	{
		var source =
			AttributeDefinition
			+ """
				[Generate(typeof(TestAttribute))]
				public readonly record struct MyModel(
					[Argument("missing")] string? Mode
				);
				""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasDiagnostics(1);
		await Assert.That(result).HasDiagnostic(AttributeDataModelValidationAnalyzer.ConstructorMemberNotFound.Id);
	}

	[Test]
	public async Task ArgumentWithOutOfRangeIndex_ReportsConstructorMemberNotFound(CancellationToken cancellationToken)
	{
		var source =
			AttributeDefinition
			+ """
				[Generate(typeof(TestAttribute))]
				public readonly record struct MyModel(
					[Argument(5)] string? Mode
				);
				""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasDiagnostics(1);
		await Assert.That(result).HasDiagnostic(AttributeDataModelValidationAnalyzer.ConstructorMemberNotFound.Id);
	}

	[Test]
	public async Task ArgumentWithValidName_DoesNotReportDiagnostic(CancellationToken cancellationToken)
	{
		var source =
			AttributeDefinition
			+ """
				[Generate(typeof(TestAttribute))]
				public readonly record struct MyModel(
					[Argument("mode")] string? Mode
				);
				""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasNoDiagnostics();
	}

	[Test]
	public async Task NestedModelWithoutGenerate_ReportsNestedModelNotGenerated(CancellationToken cancellationToken)
	{
		var source =
			AttributeDefinition
			+ """
				[Generate(typeof(TestAttribute))]
				public readonly record struct MyModel(
					[NestedModel] NotAModel Model
				);

				public readonly record struct NotAModel;
				""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasDiagnostics(1);
		await Assert.That(result).HasDiagnostic(AttributeDataModelValidationAnalyzer.NestedModelNotGenerated.Id);
	}

	[Test]
	public async Task NestedModelWithGenerate_DoesNotReportDiagnostic(CancellationToken cancellationToken)
	{
		var source =
			AttributeDefinition
			+ """
				[Generate(typeof(TestAttribute))]
				public readonly record struct MyModel(
					[NestedModel] OtherModel Model
				);

				[Generate(typeof(TestAttribute))]
				public readonly record struct OtherModel(string? Mode);
				""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasNoDiagnostics();
	}

	[Test]
	public async Task StringDefaultOnTypedConstant_ReportsDefaultValueNotSupported(CancellationToken cancellationToken)
	{
		var source =
			AttributeDefinition
			+ """
				[Generate("TestAttribute")]
				public readonly record struct MyModel(
					[Property("Test.Mode.Inherit", Name = "Mode")]
					TypedConstant Mode
				);
				""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasDiagnostics(1);
		await Assert.That(result).HasDiagnostic(AttributeDataModelValidationAnalyzer.DefaultValueNotSupported.Id);
	}

	[Test]
	public async Task NonNullableReferenceTypeWithoutDefault_ReportsRequiresDefault(CancellationToken cancellationToken)
	{
		var source =
			AttributeDefinition
			+ """
				[Generate(typeof(TestAttribute))]
				public readonly record struct MyModel(
					string Mode
				);
				""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasDiagnostics(1);
		await Assert
			.That(result)
			.HasDiagnostic(AttributeDataModelValidationAnalyzer.NonNullableReferenceTypeRequiresDefault.Id);
	}

	[Test]
	public async Task NullableReferenceType_DoesNotReportDiagnostic(CancellationToken cancellationToken)
	{
		var source =
			AttributeDefinition
			+ """
				[Generate(typeof(TestAttribute))]
				public readonly record struct MyModel(
					string? Mode
				);
				""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasNoDiagnostics();
	}

	[Test]
	public async Task AutoDiscoverWithStringTarget_ReportsAutoDiscoverRequiresType(CancellationToken cancellationToken)
	{
		var source =
			AttributeDefinition
			+ """
				[Generate("TestAttribute", AutoDiscover = true)]
				public readonly record struct MyModel;
				""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasDiagnostics(1);
		await Assert.That(result).HasDiagnostic(AttributeDataModelValidationAnalyzer.AutoDiscoverRequiresType.Id);
	}

	[Test]
	public async Task AutoDiscoverWithTypeTarget_DoesNotReportDiagnostic(CancellationToken cancellationToken)
	{
		var source =
			AttributeDefinition
			+ """
				[Generate(typeof(TestAttribute), AutoDiscover = true)]
				public readonly record struct MyModel;
				""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasNoDiagnostics();
	}

	[Test]
	public async Task TypeArgumentWithNonTypeIdentityType_ReportsTypeArgumentPropertyTypeInvalid(
		CancellationToken cancellationToken
	)
	{
		var source =
			AttributeDefinition
			+ """
				[Generate(typeof(TestAttribute))]
				public readonly record struct MyModel(
					[GenericTypeArgument] string? TypeArgument
				);
				""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasDiagnostics(1);
		await Assert
			.That(result)
			.HasDiagnostic(AttributeDataModelValidationAnalyzer.TypeArgumentPropertyTypeInvalid.Id);
	}

	[Test]
	public async Task TypeArgumentWithTypeIdentityType_DoesNotReportDiagnostic(CancellationToken cancellationToken)
	{
		var source =
			AttributeDefinition
			+ """
				[Generate(typeof(TestAttribute))]
				public readonly record struct MyModel(
					[GenericTypeArgument] TypeIdentity TypeArgument
				);
				""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasNoDiagnostics();
	}

	[Test]
	public async Task IsEnumWithNonStringType_ReportsIsEnumRequiresStringType(CancellationToken cancellationToken)
	{
		var source =
			AttributeDefinition
			+ """
				[Generate(typeof(TestAttribute))]
				public readonly record struct MyModel(
					[Property(IsEnum = true)] int Mode
				);
				""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasDiagnostics(1);
		await Assert.That(result).HasDiagnostic(AttributeDataModelValidationAnalyzer.IsEnumRequiresStringType.Id);
	}

	[Test]
	public async Task IsEnumWithStringType_DoesNotReportDiagnostic(CancellationToken cancellationToken)
	{
		var source =
			AttributeDefinition
			+ """
				[Generate(typeof(TestAttribute))]
				public readonly record struct MyModel(
					[Property(IsEnum = true)] string? Mode
				);
				""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasNoDiagnostics();
	}

	[Test]
	public async Task ValidModel_DoesNotReportDiagnostic(CancellationToken cancellationToken)
	{
		var source =
			AttributeDefinition
			+ """
				[Generate(typeof(TestAttribute))]
				public readonly record struct MyModel(
					bool Enabled,
					string? Mode
				);
				""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasNoDiagnostics();
	}

	protected override AnalyzerTestOptions OnBeforeRun(
		IEnumerable<string> sources,
		AnalyzerTestOptions options,
		CancellationToken cancellationToken
	)
	{
		var updatedOptions = options with { NullableContextOptions = NullableContextOptions.Enable };
		return base.OnBeforeRun(
			sources,
			updatedOptions.WithAdditionalAssemblyTypes(typeof(ISymbol)),
			cancellationToken
		);
	}
}
