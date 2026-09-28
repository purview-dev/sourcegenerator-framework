using Purview.SourceGeneratorFramework.Analyzers;
using Purview.SourceGeneratorFramework.Testing;
using Purview.SourceGeneratorFramework.Testing.TUnit;

namespace Purview.SourceGeneratorFramework.CodeFixers;

public sealed class MakeTypeLibrarySpecNonPublicCodeFixProviderTests
	: TUnitCodeFixTestBase<TypeLibraryValidationAnalyzer, MakeTypeLibrarySpecNonPublicCodeFixProvider>
{
	const string AttributeDefinition = """
		using System;
		using Microsoft.CodeAnalysis;
		using Purview.SourceGeneratorFramework;
		using Purview.SourceGeneratorFramework.Generators;

		namespace Purview.SourceGeneratorFramework.Generators
		{
			[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
			public sealed class GenerateTypeLibraryAttribute : Attribute
			{
				public string? ClassName { get; set; }
				public string? Namespace { get; set; }
			}

			[AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
			public sealed class TypeRefAttribute : Attribute
			{
				public TypeRefAttribute(string @namespace, int arity = 0)
				{
					Namespace = @namespace;
					Arity = arity;
				}

				public string? Namespace { get; set; }
				public int Arity { get; set; }
			}
		}

		namespace Purview.SourceGeneratorFramework
		{
			public readonly record struct TypeIdentity;
		}
		""";

	const string PublicSpecSource = """
		[GenerateTypeLibrary]
		public static partial class TypeLibraryModel
		{
			[TypeRef("Test")]
			static readonly TypeIdentity MyAttribute = default!;
		}
		""";

	static readonly (string, string)[] MergedComponentProperties =
	[
		("build_property.IsRoslynComponent", "true"),
		("build_property.PurviewEmbedSourceGeneratorFramework", "true"),
		("build_property.IsPackable", "true"),
	];

	[Test]
	public async Task PublicSpecInMergedComponent_BecomesNonPublic(CancellationToken cancellationToken)
	{
		// Arrange
		var options = new CodeFixTestOptions
		{
			EquivalenceKey = MakeTypeLibrarySpecNonPublicCodeFixProvider.EquivalenceKey,
		}.WithAnalyzerConfigOptions(MergedComponentProperties);

		// Act
		var result = await ApplyCodeFixAsync(AttributeDefinition + PublicSpecSource, options, cancellationToken);

		// Assert
		await Assert.That(result).HasDiagnostic(TypeLibraryValidationAnalyzer.SpecShouldBeNonPublic.Id);
		await Assert.That(result.FixedSource).Contains("static partial class TypeLibraryModel");
		await Assert.That(result.FixedSource).DoesNotContain("public static partial class TypeLibraryModel");
	}
}
