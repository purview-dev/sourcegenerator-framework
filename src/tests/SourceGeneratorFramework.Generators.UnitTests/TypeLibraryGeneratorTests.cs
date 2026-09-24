namespace Purview.SourceGeneratorFramework.Generators;

public class TypeLibraryGeneratorTests : TUnitSourceGeneratorTestBase<TypeLibraryGenerator, TypeLibraryTestOptions>
{
	[Test]
	public async Task Generate_NestedNamespaceShape(CancellationToken cancellationToken)
	{
		var source = """
			using Purview.SourceGeneratorFramework;
			using Purview.SourceGeneratorFramework.Generators;

			namespace Test;

			[GenerateTypeLibrary(ClassName = "SampleTypeLibrary", Namespace = "Test")]
			static partial class TypeLibraryModel
			{
				[TypeRef("Purview.Telemetry")]
				static readonly TypeIdentity ActivitySourceGenerationAttribute = default;

				[TypeRef(typeof(global::System.Diagnostics.Activity))]
				static readonly TypeIdentity Activity = default;

				[TypeRef("ILogger", "Microsoft.Extensions.Logging")]
				static readonly TypeIdentity ILogger = default;
			}
			""";

		var result = await GenerateAsync(source, cancellationToken: cancellationToken);

		var generated = await GetGeneratedStringAsync(result, "Test.TypeLibraryModel.g.cs", cancellationToken);

		await Assert.That(generated).IsNotNull();
		await Assert.That(generated).Contains("public static partial class SampleTypeLibrary");
		await Assert.That(generated).Contains("public const string Namespace = \"Test\";");
		await Assert.That(generated).Contains("public static partial class Purview");
		await Assert.That(generated).Contains("public const string Namespace = \"Purview\";");
		await Assert.That(generated).Contains("public const string Namespace = \"Purview.Telemetry\";");
		await Assert
			.That(generated)
			.Contains(
				"public static readonly global::Purview.SourceGeneratorFramework.TypeIdentity ActivitySourceGenerationAttribute = new(\"ActivitySourceGenerationAttribute\", \"Purview.Telemetry\");"
			);
		await Assert.That(generated).Contains("public static partial class System");
		await Assert.That(generated).Contains("public const string Namespace = \"System\";");
		await Assert.That(generated).Contains("public static partial class Diagnostics");
		await Assert.That(generated).Contains("public const string Namespace = \"System.Diagnostics\";");
		await Assert
			.That(generated)
			.Contains(
				"public static readonly global::Purview.SourceGeneratorFramework.TypeIdentity Activity = new(\"Activity\", \"System.Diagnostics\");"
			);
		await Assert.That(generated).Contains("public static partial class Microsoft");
		await Assert.That(generated).Contains("public static partial class Extensions");
		await Assert.That(generated).Contains("public const string Namespace = \"Microsoft.Extensions\";");
		await Assert.That(generated).Contains("public static partial class Logging");
		await Assert.That(generated).Contains("public const string Namespace = \"Microsoft.Extensions.Logging\";");
		await Assert
			.That(generated)
			.Contains(
				"public static readonly global::Purview.SourceGeneratorFramework.TypeIdentity ILogger = new(\"ILogger\", \"Microsoft.Extensions.Logging\");"
			);
		await Assert.That(generated).DoesNotContain("extension(");
	}

	[Test]
	public async Task Generate_TypeRefNamespaceOnly_UsesMemberNameAsTypeName(CancellationToken cancellationToken)
	{
		var source = """
			using Purview.SourceGeneratorFramework;
			using Purview.SourceGeneratorFramework.Generators;

			namespace Test;

			[GenerateTypeLibrary(ClassName = "SampleTypeLibrary", Namespace = "Test")]
			static partial class TypeLibraryModel
			{
				[TypeRef("Purview.Telemetry")]
				static readonly TypeIdentity ActivitySourceGenerationAttribute = default;
			}
			""";

		var result = await GenerateAsync(source, cancellationToken: cancellationToken);

		var generated = await GetGeneratedStringAsync(result, "Test.TypeLibraryModel.g.cs", cancellationToken);

		await Assert.That(generated).IsNotNull();
		await Assert
			.That(generated)
			.Contains(
				"public static readonly global::Purview.SourceGeneratorFramework.TypeIdentity ActivitySourceGenerationAttribute = new(\"ActivitySourceGenerationAttribute\", \"Purview.Telemetry\");"
			);
	}

	[Test]
	[Arguments(true)]
	[Arguments(false)]
	public async Task Generate_TypeRefGenerateFullNameConstant_IncludesTheFullNameConstant(
		bool asProperty,
		CancellationToken cancellationToken
	)
	{
		var attributeDef = asProperty
			? "[TypeRef(\"Purview.Telemetry\", generateFullNameConst: true)]"
			: "[TypeRef(\"Purview.Telemetry\", GenerateFullNameConst = true)]";

		var source =
			@$"
			using Purview.SourceGeneratorFramework;
			using Purview.SourceGeneratorFramework.Generators;

			namespace Test;

			[GenerateTypeLibrary]
			static partial class TypeLibraryModel
			{{
				{attributeDef}
				static readonly TypeIdentity TestingFullNameGeneration = default;
			}}
			";

		var result = await GenerateAsync(source, cancellationToken: cancellationToken);

		var generated = await GetGeneratedStringAsync(result, "Test.TypeLibraryModel.g.cs", cancellationToken);

		await Assert.That(generated).IsNotNull();
		await Assert
			.That(generated)
			.Contains(
				"public const string TestingFullNameGenerationFullName = \"Purview.Telemetry.TestingFullNameGeneration\";"
			);
	}

	[Test]
	public async Task Generate_TypeRef_GenericArityFromTypeOf(CancellationToken cancellationToken)
	{
		var source = """
			using Purview.SourceGeneratorFramework;
			using Purview.SourceGeneratorFramework.Generators;

			namespace Test;

			[GenerateTypeLibrary(ClassName = "SampleTypeLibrary", Namespace = "Test")]
			static partial class TypeLibraryModel
			{
				[TypeRef(typeof(global::System.Collections.Generic.List<>))]
				static readonly TypeIdentity List = default;
			}
			""";

		var result = await GenerateAsync(source, cancellationToken: cancellationToken);

		var generated = await GetGeneratedStringAsync(result, "Test.TypeLibraryModel.g.cs", cancellationToken);

		await Assert.That(generated).IsNotNull();
		await Assert
			.That(generated)
			.Contains(
				"public static readonly global::Purview.SourceGeneratorFramework.TypeIdentity List = new(\"List\", \"System.Collections.Generic\", 1);"
			);
	}

	[Test]
	public async Task Generate_TypeRef_GenericArityFromBacktickString(CancellationToken cancellationToken)
	{
		var source = """
			using Purview.SourceGeneratorFramework;
			using Purview.SourceGeneratorFramework.Generators;

			namespace Test;

			[GenerateTypeLibrary(ClassName = "SampleTypeLibrary", Namespace = "Test")]
			static partial class TypeLibraryModel
			{
				[TypeRef("List`1", "System.Collections.Generic")]
				static readonly TypeIdentity List = default;
			}
			""";

		var result = await GenerateAsync(source, cancellationToken: cancellationToken);

		var generated = await GetGeneratedStringAsync(result, "Test.TypeLibraryModel.g.cs", cancellationToken);

		await Assert.That(generated).IsNotNull();
		await Assert
			.That(generated)
			.Contains(
				"public static readonly global::Purview.SourceGeneratorFramework.TypeIdentity List = new(\"List\", \"System.Collections.Generic\", 1);"
			);
	}

