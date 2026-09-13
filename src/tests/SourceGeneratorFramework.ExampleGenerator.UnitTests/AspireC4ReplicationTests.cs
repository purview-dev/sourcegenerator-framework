using Purview.SourceGeneratorFramework.Generators;
using Purview.SourceGeneratorFramework.Testing;
using Purview.SourceGeneratorFramework.Testing.TUnit;

namespace Purview.SourceGeneratorFramework.ExampleGenerator;

public sealed record AspireC4ReplicationTestOptions : SourceGeneratorTestOptions
{
	public AspireC4ReplicationTestOptions()
	{
		AdditionalAssemblyTypes = AdditionalAssemblyTypes.Add(typeof(TypeIdentity));
		AdditionalGeneratorTypes = AdditionalGeneratorTypes.AddRange(
			typeof(TypeLibraryGenerator),
			typeof(AspireC4MarkerGenerator)
		);
	}
}

/// <summary>
/// Options for the common self-hosting scenario where the marker attributes/enums are emitted by the consumer's
/// own generator (which does not run on its own project), so they are not resolvable while the models are
/// compiled.
/// </summary>
public sealed record AspireC4AbsentTargetTestOptions : SourceGeneratorTestOptions
{
	public AspireC4AbsentTargetTestOptions()
	{
		AdditionalAssemblyTypes = AdditionalAssemblyTypes.Add(typeof(TypeIdentity));
		AdditionalGeneratorTypes = AdditionalGeneratorTypes.Add(typeof(TypeLibraryGenerator));
	}
}

