using Purview.SourceGeneratorFramework.Generators;
using Purview.SourceGeneratorFramework.Testing;
using Purview.SourceGeneratorFramework.Testing.TUnit;

namespace Purview.SourceGeneratorFramework.ExampleGenerator;

public record AttributeDataModelTypeLibraryTestOptions : SourceGeneratorTestOptions
{
	public AttributeDataModelTypeLibraryTestOptions()
	{
		AdditionalAssemblyTypes = AdditionalAssemblyTypes.Add(typeof(TypeIdentity));
		AdditionalGeneratorTypes = AdditionalGeneratorTypes.AddRange(
			typeof(TypeLibraryGenerator),
			typeof(SeverityMarkerGenerator)
		);
	}
}

/// <summary>
/// Options for testing the <see cref="Purview.SourceGeneratorFramework.Examples.SeverityAttributeData"/> sample,
/// referencing the real example types.
/// </summary>
public sealed record SeverityAttributeDataTestOptions : AttributeDataModelTypeLibraryTestOptions
{
	public SeverityAttributeDataTestOptions()
	{
		AdditionalAssemblyTypes = AdditionalAssemblyTypes.AddRange(
			typeof(Purview.SourceGeneratorFramework.Examples.SeverityLevel),
			typeof(Purview.SourceGeneratorFramework.Examples.SeverityAttribute)
		);
	}
}