	[Test]
	public async Task Generate_TypeRef_ExplicitArityOnNamespaceForm(CancellationToken cancellationToken)
	{
		var source = """
			using Purview.SourceGeneratorFramework;
			using Purview.SourceGeneratorFramework.Generators;

			namespace Test;

			[GenerateTypeLibrary(ClassName = "SampleTypeLibrary", Namespace = "Test")]
			static partial class TypeLibraryModel
			{
				[TypeRef("Test", 2)]
				static readonly TypeIdentity Dictionary = default;
			}
			""";

		var result = await GenerateAsync(source, cancellationToken: cancellationToken);

		var generated = await GetGeneratedStringAsync(result, "Test.TypeLibraryModel.g.cs", cancellationToken);

		await Assert.That(generated).IsNotNull();
		await Assert
			.That(generated)
			.Contains(
				"public static readonly global::Purview.SourceGeneratorFramework.TypeIdentity Dictionary = new(\"Dictionary\", \"Test\", 2);"
			);
	}

	[Test]
	public async Task Generate_ReferenceMember_CopiesInitializerAndEmitsNestedField(CancellationToken cancellationToken)
	{
		var source = """
			using Purview.SourceGeneratorFramework;
			using Purview.SourceGeneratorFramework.Generators;

			namespace Test;

			[GenerateTypeLibrary(ClassName = "SampleTypeLibrary", Namespace = "Test")]
			static partial class TypeLibraryModel
			{
				[TypeRef("System.Diagnostics")]
				static readonly TypeIdentity ActivityLink = default;

				[TypeRef("System.Diagnostics")]
				internal static readonly TypeReference ActivityLinkArray = new TypeReference(ActivityLink).MakeArray();

				[TypeRef("System.Collections.Generic")]
				internal static readonly TypeReference ActivityTagIEnumerable =
					global::Purview.SourceGeneratorFramework.PurviewTypeLibrary.System.Collections.Generic.IEnumerable.MakeGeneric(
						global::Purview.SourceGeneratorFramework.PurviewTypeLibrary.System.String
					);
			}
			""";

		var result = await GenerateAsync(source, cancellationToken: cancellationToken);

		var generated = await GetGeneratedStringAsync(result, "Test.TypeLibraryModel.g.cs", cancellationToken);

		await Assert.That(generated).IsNotNull();
		await Assert
			.That(generated)
			.Contains(
				"public static readonly global::Purview.SourceGeneratorFramework.TypeReference ActivityLinkArray = new TypeReference(ActivityLink).MakeArray();"
			);
		await Assert.That(generated).Contains("public const string Namespace = \"System.Collections.Generic\";");
		await Assert
			.That(generated)
			.Contains(
				"public static readonly global::Purview.SourceGeneratorFramework.TypeReference ActivityTagIEnumerable = global::Purview.SourceGeneratorFramework.PurviewTypeLibrary.System.Collections.Generic.IEnumerable.MakeGeneric("
			);
	}

	[Test]
	public async Task Generate_ValueMember_InitialisedTypeIdentity(CancellationToken cancellationToken)
	{
		var source = """
			using Purview.SourceGeneratorFramework;
			using Purview.SourceGeneratorFramework.Generators;

			namespace Test;

			[GenerateTypeLibrary(ClassName = "SampleTypeLibrary", Namespace = "Test")]
			static partial class TypeLibraryModel
			{
				[TypeRef("System")]
				internal static readonly TypeIdentity StringType = global::Purview.SourceGeneratorFramework.PurviewTypeLibrary.System.String;
			}
			""";

		var result = await GenerateAsync(source, cancellationToken: cancellationToken);

		var generated = await GetGeneratedStringAsync(result, "Test.TypeLibraryModel.g.cs", cancellationToken);

		await Assert.That(generated).IsNotNull();
		await Assert
			.That(generated)
			.Contains(
				"public static readonly global::Purview.SourceGeneratorFramework.TypeIdentity StringType = global::Purview.SourceGeneratorFramework.PurviewTypeLibrary.System.String;"
			);
	}

	[Test]
	public async Task Generate_GlobalNamespaceAndCustomName(CancellationToken cancellationToken)
	{
		var source = """
			using Purview.SourceGeneratorFramework;
			using Purview.SourceGeneratorFramework.Generators;

			[GenerateTypeLibrary(ClassName = "MyTypeLibrary")]
			static partial class TypeLibraryModel
			{
				[TypeRef("Test")]
				static readonly TypeIdentity MyAttribute = default;
			}
			""";

		var result = await GenerateAsync(source, cancellationToken: cancellationToken);

		var generated = await GetGeneratedStringAsync(result, "TypeLibraryModel.g.cs", cancellationToken);

		await Assert.That(generated).IsNotNull();
		await Assert.That(generated).Contains("public static partial class MyTypeLibrary");
		await Assert.That(generated).Contains("public const string Namespace = \"\";");
		await Assert
			.That(generated)
			.Contains(
				"public static readonly global::Purview.SourceGeneratorFramework.TypeIdentity MyAttribute = new(\"MyAttribute\", \"Test\");"
			);
	}

	[Test]
	public async Task Generate_InheritsFrameworkMembers(CancellationToken cancellationToken)
	{
		var source = """
			using Purview.SourceGeneratorFramework;
			using Purview.SourceGeneratorFramework.Generators;

			namespace Test;

			[GenerateTypeLibrary(ClassName = "SampleTypeLibrary", Namespace = "Test")]
			static partial class TypeLibraryModel
			{
				[TypeRef("System.Diagnostics")]
				static readonly TypeIdentity Debug = default;
			}
			""";

		var result = await GenerateAsync(source, cancellationToken: cancellationToken);

		var generated = await GetGeneratedStringAsync(result, "Test.TypeLibraryModel.g.cs", cancellationToken);

		await Assert.That(generated).IsNotNull();
		await Assert
			.That(generated)
			.Contains(
				"public static readonly global::Purview.SourceGeneratorFramework.TypeIdentity String = global::Purview.SourceGeneratorFramework.PurviewTypeLibrary.System.String;"
			);
		await Assert
			.That(generated)
			.Contains(
				"public static readonly global::Purview.SourceGeneratorFramework.TypeIdentity List = global::Purview.SourceGeneratorFramework.PurviewTypeLibrary.System.Collections.Generic.List;"
			);
		await Assert.That(generated).Contains("public static partial class DependencyInjection");
		await Assert
			.That(generated)
			.Contains("public const string Namespace = \"Microsoft.Extensions.DependencyInjection\";");
		await Assert
			.That(generated)
			.Contains(
				"public static readonly global::Purview.SourceGeneratorFramework.TypeIdentity IServiceCollection = global::Purview.SourceGeneratorFramework.PurviewTypeLibrary.Microsoft.Extensions.DependencyInjection.IServiceCollection;"
			);
	}

