using Microsoft.CodeAnalysis.Text;
using Purview.SourceGeneratorFramework.Generators.Helpers;

namespace Purview.SourceGeneratorFramework.Generators.Model;

partial class SourceEmitter
{
	static SourceText GenerateAttribute()
	{
		var writer = CreateWriter(GeneratorTypeLibrary.Attirbutes.GenerateAttribute);

		return writer
			.XmlSummary("Generates parsing members for an attribute-data model.")
			.AttributeClass(
				new(GeneratorTypeLibrary.Attirbutes.GenerateAttribute),
				AttributeTargets.Struct,
				bodyWriter =>
				{
					bodyWriter
						.XmlSummary("Initializes the attribute for the target attribute type.")
						.Constructor(
							new(GeneratorTypeLibrary.Attirbutes.GenerateAttribute, TypeDeclarationAccessibility.Public)
							{
								Parameters = [new("targetAttribute", PurviewTypeLibrary.System.Type)],
							},
							constructorWriter =>
								constructorWriter.Line(
									$"TargetAttribute = targetAttribute ?? throw new global::System.ArgumentNullException(nameof(targetAttribute));"
								)
						);

					bodyWriter
						.XmlSummary("Initializes the attribute for the target attribute name.")
						.Constructor(
							new(GeneratorTypeLibrary.Attirbutes.GenerateAttribute, TypeDeclarationAccessibility.Public)
							{
								Parameters = [new("targetAttributeName", PurviewTypeLibrary.System.String)],
							},
							constructorWriter =>
								constructorWriter.Line(
									$"TargetAttributeName = targetAttributeName ?? throw new global::System.ArgumentNullException(nameof(targetAttributeName));"
								)
						);

					bodyWriter
						.XmlSummary("Gets the attribute type represented by the generated model.")
						.Property(
							new(
								"TargetAttribute",
								PurviewTypeLibrary.System.Type.MakeNullable(),
								TypeDeclarationAccessibility.Public
							)
						);

					bodyWriter
						.XmlSummary("Gets the attribute type name represented by the generated model.")
						.Property(
							new(
								"TargetAttributeName",
								PurviewTypeLibrary.System.String.MakeNullable(),
								TypeDeclarationAccessibility.Public
							)
						);

					bodyWriter
						.XmlSummary("Gets or sets whether derived attribute types are accepted.")
						.Property(
							new(
								"MatchByInheritance",
								PurviewTypeLibrary.System.Boolean,
								TypeDeclarationAccessibility.Public
							)
							{
								IsInitOnly = true,
							}
						);

					bodyWriter
						.XmlSummary("Gets or sets whether the attribute should be automatically discovered.")
						.Property(
							new("AutoDiscover", PurviewTypeLibrary.System.Boolean, TypeDeclarationAccessibility.Public)
							{
								IsInitOnly = true,
							}
						);
				}
			);
	}

	static SourceText PropertyAttribute()
	{
		var writer = CreateWriter(GeneratorTypeLibrary.Attirbutes.PropertyAttribute);

		return writer
			.XmlSummary("Marks a record parameter as a named attribute argument.")
			.AttributeClass(
				new(GeneratorTypeLibrary.Attirbutes.PropertyAttribute),
				AttributeTargets.Parameter,
				bodyWriter =>
				{
					bodyWriter
						.XmlSummary(
							$"Initializes a new instance of the <see cref=\"{GeneratorTypeLibrary.Attirbutes.PropertyAttribute}\"/> class."
						)
						.Constructor(
							new(GeneratorTypeLibrary.Attirbutes.PropertyAttribute, TypeDeclarationAccessibility.Public)
							{
								Parameters =
								[
									new("defaultValue", PurviewTypeLibrary.System.Object.MakeNullable())
									{
										DefaultValue = "null",
									},
								],
							},
							writeBody => writeBody.Line("DefaultValue = defaultValue;")
						);

					bodyWriter
						.XmlSummary("Gets or sets an optional named-property mapping.")
						.Property(
							new(
								"Name",
								PurviewTypeLibrary.System.String.MakeNullable(),
								TypeDeclarationAccessibility.Public
							)
							{
								IsInitOnly = true,
							}
						);

					bodyWriter
						.XmlSummary("Gets or sets the value used when the named argument is not specified.")
						.Property(
							new(
								"DefaultValue",
								PurviewTypeLibrary.System.Object.MakeNullable(),
								TypeDeclarationAccessibility.Public
							)
							{
								IsInitOnly = true,
							}
						);

					bodyWriter
						.XmlSummary(
							"Gets or sets a value indicating whether the property represents an enum whose member name is captured as a string."
						)
						.XmlRemarks(
							"When the default value is a bare member name (for example \"Inherit\"), the generator expands it to the fully-qualified \"{EnumFullName}.{Member}\" form using the target attribute's matching property type."
						)
						.Property(
							new("IsEnum", PurviewTypeLibrary.System.Boolean, TypeDeclarationAccessibility.Public)
							{
								IsInitOnly = true,
							}
						);
				}
			);
	}