/// <summary>
/// Replicates the aspirec4 consumer's attribute-data model declarations end-to-end: a
/// <c>[GenerateTypeLibrary]</c> spec, models whose <c>[Generate]</c> targets reference generated
/// <c>{Member}FullName</c> constants, and the marker attributes/enums emitted as post-initialization output.
/// Proves the TypeLibrary-path target resolution and the bare enum-member default expansion work against the
/// consumer's exact shapes (including the positional <c>[Property("Inherit", IsEnum = true)]</c> form and
/// non-nullable <c>string</c> model properties).
/// </summary>
public class AspireC4ReplicationTests
	: TUnitSourceGeneratorTestBase<AttributeDataModelGenerator, AspireC4ReplicationTestOptions>
{
	const string Source = """
		using Purview.SourceGeneratorFramework;
		using Purview.SourceGeneratorFramework.Generators;

		namespace Aspire.Hosting.AspireC4.SourceGenerators.Helpers
		{
			[GenerateTypeLibrary]
			static partial class TypeLibraryGenerator
			{
				const string AspireC4Namespace = "Aspire.Hosting.AspireC4";

				[TypeRef(AspireC4Namespace, generateFullNameConst: true)]
				static readonly TypeIdentity LikeC4RegistryAttribute = default;

				[TypeRef(AspireC4Namespace, generateFullNameConst: true)]
				static readonly TypeIdentity SeverityAttribute = default;

				[TypeRef(AspireC4Namespace, generateFullNameConst: true)]
				static readonly TypeIdentity KnownTypeAttribute = default;

				[TypeRef(AspireC4Namespace, generateFullNameConst: true)]
				static readonly TypeIdentity LikeC4RegistryType = default;

				[TypeRef(AspireC4Namespace, generateFullNameConst: true)]
				static readonly TypeIdentity LikeC4Severity = default;

				[TypeRef(AspireC4Namespace)]
				static readonly TypeIdentity LikeC4StrictValidatorGenerator = default;
			}
		}

		namespace Aspire.Hosting.AspireC4.SourceGenerators.Models
		{
			[Generate(TypeLibrary.Aspire.Hosting.AspireC4.LikeC4RegistryAttributeFullName)]
			readonly partial record struct LikeC4RegistryAttributeData(
				[Property(DefaultValue = "Inherit", IsEnum = true)] string Strict
			);

			[Generate(TypeLibrary.Aspire.Hosting.AspireC4.KnownTypeAttributeFullName)]
			readonly partial record struct KnownTypesAttributeData(
				[Argument(IsEnum = true, Name = "type", DefaultValue = "Tag")] string Type,
				[Property("Inherit", IsEnum = true)] string Strict
			);

			[Generate(TypeLibrary.Aspire.Hosting.AspireC4.SeverityAttributeFullName)]
			readonly partial record struct SeverityAttributeData(
				[Argument(IsEnum = true, Name = "severity", DefaultValue = "Inherit")] string Severity
			);
		}
		""";

	[Test]
	public async Task Generate_ConsumerModels_ResolveTargetsAndExpandEnumDefaults(CancellationToken cancellationToken)
	{
		var result = await GenerateAsync(Source, cancellationToken: cancellationToken);

		result.AssertNoGenerationExceptions().AssertNoLogErrors();

		var registry = await GetGeneratedStringAsync(
			result,
			"LikeC4RegistryAttributeData.AttributeDataModel.g.cs",
			cancellationToken
		);
		await Assert.That(registry).IsNotNull();
		await Assert.That(registry).Contains("new(\"LikeC4RegistryAttribute\", \"Aspire.Hosting.AspireC4\")");
		await Assert
			.That(registry)
			.Contains(
				"attributeData.GetEnumNamedArgument(\"Strict\", \"Aspire.Hosting.AspireC4.LikeC4Severity.Inherit\")"
			);
		await Assert.That(registry).DoesNotContain("attributeData.GetEnumNamedArgument(\"Strict\", \"Inherit\")");

		var knownTypes = await GetGeneratedStringAsync(
			result,
			"KnownTypesAttributeData.AttributeDataModel.g.cs",
			cancellationToken
		);
		await Assert.That(knownTypes).IsNotNull();
		await Assert.That(knownTypes).Contains("new(\"KnownTypeAttribute\", \"Aspire.Hosting.AspireC4\")");
		await Assert
			.That(knownTypes)
			.Contains(
				"attributeData.GetEnumConstructorArgument(\"type\", \"Aspire.Hosting.AspireC4.LikeC4RegistryType.Tag\")"
			);
		await Assert
			.That(knownTypes)
			.Contains(
				"attributeData.GetEnumNamedArgument(\"Strict\", \"Aspire.Hosting.AspireC4.LikeC4Severity.Inherit\")"
			);

		var severity = await GetGeneratedStringAsync(
			result,
			"SeverityAttributeData.AttributeDataModel.g.cs",
			cancellationToken
		);
		await Assert.That(severity).IsNotNull();
		await Assert.That(severity).Contains("new(\"SeverityAttribute\", \"Aspire.Hosting.AspireC4\")");
		await Assert
			.That(severity)
			.Contains(
				"attributeData.GetEnumConstructorArgument(\"severity\", \"Aspire.Hosting.AspireC4.LikeC4Severity.Inherit\")"
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
/// Covers the common self-hosting scenario: the target attribute/enum types are emitted only by the consumer's
/// own generator via post-initialization output, which does not run on the project being compiled, so they are
/// not resolvable in the compilation. The models must still generate using the reconstructed type-library
/// identity, with bare enum-member defaults kept verbatim.
/// </summary>
public class AspireC4AbsentTargetTests
	: TUnitSourceGeneratorTestBase<AttributeDataModelGenerator, AspireC4AbsentTargetTestOptions>
{
	const string Source = """
		using Purview.SourceGeneratorFramework;
		using Purview.SourceGeneratorFramework.Generators;

		namespace Aspire.Hosting.AspireC4.SourceGenerators.Helpers
		{
			[GenerateTypeLibrary]
			static partial class TypeLibraryGenerator
			{
				const string AspireC4Namespace = "Aspire.Hosting.AspireC4";

				[TypeRef(AspireC4Namespace, generateFullNameConst: true)]
				static readonly TypeIdentity LikeC4RegistryAttribute = default;

				[TypeRef(AspireC4Namespace, generateFullNameConst: true)]
				static readonly TypeIdentity SeverityAttribute = default;

				[TypeRef(AspireC4Namespace, generateFullNameConst: true)]
				static readonly TypeIdentity KnownTypeAttribute = default;

				[TypeRef(AspireC4Namespace, generateFullNameConst: true)]
				static readonly TypeIdentity LikeC4RegistryType = default;

				[TypeRef(AspireC4Namespace, generateFullNameConst: true)]
				static readonly TypeIdentity LikeC4Severity = default;
			}
		}

		namespace Aspire.Hosting.AspireC4.SourceGenerators.Models
		{
			[Generate(TypeLibrary.Aspire.Hosting.AspireC4.LikeC4RegistryAttributeFullName)]
			readonly partial record struct LikeC4RegistryAttributeData(
				[Property(DefaultValue = "Inherit", IsEnum = true)] string Strict
			);

			[Generate(TypeLibrary.Aspire.Hosting.AspireC4.KnownTypeAttributeFullName)]
			readonly partial record struct KnownTypesAttributeData(
				[Argument(IsEnum = true, Name = "type", DefaultValue = "Tag")] string Type,
				[Property("Inherit", IsEnum = true)] string Strict
			);

			[Generate(TypeLibrary.Aspire.Hosting.AspireC4.SeverityAttributeFullName)]
			readonly partial record struct SeverityAttributeData(
				[Argument(IsEnum = true, Name = "severity", DefaultValue = "Inherit")] string Severity
			);
		}
		""";

	[Test]
	public async Task Generate_UnresolvableTargetTypes_StillGeneratesWithReconstructedIdentity(
		CancellationToken cancellationToken
	)
	{
		var result = await GenerateAsync(Source, cancellationToken: cancellationToken);

		result.AssertNoGenerationExceptions().AssertNoLogErrors();

		var registry = await GetGeneratedStringAsync(
			result,
			"LikeC4RegistryAttributeData.AttributeDataModel.g.cs",
			cancellationToken
		);
		await Assert.That(registry).IsNotNull();
		await Assert.That(registry).Contains("new(\"LikeC4RegistryAttribute\", \"Aspire.Hosting.AspireC4\")");
		await Assert.That(registry).Contains("attributeData.GetEnumNamedArgument(\"Strict\", \"Inherit\")");

		var knownTypes = await GetGeneratedStringAsync(
			result,
			"KnownTypesAttributeData.AttributeDataModel.g.cs",
			cancellationToken
		);
		await Assert.That(knownTypes).IsNotNull();
		await Assert.That(knownTypes).Contains("new(\"KnownTypeAttribute\", \"Aspire.Hosting.AspireC4\")");
		await Assert.That(knownTypes).Contains("attributeData.GetEnumConstructorArgument(\"type\", \"Tag\")");
		await Assert.That(knownTypes).Contains("attributeData.GetEnumNamedArgument(\"Strict\", \"Inherit\")");

		var severity = await GetGeneratedStringAsync(
			result,
			"SeverityAttributeData.AttributeDataModel.g.cs",
			cancellationToken
		);
		await Assert.That(severity).IsNotNull();
		await Assert.That(severity).Contains("new(\"SeverityAttribute\", \"Aspire.Hosting.AspireC4\")");
		await Assert.That(severity).Contains("attributeData.GetEnumConstructorArgument(\"severity\", \"Inherit\")");

		// The enum full names cannot be derived without the enum types, so the bare member names are kept.
		await Assert.That(registry).DoesNotContain("Aspire.Hosting.AspireC4.LikeC4Severity.Inherit");
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