	[Test]
	public async Task Generate_UserMemberShadowsInherited(CancellationToken cancellationToken)
	{
		var source = """
			using Purview.SourceGeneratorFramework;
			using Purview.SourceGeneratorFramework.Generators;

			namespace Test;

			[GenerateTypeLibrary(ClassName = "SampleTypeLibrary", Namespace = "Test")]
			static partial class TypeLibraryModel
			{
				[TypeRef("System")]
				static readonly TypeIdentity String = default;
			}
			""";

		var result = await GenerateAsync(source, cancellationToken: cancellationToken);

		var generated = await GetGeneratedStringAsync(result, "Test.TypeLibraryModel.g.cs", cancellationToken);

		await Assert.That(generated).IsNotNull();
		await Assert
			.That(generated)
			.Contains(
				"public static readonly global::Purview.SourceGeneratorFramework.TypeIdentity String = new(\"String\", \"System\");"
			);
		await Assert
			.That(generated)
			.DoesNotContain("= global::Purview.SourceGeneratorFramework.PurviewTypeLibrary.System.String;");
	}

	[Test]
	public async Task Generate_GetTypes_IncludedMembersOnly(CancellationToken cancellationToken)
	{
		var source = """
			using Purview.SourceGeneratorFramework;
			using Purview.SourceGeneratorFramework.Generators;

			namespace Test;

			[GenerateTypeLibrary(ClassName = "SampleTypeLibrary", Namespace = "Test")]
			static partial class TypeLibraryModel
			{
				[TypeRef("System.Diagnostics", IncludeInGetTypes = true)]
				static readonly TypeIdentity Activity = default;

				[TypeRef("System.Diagnostics")]
				static readonly TypeIdentity Debug = default;

				[TypeRef("System.Diagnostics", IncludeInGetTypes = true)]
				internal static readonly TypeReference ActivityArray = new TypeReference(Activity).MakeArray();
			}
			""";

		var result = await GenerateAsync(source, cancellationToken: cancellationToken);

		var generated = await GetGeneratedStringAsync(result, "Test.TypeLibraryModel.g.cs", cancellationToken);

		await Assert.That(generated).IsNotNull();
		await Assert
			.That(generated)
			.Contains(
				"global::System.Collections.Immutable.ImmutableArray<global::Purview.SourceGeneratorFramework.TypeReference> GetTypes("
			);
		await Assert.That(generated).Contains(") => [Activity, ActivityArray];");
		await Assert.That(generated).DoesNotContain("[Activity, ActivityArray, Debug]");
	}

	[Test]
	public async Task Generate_GetTypes_NoIncludedMembers_NoMethod(CancellationToken cancellationToken)
	{
		var source = """
			using Purview.SourceGeneratorFramework;
			using Purview.SourceGeneratorFramework.Generators;

			namespace Test;

			[GenerateTypeLibrary(ClassName = "SampleTypeLibrary", Namespace = "Test")]
			static partial class TypeLibraryModel
			{
				[TypeRef("System.Diagnostics")]
				static readonly TypeIdentity Debug = default;
			}
			""";

		var result = await GenerateAsync(source, cancellationToken: cancellationToken);

		var generated = await GetGeneratedStringAsync(result, "Test.TypeLibraryModel.g.cs", cancellationToken);

		await Assert.That(generated).IsNotNull();
		await Assert.That(generated).DoesNotContain("GetTypes()");
	}

	[Test]
	public async Task Generate_GetTypes_PositionalIncludeInGetTypes_NamespaceOnlyForm(
		CancellationToken cancellationToken
	)
	{
		var source = """
			using Purview.SourceGeneratorFramework;
			using Purview.SourceGeneratorFramework.Generators;

			namespace Test;

			[GenerateTypeLibrary(ClassName = "SampleTypeLibrary", Namespace = "Test")]
			static partial class TypeLibraryModel
			{
				[TypeRef("System.Diagnostics", 0, true)]
				static readonly TypeIdentity Activity = default;
			}
			""";

		var result = await GenerateAsync(source, cancellationToken: cancellationToken);

		var generated = await GetGeneratedStringAsync(result, "Test.TypeLibraryModel.g.cs", cancellationToken);

		await Assert.That(generated).IsNotNull();
		await Assert.That(generated).Contains(") => [Activity];");
	}

	[Test]
	public async Task Generate_GetTypes_PositionalIncludeInGetTypes_ExplicitForm(CancellationToken cancellationToken)
	{
		var source = """
			using Purview.SourceGeneratorFramework;
			using Purview.SourceGeneratorFramework.Generators;

			namespace Test;

			[GenerateTypeLibrary(ClassName = "SampleTypeLibrary", Namespace = "Test")]
			static partial class TypeLibraryModel
			{
				[TypeRef("Activity", "System.Diagnostics", -1, true)]
				static readonly TypeIdentity Activity = default;
			}
			""";

		var result = await GenerateAsync(source, cancellationToken: cancellationToken);

		var generated = await GetGeneratedStringAsync(result, "Test.TypeLibraryModel.g.cs", cancellationToken);

		await Assert.That(generated).IsNotNull();
		await Assert.That(generated).Contains(") => [Activity];");
	}

	[Test]
	public async Task Generate_GetTypes_NamedCtorIncludeInGetTypes(CancellationToken cancellationToken)
	{
		var source = """
			using Purview.SourceGeneratorFramework;
			using Purview.SourceGeneratorFramework.Generators;

			namespace Test;

			[GenerateTypeLibrary(ClassName = "SampleTypeLibrary", Namespace = "Test")]
			static partial class TypeLibraryModel
			{
				[TypeRef("System.Diagnostics", includeInGetTypes: true)]
				static readonly TypeIdentity Activity = default;
			}
			""";

		var result = await GenerateAsync(source, cancellationToken: cancellationToken);

		var generated = await GetGeneratedStringAsync(result, "Test.TypeLibraryModel.g.cs", cancellationToken);

		await Assert.That(generated).IsNotNull();
		await Assert.That(generated).Contains(") => [Activity];");
	}

	[Test]
	public async Task Generate_TypeRefAttribute_ExposesIncludeInGetTypesCtorParameter(
		CancellationToken cancellationToken
	)
	{
		const string source = "public sealed class UnrelatedType { }";

		var result = await GenerateAsync(source, cancellationToken: cancellationToken);

		var generated = await GetGeneratedStringAsync(result, "TypeRefAttribute.g.cs", cancellationToken);

		await Assert.That(generated).IsNotNull();
		await Assert.That(generated).Contains("bool includeInGetTypes = false");
		await Assert.That(generated).Contains("IncludeInGetTypes = includeInGetTypes;");
	}