/// <summary>
/// Covers the <see cref="Purview.SourceGeneratorFramework.Examples.SeverityAttributeData"/> sample: the model's
/// <c>[Generate]</c> target references the generated type library's <c>SeverityAttributeFullName</c> constant and
/// its <c>IsEnum</c> defaults are bare member names expanded against the real example attribute.
/// </summary>
public class SeverityAttributeDataSampleTests
	: TUnitSourceGeneratorTestBase<AttributeDataModelGenerator, SeverityAttributeDataTestOptions>
{
	const string SampleSource = """
		using Purview.SourceGeneratorFramework;
		using Purview.SourceGeneratorFramework.Generators;

		namespace Test
		{
			[GenerateTypeLibrary(ClassName = "SampleTypeLibrary")]
			static partial class TypeLibrarySpec
			{
				[TypeRef("Purview.SourceGeneratorFramework.Examples", GenerateFullNameConst = true)]
				static readonly TypeIdentity SeverityAttribute = default;
			}

			[Generate(SampleTypeLibrary.Purview.SourceGeneratorFramework.Examples.SeverityAttributeFullName)]
			public readonly partial record struct SeverityAttributeData(
				[Argument(IsEnum = true, Name = "severity", DefaultValue = "Inherit")] string Severity,
				[Property(IsEnum = true, DefaultValue = "Inherit")] string Level
			);
		}
		""";

	[Test]
	public async Task Generate_Sample_ResolvesTargetAndExpandsEnumDefaults(CancellationToken cancellationToken)
	{
		var result = await GenerateAsync(SampleSource, cancellationToken: cancellationToken);

		result.AssertNoGenerationExceptions().AssertNoLogErrors();

		var generated = await GetGeneratedStringAsync(
			result,
			"SeverityAttributeData.AttributeDataModel.g.cs",
			cancellationToken
		);

		await Assert.That(generated).IsNotNull();
		await Assert
			.That(generated)
			.Contains("new(\"SeverityAttribute\", \"Purview.SourceGeneratorFramework.Examples\")");
		await Assert
			.That(generated)
			.Contains(
				"attributeData.GetEnumConstructorArgument(\"severity\", \"Purview.SourceGeneratorFramework.Examples.SeverityLevel.Inherit\")"
			);
		await Assert
			.That(generated)
			.Contains(
				"attributeData.GetEnumNamedArgument(\"Level\", \"Purview.SourceGeneratorFramework.Examples.SeverityLevel.Inherit\")"
			);
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

/// <summary>
/// Proves that <see cref="AttributeDataModelGenerator"/> resolves a <c>[Generate]</c> target that references a
/// generated type-library full-name constant (<c>TypeLibrary.{Namespace}.{Member}FullName</c>) even though the
/// <c>TypeLibrary</c> class is emitted by <c>TypeLibraryGenerator</c>'s main pipeline in the same pass (so the
/// constant is not resolvable in this generator's input compilation). The target attribute and enum come from
/// the marker generator's post-initialization output, which is shared across generators.
/// </summary>
public class AttributeDataModelTypeLibraryTests
	: TUnitSourceGeneratorTestBase<AttributeDataModelGenerator, AttributeDataModelTypeLibraryTestOptions>
{
	const string PositiveSource = """
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

	[Test]
	public async Task Generate_TypeLibraryConstTarget_WithBareEnumMemberDefaults(CancellationToken cancellationToken)
	{
		var result = await GenerateAsync(PositiveSource, cancellationToken: cancellationToken);

		result.AssertNoGenerationExceptions().AssertNoLogErrors();

		var generated = await GetGeneratedStringAsync(
			result,
			"SeverityAttributeData.AttributeDataModel.g.cs",
			cancellationToken
		);

		await Assert.That(generated).IsNotNull();
		await Assert.That(generated).Contains("readonly partial record struct SeverityAttributeData");
		await Assert.That(generated).Contains("new(\"SeverityAttribute\", \"Aspire.Hosting.AspireC4\")");
		await Assert
			.That(generated)
			.Contains(
				"attributeData.GetEnumConstructorArgument(\"severity\", \"Aspire.Hosting.AspireC4.LikeC4Severity.Inherit\")"
			);
		await Assert
			.That(generated)
			.Contains(
				"attributeData.GetEnumNamedArgument(\"Level\", \"Aspire.Hosting.AspireC4.LikeC4Severity.Inherit\")"
			);

		// The TypeLibrary class (main-pipeline output) and the model are both present in the final compilation.
		await Assert.That(result.CompilationResult.Compilation.GetTypeByMetadataName("TypeLibrary")).IsNotNull();
	}

	[Test]
	public async Task Generate_LiteralStringTarget_StillWorks(CancellationToken cancellationToken)
	{
		var source = """
			using Purview.SourceGeneratorFramework.Generators;

			namespace Test
			{
				[Generate("Aspire.Hosting.AspireC4.SeverityAttribute")]
				public readonly partial record struct SeverityAttributeData(
					[Property(IsEnum = true, DefaultValue = "Aspire.Hosting.AspireC4.LikeC4Severity.Inherit")] string Level
				);
			}
			""";

		var result = await GenerateAsync(source, cancellationToken: cancellationToken);

		var generated = await GetGeneratedStringAsync(
			result,
			"SeverityAttributeData.AttributeDataModel.g.cs",
			cancellationToken
		);

		await Assert.That(generated).IsNotNull();
		await Assert.That(generated).Contains("new(\"SeverityAttribute\", \"Aspire.Hosting.AspireC4\")");
		// A fully-qualified enum default is emitted unchanged.
		await Assert
			.That(generated)
			.Contains(
				"attributeData.GetEnumNamedArgument(\"Level\", \"Aspire.Hosting.AspireC4.LikeC4Severity.Inherit\")"
			);
	}

	[Test]
	public async Task Generate_TypeOfTarget_StillWorks_WithBareEnumDefault(CancellationToken cancellationToken)
	{
		var source = """
			using Purview.SourceGeneratorFramework.Generators;

			namespace Test
			{
				public enum MyEnum { A, B }

				[Generate(typeof(MyAttribute))]
				public readonly partial record struct MyAttributeData(
					[Property(IsEnum = true, DefaultValue = "B")] string? Value
				);

				public class MyAttribute : System.Attribute
				{
					public MyEnum Value { get; set; }
				}
			}
			""";

		var result = await GenerateAsync(source, cancellationToken: cancellationToken);

		var generated = await GetGeneratedStringAsync(
			result,
			"MyAttributeData.AttributeDataModel.g.cs",
			cancellationToken
		);

		await Assert.That(generated).IsNotNull();
		await Assert.That(generated).Contains("new(\"MyAttribute\", \"Test\")");
		await Assert.That(generated).Contains("attributeData.GetEnumNamedArgument(\"Value\", \"Test.MyEnum.B\")");
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