	static SourceText ArgumentAttribute()
	{
		var writer = CreateWriter(GeneratorTypeLibrary.Attirbutes.ArgumentAttribute);

		return writer
			.XmlSummary("Marks a record parameter as a constructor argument.")
			.AttributeClass(
				new(GeneratorTypeLibrary.Attirbutes.ArgumentAttribute),
				AttributeTargets.Parameter,
				bodyWriter =>
				{
					bodyWriter
						.XmlSummary(
							$"Initializes a new instance of the <see cref=\"{GeneratorTypeLibrary.Attirbutes.ArgumentAttribute}\"/> class."
						)
						.XmlParam(
							"name",
							"The name of the constructor parameter. If this value is not specified, the parameter name will be used."
						)
						.XmlParam("defaultValue", "The default value of the constructor parameter.")
						.Constructor(
							new(GeneratorTypeLibrary.Attirbutes.ArgumentAttribute, TypeDeclarationAccessibility.Public)
							{
								Parameters =
								[
									new("name", PurviewTypeLibrary.System.String.MakeNullable())
									{
										DefaultValue = "null",
									},
									new("defaultValue", PurviewTypeLibrary.System.Object.MakeNullable())
									{
										DefaultValue = "null",
									},
								],
							},
							writerBody =>
								writerBody.Assignment("Name", "name").Assignment("DefaultValue", "defaultValue")
						);

					bodyWriter
						.XmlSummary(
							$"Initializes a new instance of the <see cref=\"{GeneratorTypeLibrary.Attirbutes.ArgumentAttribute}\"/> class."
						)
						.XmlParam("index", "The index of the constructor parameter.")
						.XmlParam("defaultValue", "The default value of the constructor parameter.")
						.Constructor(
							new(GeneratorTypeLibrary.Attirbutes.ArgumentAttribute, TypeDeclarationAccessibility.Public)
							{
								Parameters =
								[
									new("index", PurviewTypeLibrary.System.Int32),
									new("defaultValue", PurviewTypeLibrary.System.Object.MakeNullable())
									{
										DefaultValue = "null",
									},
								],
							},
							writerBody => writerBody.Line("Index = index;").Line("DefaultValue = defaultValue;")
						);

					bodyWriter
						.XmlSummary("Gets or sets the constructor parameter name.")
						.XmlRemarks(
							"If the <see cref=\"Index\" /> property is -1, this value will be used to match the constructor parameter.",
							"The property uses a camel-case comparison to match the parameter name.",
							$"A property name of {CodeWriter.XmlInlineCode("MyProperty")} will match a constructor parameter named {CodeWriter.XmlInlineCode("myProperty")}."
						)
						.Property(
							new(
								"Name",
								PurviewTypeLibrary.System.String.MakeNullable(),
								TypeDeclarationAccessibility.Public
							)
							{
								IsInitOnly = true,
							}
						);

					bodyWriter
						.XmlSummary("Gets or sets the constructor argument index.")
						.Property(
							new("Index", PurviewTypeLibrary.System.Int32, TypeDeclarationAccessibility.Public)
							{
								IsInitOnly = true,
								Initializer = "-1",
							}
						);

					bodyWriter
						.XmlSummary("Gets or sets the value used when the constructor argument is not specified.")
						.Property(
							new(
								"DefaultValue",
								PurviewTypeLibrary.System.Object.MakeNullable(),
								TypeDeclarationAccessibility.Public
							)
							{
								IsInitOnly = true,
							}
						);

					bodyWriter
						.XmlSummary(
							"Gets or sets a value indicating whether the argument represents an enum whose member name is captured as a string."
						)
						.XmlRemarks(
							"When the default value is a bare member name (for example \"Inherit\"), the generator expands it to the fully-qualified \"{EnumFullName}.{Member}\" form using the target attribute's matching constructor parameter type."
						)
						.Property(
							new("IsEnum", PurviewTypeLibrary.System.Boolean, TypeDeclarationAccessibility.Public)
							{
								IsInitOnly = true,
							}
						);
				}
			);
	}