	[Test]
	public async Task Generate_FileScopedNamespaces(CancellationToken cancellationToken)
	{
		var source = """
			using Purview.SourceGeneratorFramework;
			using Purview.SourceGeneratorFramework.Generators;

			namespace Test;

			[GenerateTypeLibrary(ClassName = "SampleTypeLibrary", Namespace = "Test")]
			static partial class TypeLibraryModel
			{
				[TypeRef("System.Diagnostics")]
				static readonly TypeIdentity Activity = default;
			}
			""";

		var result = await GenerateAsync(source, cancellationToken: cancellationToken);

		var library = await GetGeneratedStringAsync(result, "Test.TypeLibraryModel.g.cs", cancellationToken);
		await Assert.That(library).IsNotNull();
		await Assert.That(library).Contains("namespace Test;");
		await Assert.That(library).DoesNotContain("namespace Test\n{");

		var typeRefs = await GetGeneratedStringAsync(
			result,
			"TypeLibrary.SampleTypeLibrary.Test.TypeLibraryModel.TypeRefs.g.cs",
			cancellationToken
		);
		await Assert.That(typeRefs).IsNotNull();
		await Assert.That(typeRefs).Contains("namespace Test;");
		await Assert.That(typeRefs).DoesNotContain("namespace Test\n{");
		await Assert.That(typeRefs).Contains("TypeRefMarkers");
		await Assert.That(typeRefs).Contains("[Activity]");
	}

	[Test]
	public async Task Generate_MarkerWithoutDefaultInitializer_StillGenerates(CancellationToken cancellationToken)
	{
		var source = """
			using Purview.SourceGeneratorFramework;
			using Purview.SourceGeneratorFramework.Generators;

			namespace Test;

			[GenerateTypeLibrary(ClassName = "SampleTypeLibrary", Namespace = "Test")]
			static partial class TypeLibraryModel
			{
				[TypeRef("Purview.Telemetry")]
				static readonly TypeIdentity ActivitySourceGenerationAttribute;
			}
			""";

		var result = await GenerateAsync(source, cancellationToken: cancellationToken);

		// TLB0010 is non-blocking and reported by the analyzer; the generator still emits the library.
		await Assert.That(result.DriverResult.Diagnostics).DoesNotContain(d => d.Id == "TLB0010");

		var generated = await GetGeneratedStringAsync(result, "Test.TypeLibraryModel.g.cs", cancellationToken);

		await Assert.That(generated).IsNotNull();
		await Assert
			.That(generated)
			.Contains(
				"public static readonly global::Purview.SourceGeneratorFramework.TypeIdentity ActivitySourceGenerationAttribute = new(\"ActivitySourceGenerationAttribute\", \"Purview.Telemetry\");"
			);
	}

	[Test]
	public async Task Generate_EnumValues_EmitsValuesGroup(CancellationToken cancellationToken)
	{
		var source = """
			using Purview.SourceGeneratorFramework;
			using Purview.SourceGeneratorFramework.Generators;

			namespace Test;

			[GenerateTypeLibrary(ClassName = "SampleTypeLibrary", Namespace = "Test")]
			static partial class TypeLibraryModel
			{
				[TypeRef("ServiceLifetime", "Test")]
				static readonly TypeIdentity ServiceLifetime = default;

				[EnumValue("ServiceLifetime", "Test", 0)]
				static readonly TypeIdentity Singleton = default;

				[EnumValue("Test.ServiceLifetime", 1)]
				static readonly TypeIdentity Scoped = default;

				[EnumValue("ServiceLifetime", "Test", 2)]
				static readonly TypeIdentity Transient = default;
			}
			""";

		var result = await GenerateAsync(source, cancellationToken: cancellationToken);

		var generated = await GetGeneratedStringAsync(result, "Test.TypeLibraryModel.g.cs", cancellationToken);

		await Assert.That(generated).IsNotNull();
		await Assert.That(generated).Contains("public static partial class ServiceLifetimeValues");
		await Assert
			.That(generated)
			.Contains(
				"public static readonly global::Purview.SourceGeneratorFramework.EnumValueDefinition Singleton = new(ServiceLifetime, \"Singleton\", 0);"
			);
		await Assert
			.That(generated)
			.Contains(
				"public static readonly global::Purview.SourceGeneratorFramework.EnumValueDefinition Scoped = new(ServiceLifetime, \"Scoped\", 1);"
			);
		await Assert
			.That(generated)
			.Contains(
				"public static readonly global::Purview.SourceGeneratorFramework.EnumValueDefinition Transient = new(ServiceLifetime, \"Transient\", 2);"
			);
		await Assert
			.That(generated)
			.Contains("public static global::Purview.SourceGeneratorFramework.EnumValueDefinition Get(");
		await Assert.That(generated).Contains("if (Singleton.Matches(name))");
		await Assert
			.That(generated)
			.Contains("return global::Purview.SourceGeneratorFramework.EnumValueDefinition.Empty;");
	}

	[Test]
	public async Task Generate_EnumValues_GenerateFullNameConst_EmitsPerValueConsts(CancellationToken cancellationToken)
	{
		var source = """
			using Purview.SourceGeneratorFramework;
			using Purview.SourceGeneratorFramework.Generators;

			namespace Test;

			[GenerateTypeLibrary(ClassName = "SampleTypeLibrary", Namespace = "Test")]
			static partial class TypeLibraryModel
			{
				[TypeRef("ServiceLifetime", "Test", GenerateFullNameConst = true)]
				static readonly TypeIdentity ServiceLifetime = default;

				[EnumValue("ServiceLifetime", "Test", 0)]
				static readonly TypeIdentity Singleton = default;
			}
			""";

		var result = await GenerateAsync(source, cancellationToken: cancellationToken);

		var generated = await GetGeneratedStringAsync(result, "Test.TypeLibraryModel.g.cs", cancellationToken);

		await Assert.That(generated).IsNotNull();
		await Assert
			.That(generated)
			.Contains("public const string ServiceLifetimeFullName = \"Test.ServiceLifetime\";");
		await Assert
			.That(generated)
			.Contains("public const string SingletonFullName = ServiceLifetimeFullName + \".\" + \"Singleton\";");
	}

	[Test]
	public async Task Generate_EnumValues_WithoutGenerateFullNameConst_NoPerValueConsts(
		CancellationToken cancellationToken
	)
	{
		var source = """
			using Purview.SourceGeneratorFramework;
			using Purview.SourceGeneratorFramework.Generators;

			namespace Test;

			[GenerateTypeLibrary(ClassName = "SampleTypeLibrary", Namespace = "Test")]
			static partial class TypeLibraryModel
			{
				[TypeRef("ServiceLifetime", "Test")]
				static readonly TypeIdentity ServiceLifetime = default;

				[EnumValue("ServiceLifetime", "Test", 0)]
				static readonly TypeIdentity Singleton = default;
			}
			""";

		var result = await GenerateAsync(source, cancellationToken: cancellationToken);

		var generated = await GetGeneratedStringAsync(result, "Test.TypeLibraryModel.g.cs", cancellationToken);

		await Assert.That(generated).IsNotNull();
		await Assert.That(generated).Contains("public static partial class ServiceLifetimeValues");
		await Assert.That(generated).DoesNotContain("SingletonFullName");
	}

	[Test]
	public async Task Generate_EnumValues_Aliases_EmitsCollectionExpression(CancellationToken cancellationToken)
	{
		var source = """
			using Purview.SourceGeneratorFramework;
			using Purview.SourceGeneratorFramework.Generators;

			namespace Test;

			[GenerateTypeLibrary(ClassName = "SampleTypeLibrary", Namespace = "Test")]
			static partial class TypeLibraryModel
			{
				[TypeRef("RegistryType", "Test")]
				static readonly TypeIdentity RegistryType = default;

				[EnumValue("RegistryType", "Test", 0, ["Tag", "Tags"])]
				static readonly TypeIdentity Tag = default;
			}
			""";

		var result = await GenerateAsync(source, cancellationToken: cancellationToken);

		var generated = await GetGeneratedStringAsync(result, "Test.TypeLibraryModel.g.cs", cancellationToken);

		await Assert.That(generated).IsNotNull();
		await Assert
			.That(generated)
			.Contains(
				"public static readonly global::Purview.SourceGeneratorFramework.EnumValueDefinition Tag = new(RegistryType, \"Tag\", 0, [\"Tag\", \"Tags\"]);"
			);
	}

	[Test]
	public async Task Generate_EnumValues_UnderlyingType_EmitsNamedArgument(CancellationToken cancellationToken)
	{
		var source = """
			using Purview.SourceGeneratorFramework;
			using Purview.SourceGeneratorFramework.Generators;

			namespace Test;

			[GenerateTypeLibrary(ClassName = "SampleTypeLibrary", Namespace = "Test")]
			static partial class TypeLibraryModel
			{
				[TypeRef("Status", "Test")]
				static readonly TypeIdentity Status = default;

				[EnumValue("Status", "Test", (byte)5)]
				static readonly TypeIdentity Ready = default;
			}
			""";

		var result = await GenerateAsync(source, cancellationToken: cancellationToken);

		var generated = await GetGeneratedStringAsync(result, "Test.TypeLibraryModel.g.cs", cancellationToken);

		await Assert.That(generated).IsNotNull();
		await Assert
			.That(generated)
			.Contains(
				"public static readonly global::Purview.SourceGeneratorFramework.EnumValueDefinition Ready = new(Status, \"Ready\", 5, underlyingType: EnumUnderlyingType.Byte);"
			);
	}

	[Test]
	public async Task Generate_EnumValues_UInt64Value_EmitsLiteralAndUnderlyingType(CancellationToken cancellationToken)
	{
		var source = """
			using Purview.SourceGeneratorFramework;
			using Purview.SourceGeneratorFramework.Generators;

			namespace Test;

			[GenerateTypeLibrary(ClassName = "SampleTypeLibrary", Namespace = "Test")]
			static partial class TypeLibraryModel
			{
				[TypeRef("Big", "Test")]
				static readonly TypeIdentity Big = default;

				[EnumValue("Big", "Test", 18446744073709551615)]
				static readonly TypeIdentity Maximum = default;
			}
			""";

		var result = await GenerateAsync(source, cancellationToken: cancellationToken);

		var generated = await GetGeneratedStringAsync(result, "Test.TypeLibraryModel.g.cs", cancellationToken);

		await Assert.That(generated).IsNotNull();
		await Assert
			.That(generated)
			.Contains(
				"public static readonly global::Purview.SourceGeneratorFramework.EnumValueDefinition Maximum = new(Big, \"Maximum\", 18446744073709551615, underlyingType: EnumUnderlyingType.UInt64);"
			);
	}

	[Test]
	public async Task Generate_EnumValues_MissingEnumType_DoesNotEmitGroup(CancellationToken cancellationToken)
	{
		var source = """
			using Purview.SourceGeneratorFramework;
			using Purview.SourceGeneratorFramework.Generators;

			namespace Test;

			[GenerateTypeLibrary(ClassName = "SampleTypeLibrary", Namespace = "Test")]
			static partial class TypeLibraryModel
			{
				[EnumValue("ServiceLifetime", "Test", 0)]
				static readonly TypeIdentity Singleton = default;
			}
			""";

		var result = await GenerateAsync(source, cancellationToken: cancellationToken);

		// TLB0017 is blocking, so the type library is not generated at all.
		var generated = await GetGeneratedStringAsync(result, "Test.TypeLibraryModel.g.cs", cancellationToken);

		await Assert.That(generated).IsNull();
	}

	[Test]
	public async Task Generate_EnumValuesFromType_EmitsValuesGroup(CancellationToken cancellationToken)
	{
		var source = """
			using Purview.SourceGeneratorFramework;
			using Purview.SourceGeneratorFramework.Generators;

			namespace Test;

			public enum ServiceLifetime
			{
				Singleton = 0,
				Scoped = 1,
				Transient = 2,
			}

			[GenerateTypeLibrary(ClassName = "SampleTypeLibrary", Namespace = "Test")]
			static partial class TypeLibraryModel
			{
				[TypeRef("ServiceLifetime", "Test")]
				static readonly TypeIdentity ServiceLifetime = default;

				[EnumValues(typeof(ServiceLifetime))]
				static readonly TypeIdentity ServiceLifetimeValues = default;
			}
			""";

		var result = await GenerateAsync(source, cancellationToken: cancellationToken);

		var generated = await GetGeneratedStringAsync(result, "Test.TypeLibraryModel.g.cs", cancellationToken);

		await Assert.That(generated).IsNotNull();
		await Assert.That(generated).Contains("public static partial class ServiceLifetimeValues");
		await Assert
			.That(generated)
			.Contains(
				"public static readonly global::Purview.SourceGeneratorFramework.EnumValueDefinition Singleton = new(ServiceLifetime, \"Singleton\", 0);"
			);
		await Assert
			.That(generated)
			.Contains(
				"public static readonly global::Purview.SourceGeneratorFramework.EnumValueDefinition Scoped = new(ServiceLifetime, \"Scoped\", 1);"
			);
		await Assert
			.That(generated)
			.Contains(
				"public static readonly global::Purview.SourceGeneratorFramework.EnumValueDefinition Transient = new(ServiceLifetime, \"Transient\", 2);"
			);
		await Assert
			.That(generated)
			.Contains("public static global::Purview.SourceGeneratorFramework.EnumValueDefinition Get(");
		await Assert.That(generated).Contains("if (Singleton.Matches(name))");
		await Assert
			.That(generated)
			.Contains("return global::Purview.SourceGeneratorFramework.EnumValueDefinition.Empty;");
	}

	[Test]
	public async Task Generate_EnumValuesFromType_GenerateFullNameConst_EmitsPerValueConsts(
		CancellationToken cancellationToken
	)
	{
		var source = """
			using Purview.SourceGeneratorFramework;
			using Purview.SourceGeneratorFramework.Generators;

			namespace Test;

			public enum ServiceLifetime
			{
				Singleton = 0,
				Scoped = 1,
			}

			[GenerateTypeLibrary(ClassName = "SampleTypeLibrary", Namespace = "Test")]
			static partial class TypeLibraryModel
			{
				[TypeRef("ServiceLifetime", "Test", GenerateFullNameConst = true)]
				static readonly TypeIdentity ServiceLifetime = default;

				[EnumValues(typeof(ServiceLifetime))]
				static readonly TypeIdentity ServiceLifetimeValues = default;
			}
			""";

		var result = await GenerateAsync(source, cancellationToken: cancellationToken);

		var generated = await GetGeneratedStringAsync(result, "Test.TypeLibraryModel.g.cs", cancellationToken);

		await Assert.That(generated).IsNotNull();
		await Assert
			.That(generated)
			.Contains("public const string ServiceLifetimeFullName = \"Test.ServiceLifetime\";");
		await Assert
			.That(generated)
			.Contains("public const string SingletonFullName = ServiceLifetimeFullName + \".\" + \"Singleton\";");
		await Assert
			.That(generated)
			.Contains("public const string ScopedFullName = ServiceLifetimeFullName + \".\" + \"Scoped\";");
	}