	static SourceText NestedModelAttribute()
	{
		var writer = CreateWriter(GeneratorTypeLibrary.Attirbutes.NestedModelAttribute);
		return writer
			.XmlSummary("Marks a record parameter as a nested generated attribute-data model.")
			.AttributeClass(
				new(GeneratorTypeLibrary.Attirbutes.NestedModelAttribute),
				AttributeTargets.Parameter,
				bodyWriter => bodyWriter.Comment("Empty")
			);
	}

	static SourceText ExcludeAttribute()
	{
		var writer = CreateWriter(GeneratorTypeLibrary.Attirbutes.ExcludeAttribute);
		return writer
			.XmlSummary("Excludes a record parameter from the generated attribute-data model.")
			.AttributeClass(
				new(GeneratorTypeLibrary.Attirbutes.ExcludeAttribute),
				AttributeTargets.Parameter,
				bodyWriter => bodyWriter.Comment("Empty")
			);
	}

	static SourceText GenericTypeArgumentAttribute()
	{
		var writer = CreateWriter(GeneratorTypeLibrary.Attirbutes.GenericTypeArgumentAttribute);

		return writer
			.XmlSummary("Marks a record parameter as a generic type argument of the attribute class.")
			.AttributeClass(
				new(GeneratorTypeLibrary.Attirbutes.GenericTypeArgumentAttribute),
				AttributeTargets.Parameter,
				bodyWriter =>
				{
					bodyWriter
						.XmlSummary("Initializes a new instance marking the first type argument.")
						.Constructor(
							new(
								GeneratorTypeLibrary.Attirbutes.GenericTypeArgumentAttribute,
								TypeDeclarationAccessibility.Public
							),
							constructorWriter => constructorWriter.Comment("Empty")
						);

					bodyWriter
						.XmlSummary("Initializes a new instance marking the type argument at the specified index.")
						.Constructor(
							new(
								GeneratorTypeLibrary.Attirbutes.GenericTypeArgumentAttribute,
								TypeDeclarationAccessibility.Public
							)
							{
								Parameters = [new("index", PurviewTypeLibrary.System.Int32)],
							},
							constructorWriter => constructorWriter.Line("Index = index;")
						);

					bodyWriter
						.XmlSummary(
							"Initializes a new instance marking the type argument with the specified type parameter name."
						)
						.Constructor(
							new(
								GeneratorTypeLibrary.Attirbutes.GenericTypeArgumentAttribute,
								TypeDeclarationAccessibility.Public
							)
							{
								Parameters = [new("name", PurviewTypeLibrary.System.String)],
							},
							constructorWriter =>
								constructorWriter.Line(
									"Name = name ?? throw new global::System.ArgumentNullException(nameof(name));"
								)
						);

					bodyWriter
						.XmlSummary("Gets or sets the type parameter name.")
						.Property(
							new(
								"Name",
								PurviewTypeLibrary.System.String.MakeNullable(),
								TypeDeclarationAccessibility.Public
							)
							{
								IsInitOnly = true,
							}
						);

					bodyWriter
						.XmlSummary("Gets or sets the type argument index.")
						.Property(
							new("Index", PurviewTypeLibrary.System.Int32, TypeDeclarationAccessibility.Public)
							{
								IsInitOnly = true,
								Initializer = "-1",
							}
						);
				}
			);
	}
}