	[Test]
	public async Task Generate_EnumValuesFromType_UnderlyingType_EmitsNamedArgument(CancellationToken cancellationToken)
	{
		var source = """
			using Purview.SourceGeneratorFramework;
			using Purview.SourceGeneratorFramework.Generators;

			namespace Test;

			public enum Status : byte
			{
				Ready = 5,
				Busy = 6,
			}

			[GenerateTypeLibrary(ClassName = "SampleTypeLibrary", Namespace = "Test")]
			static partial class TypeLibraryModel
			{
				[TypeRef("Status", "Test")]
				static readonly TypeIdentity Status = default;

				[EnumValues(typeof(Status))]
				static readonly TypeIdentity StatusValues = default;
			}
			""";

		var result = await GenerateAsync(source, cancellationToken: cancellationToken);

		var generated = await GetGeneratedStringAsync(result, "Test.TypeLibraryModel.g.cs", cancellationToken);

		await Assert.That(generated).IsNotNull();
		await Assert
			.That(generated)
			.Contains(
				"public static readonly global::Purview.SourceGeneratorFramework.EnumValueDefinition Ready = new(Status, \"Ready\", 5, underlyingType: EnumUnderlyingType.Byte);"
			);
		await Assert
			.That(generated)
			.Contains(
				"public static readonly global::Purview.SourceGeneratorFramework.EnumValueDefinition Busy = new(Status, \"Busy\", 6, underlyingType: EnumUnderlyingType.Byte);"
			);
	}

	[Test]
	public async Task Generate_EnumValuesFromType_UInt64Value_EmitsLiteralAndUnderlyingType(
		CancellationToken cancellationToken
	)
	{
		var source = """
			using Purview.SourceGeneratorFramework;
			using Purview.SourceGeneratorFramework.Generators;

			namespace Test;

			public enum Big : ulong
			{
				Maximum = 18446744073709551615,
			}

			[GenerateTypeLibrary(ClassName = "SampleTypeLibrary", Namespace = "Test")]
			static partial class TypeLibraryModel
			{
				[TypeRef("Big", "Test")]
				static readonly TypeIdentity Big = default;

				[EnumValues(typeof(Big))]
				static readonly TypeIdentity BigValues = default;
			}
			""";

		var result = await GenerateAsync(source, cancellationToken: cancellationToken);

		var generated = await GetGeneratedStringAsync(result, "Test.TypeLibraryModel.g.cs", cancellationToken);

		await Assert.That(generated).IsNotNull();
		await Assert
			.That(generated)
			.Contains(
				"public static readonly global::Purview.SourceGeneratorFramework.EnumValueDefinition Maximum = new(Big, \"Maximum\", 18446744073709551615, underlyingType: EnumUnderlyingType.UInt64);"
			);
	}

	[Test]
	public async Task Generate_EnumValuesFromType_XmlDocumentation_EmitsEnumMemberDocs(
		CancellationToken cancellationToken
	)
	{
		var source = """
			using Purview.SourceGeneratorFramework;
			using Purview.SourceGeneratorFramework.Generators;

			namespace Test;

			public enum ServiceLifetime
			{
				/// <summary>
				/// A single instance is created and reused for the lifetime of the application.
				/// </summary>
				Singleton = 0,

				/// <summary>
				/// A new instance is created once per scope.
				/// </summary>
				Scoped = 1,
			}

			[GenerateTypeLibrary(ClassName = "SampleTypeLibrary", Namespace = "Test")]
			static partial class TypeLibraryModel
			{
				[TypeRef("ServiceLifetime", "Test")]
				static readonly TypeIdentity ServiceLifetime = default;

				[EnumValues(typeof(ServiceLifetime))]
				static readonly TypeIdentity ServiceLifetimeValues = default;
			}
			""";

		var result = await GenerateAsync(source, cancellationToken: cancellationToken);

		var generated = await GetGeneratedStringAsync(result, "Test.TypeLibraryModel.g.cs", cancellationToken);

		await Assert.That(generated).IsNotNull();
		await Assert
			.That(generated)
			.Contains("A single instance is created and reused for the lifetime of the application.");
		await Assert.That(generated).Contains("A new instance is created once per scope.");
	}

	[Test]
	public async Task Generate_EnumValuesFromType_TypeRefMarkers_ReferencesMarkerField(
		CancellationToken cancellationToken
	)
	{
		var source = """
			using Purview.SourceGeneratorFramework;
			using Purview.SourceGeneratorFramework.Generators;

			namespace Test;

			public enum ServiceLifetime
			{
				Singleton = 0,
				Scoped = 1,
			}

			[GenerateTypeLibrary(ClassName = "SampleTypeLibrary", Namespace = "Test")]
			static partial class TypeLibraryModel
			{
				[TypeRef("ServiceLifetime", "Test")]
				static readonly TypeIdentity ServiceLifetime = default;

				[EnumValues(typeof(ServiceLifetime))]
				static readonly TypeIdentity ServiceLifetimeValues = default;
			}
			""";

		var result = await GenerateAsync(source, cancellationToken: cancellationToken);

		var typeRefs = await GetGeneratedStringAsync(
			result,
			"TypeLibrary.SampleTypeLibrary.Test.TypeLibraryModel.TypeRefs.g.cs",
			cancellationToken
		);

		await Assert.That(typeRefs).IsNotNull();
		await Assert.That(typeRefs).Contains("ServiceLifetimeValues");
		// Enum member names are not spec fields and must not be referenced by the generated partial.
		await Assert.That(typeRefs).DoesNotContain("[Singleton]");
		await Assert.That(typeRefs).DoesNotContain("[Scoped]");
	}

	[Test]
	public async Task Generate_EnumValuesFromType_MissingEnumTypeRef_DoesNotEmitGroup(
		CancellationToken cancellationToken
	)
	{
		var source = """
			using Purview.SourceGeneratorFramework;
			using Purview.SourceGeneratorFramework.Generators;

			namespace Test;

			public enum ServiceLifetime
			{
				Singleton = 0,
			}

			[GenerateTypeLibrary(ClassName = "SampleTypeLibrary", Namespace = "Test")]
			static partial class TypeLibraryModel
			{
				[EnumValues(typeof(ServiceLifetime))]
				static readonly TypeIdentity ServiceLifetimeValues = default;
			}
			""";

		var result = await GenerateAsync(source, cancellationToken: cancellationToken);

		// TLB0017 is blocking, so the type library is not generated at all.
		var generated = await GetGeneratedStringAsync(result, "Test.TypeLibraryModel.g.cs", cancellationToken);

		await Assert.That(generated).IsNull();
	}

	[Test]
	public async Task Generate_EnumValuesFromType_NotAnEnum_DoesNotEmitGroup(CancellationToken cancellationToken)
	{
		var source = """
			using Purview.SourceGeneratorFramework;
			using Purview.SourceGeneratorFramework.Generators;

			namespace Test;

			[GenerateTypeLibrary(ClassName = "SampleTypeLibrary", Namespace = "Test")]
			static partial class TypeLibraryModel
			{
				[EnumValues(typeof(string))]
				static readonly TypeIdentity NotEnum = default;
			}
			""";

		var result = await GenerateAsync(source, cancellationToken: cancellationToken);

		// TLB0020 is blocking, so the type library is not generated at all.
		var generated = await GetGeneratedStringAsync(result, "Test.TypeLibraryModel.g.cs", cancellationToken);

		await Assert.That(generated).IsNull();
	}

	[Test]
	public async Task Generate_EnumValuesFromType_CombinedWithEnumValue_DuplicateMember_DoesNotEmitGroup(
		CancellationToken cancellationToken
	)
	{
		var source = """
			using Purview.SourceGeneratorFramework;
			using Purview.SourceGeneratorFramework.Generators;

			namespace Test;

			public enum ServiceLifetime
			{
				Singleton = 0,
				Scoped = 1,
			}

			[GenerateTypeLibrary(ClassName = "SampleTypeLibrary", Namespace = "Test")]
			static partial class TypeLibraryModel
			{
				[TypeRef("ServiceLifetime", "Test")]
				static readonly TypeIdentity ServiceLifetime = default;

				[EnumValue("ServiceLifetime", "Test", 0)]
				static readonly TypeIdentity Singleton = default;

				[EnumValues(typeof(ServiceLifetime))]
				static readonly TypeIdentity ServiceLifetimeValues = default;
			}
			""";

		var result = await GenerateAsync(source, cancellationToken: cancellationToken);

		// TLB0018 (duplicate member) is blocking, so the type library is not generated at all.
		var generated = await GetGeneratedStringAsync(result, "Test.TypeLibraryModel.g.cs", cancellationToken);

		await Assert.That(generated).IsNull();
	}

	[Test]
	public async Task Generate_EnumValuesFromType_CombinedWithEnumValue_NoOverlap_EmitsAll(
		CancellationToken cancellationToken
	)
	{
		var source = """
			using Purview.SourceGeneratorFramework;
			using Purview.SourceGeneratorFramework.Generators;

			namespace Test;

			public enum ServiceLifetime
			{
				Singleton = 0,
				Scoped = 1,
			}

			[GenerateTypeLibrary(ClassName = "SampleTypeLibrary", Namespace = "Test")]
			static partial class TypeLibraryModel
			{
				[TypeRef("ServiceLifetime", "Test")]
				static readonly TypeIdentity ServiceLifetime = default;

				[EnumValues(typeof(ServiceLifetime))]
				static readonly TypeIdentity ServiceLifetimeValues = default;

				[EnumValue("ServiceLifetime", "Test", 2)]
				static readonly TypeIdentity Transient = default;
			}
			""";

		var result = await GenerateAsync(source, cancellationToken: cancellationToken);

		var generated = await GetGeneratedStringAsync(result, "Test.TypeLibraryModel.g.cs", cancellationToken);

		await Assert.That(generated).IsNotNull();
		await Assert.That(generated).Contains("public static partial class ServiceLifetimeValues");
		await Assert
			.That(generated)
			.Contains(
				"public static readonly global::Purview.SourceGeneratorFramework.EnumValueDefinition Singleton = new(ServiceLifetime, \"Singleton\", 0);"
			);
		await Assert
			.That(generated)
			.Contains(
				"public static readonly global::Purview.SourceGeneratorFramework.EnumValueDefinition Scoped = new(ServiceLifetime, \"Scoped\", 1);"
			);
		await Assert
			.That(generated)
			.Contains(
				"public static readonly global::Purview.SourceGeneratorFramework.EnumValueDefinition Transient = new(ServiceLifetime, \"Transient\", 2);"
			);
	}

	[Test]
	public async Task Generate_EnumValuesFromType_EmptyEnum_EmitsGroupWithGetOnly(CancellationToken cancellationToken)
	{
		var source = """
			using Purview.SourceGeneratorFramework;
			using Purview.SourceGeneratorFramework.Generators;

			namespace Test;

			public enum EmptyEnum
			{
			}

			[GenerateTypeLibrary(ClassName = "SampleTypeLibrary", Namespace = "Test")]
			static partial class TypeLibraryModel
			{
				[TypeRef("EmptyEnum", "Test")]
				static readonly TypeIdentity EmptyEnum = default;

				[EnumValues(typeof(EmptyEnum))]
				static readonly TypeIdentity EmptyEnumValues = default;
			}
			""";

		var result = await GenerateAsync(source, cancellationToken: cancellationToken);

		var generated = await GetGeneratedStringAsync(result, "Test.TypeLibraryModel.g.cs", cancellationToken);

		await Assert.That(generated).IsNotNull();
		await Assert.That(generated).Contains("public static partial class EmptyEnumValues");
		await Assert
			.That(generated)
			.Contains("public static global::Purview.SourceGeneratorFramework.EnumValueDefinition Get(");
		await Assert
			.That(generated)
			.Contains("return global::Purview.SourceGeneratorFramework.EnumValueDefinition.Empty;");
		await Assert.That(generated).DoesNotContain("EnumValueDefinition Singleton");
	}

	[Test]
	public async Task Generate_EnumValueOnTypeRefField_EmitsValuesGroup(CancellationToken cancellationToken)
	{
		var source = """
			using Purview.SourceGeneratorFramework;
			using Purview.SourceGeneratorFramework.Generators;

			namespace Test;

			[GenerateTypeLibrary(ClassName = "SampleTypeLibrary", Namespace = "Test")]
			static partial class TypeLibraryModel
			{
				[TypeRef("ServiceLifetime", "Test")]
				[EnumValue("Singleton", 0)]
				[EnumValue("Scoped", 1)]
				[EnumValue("Transient", 2)]
				static readonly TypeIdentity ServiceLifetime = default;
			}
			""";

		var result = await GenerateAsync(source, cancellationToken: cancellationToken);

		var generated = await GetGeneratedStringAsync(result, "Test.TypeLibraryModel.g.cs", cancellationToken);

		await Assert.That(generated).IsNotNull();
		await Assert.That(generated).Contains("public static partial class ServiceLifetimeValues");
		await Assert
			.That(generated)
			.Contains(
				"public static readonly global::Purview.SourceGeneratorFramework.EnumValueDefinition Singleton = new(ServiceLifetime, \"Singleton\", 0);"
			);
		await Assert
			.That(generated)
			.Contains(
				"public static readonly global::Purview.SourceGeneratorFramework.EnumValueDefinition Scoped = new(ServiceLifetime, \"Scoped\", 1);"
			);
		await Assert
			.That(generated)
			.Contains(
				"public static readonly global::Purview.SourceGeneratorFramework.EnumValueDefinition Transient = new(ServiceLifetime, \"Transient\", 2);"
			);
		await Assert
			.That(generated)
			.Contains("public static global::Purview.SourceGeneratorFramework.EnumValueDefinition Get(");
		await Assert.That(generated).Contains("if (Singleton.Matches(name))");
	}

	[Test]
	public async Task Generate_EnumValueOnTypeRefField_GenerateFullNameConst_EmitsPerValueConsts(
		CancellationToken cancellationToken
	)
	{
		var source = """
			using Purview.SourceGeneratorFramework;
			using Purview.SourceGeneratorFramework.Generators;

			namespace Test;

			[GenerateTypeLibrary(ClassName = "SampleTypeLibrary", Namespace = "Test")]
			static partial class TypeLibraryModel
			{
				[TypeRef("ServiceLifetime", "Test", GenerateFullNameConst = true)]
				[EnumValue("Singleton", 0)]
				[EnumValue("Scoped", 1)]
				static readonly TypeIdentity ServiceLifetime = default;
			}
			""";

		var result = await GenerateAsync(source, cancellationToken: cancellationToken);

		var generated = await GetGeneratedStringAsync(result, "Test.TypeLibraryModel.g.cs", cancellationToken);

		await Assert.That(generated).IsNotNull();
		await Assert
			.That(generated)
			.Contains("public const string ServiceLifetimeFullName = \"Test.ServiceLifetime\";");
		await Assert
			.That(generated)
			.Contains("public const string SingletonFullName = ServiceLifetimeFullName + \".\" + \"Singleton\";");
		await Assert
			.That(generated)
			.Contains("public const string ScopedFullName = ServiceLifetimeFullName + \".\" + \"Scoped\";");
	}

	[Test]
	public async Task Generate_EnumValueOnTypeRefField_UnderlyingType_EmitsNamedArgument(
		CancellationToken cancellationToken
	)
	{
		var source = """
			using Purview.SourceGeneratorFramework;
			using Purview.SourceGeneratorFramework.Generators;

			namespace Test;

			[GenerateTypeLibrary(ClassName = "SampleTypeLibrary", Namespace = "Test")]
			static partial class TypeLibraryModel
			{
				[TypeRef("Status", "Test")]
				[EnumValue("Ready", (byte)5)]
				[EnumValue("Busy", (byte)6)]
				static readonly TypeIdentity Status = default;
			}
			""";

		var result = await GenerateAsync(source, cancellationToken: cancellationToken);

		var generated = await GetGeneratedStringAsync(result, "Test.TypeLibraryModel.g.cs", cancellationToken);

		await Assert.That(generated).IsNotNull();
		await Assert
			.That(generated)
			.Contains(
				"public static readonly global::Purview.SourceGeneratorFramework.EnumValueDefinition Ready = new(Status, \"Ready\", 5, underlyingType: EnumUnderlyingType.Byte);"
			);
		await Assert
			.That(generated)
			.Contains(
				"public static readonly global::Purview.SourceGeneratorFramework.EnumValueDefinition Busy = new(Status, \"Busy\", 6, underlyingType: EnumUnderlyingType.Byte);"
			);
	}

	[Test]
	public async Task Generate_EnumValueOnTypeRefField_TypeRefMarkers_ReferencesFieldOnce(
		CancellationToken cancellationToken
	)
	{
		var source = """
			using Purview.SourceGeneratorFramework;
			using Purview.SourceGeneratorFramework.Generators;

			namespace Test;

			[GenerateTypeLibrary(ClassName = "SampleTypeLibrary", Namespace = "Test")]
			static partial class TypeLibraryModel
			{
				[TypeRef("ServiceLifetime", "Test")]
				[EnumValue("Singleton", 0)]
				[EnumValue("Scoped", 1)]
				static readonly TypeIdentity ServiceLifetime = default;
			}
			""";

		var result = await GenerateAsync(source, cancellationToken: cancellationToken);

		var typeRefs = await GetGeneratedStringAsync(
			result,
			"TypeLibrary.SampleTypeLibrary.Test.TypeLibraryModel.TypeRefs.g.cs",
			cancellationToken
		);

		await Assert.That(typeRefs).IsNotNull();
		await Assert.That(typeRefs).Contains("ServiceLifetime");
		// Enum member names are not spec fields and must not be referenced by the generated partial.
		await Assert.That(typeRefs).DoesNotContain("[Singleton]");
		await Assert.That(typeRefs).DoesNotContain("[Scoped]");
	}

	[Test]
	public async Task Generate_EnumValueOnTypeRefField_CombinedWithStandaloneMarker_EmitsAll(
		CancellationToken cancellationToken
	)
	{
		var source = """
			using Purview.SourceGeneratorFramework;
			using Purview.SourceGeneratorFramework.Generators;

			namespace Test;

			[GenerateTypeLibrary(ClassName = "SampleTypeLibrary", Namespace = "Test")]
			static partial class TypeLibraryModel
			{
				[TypeRef("ServiceLifetime", "Test")]
				[EnumValue("Singleton", 0)]
				static readonly TypeIdentity ServiceLifetime = default;

				[EnumValue("ServiceLifetime", "Test", 1)]
				static readonly TypeIdentity Scoped = default;

				[EnumValue("ServiceLifetime", "Test", 2)]
				static readonly TypeIdentity Transient = default;
			}
			""";

		var result = await GenerateAsync(source, cancellationToken: cancellationToken);

		var generated = await GetGeneratedStringAsync(result, "Test.TypeLibraryModel.g.cs", cancellationToken);

		await Assert.That(generated).IsNotNull();
		await Assert.That(generated).Contains("public static partial class ServiceLifetimeValues");
		await Assert
			.That(generated)
			.Contains(
				"public static readonly global::Purview.SourceGeneratorFramework.EnumValueDefinition Singleton = new(ServiceLifetime, \"Singleton\", 0);"
			);
		await Assert
			.That(generated)
			.Contains(
				"public static readonly global::Purview.SourceGeneratorFramework.EnumValueDefinition Scoped = new(ServiceLifetime, \"Scoped\", 1);"
			);
		await Assert
			.That(generated)
			.Contains(
				"public static readonly global::Purview.SourceGeneratorFramework.EnumValueDefinition Transient = new(ServiceLifetime, \"Transient\", 2);"
			);
	}

	[Test]
	public async Task Generate_CopiedDocumentation_RendersFrameworkTypesAsInlineCode(
		CancellationToken cancellationToken
	)
	{
		const string source = """
			using Purview.SourceGeneratorFramework;
			using Purview.SourceGeneratorFramework.Generators;

			namespace Test;

			/// <summary>
			/// Spec that documents <see cref="TypeReference"/> and <see cref="String"/>.
			/// </summary>
			[GenerateTypeLibrary(ClassName = "SampleTypeLibrary", Namespace = "Test")]
			static partial class TypeLibraryModel
			{
				/// <summary>
				/// Identity for <see cref="CodeWriter">the writer</see>.
				/// </summary>
				[TypeRef("Purview.Telemetry")]
				static readonly TypeIdentity ActivitySourceGenerationAttribute = default;
			}
			""";

		var result = await GenerateAsync(source, cancellationToken: cancellationToken);

		var generated = await GetGeneratedStringAsync(result, "Test.TypeLibraryModel.g.cs", cancellationToken);

		await Assert.That(generated).IsNotNull();
		// Framework types become inline code so the copied documentation cannot fail to resolve.
		await Assert.That(generated).Contains("<c>TypeReference</c>");
		await Assert.That(generated).Contains("<c>the writer</c>");
		// Non-framework crefs keep their original form (Roslyn expands them when docs are captured).
		await Assert.That(generated).Contains("cref=\"T:System.String\"");
		await Assert.That(generated).DoesNotContain("<c>System.String</c>");
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
