using Microsoft.CodeAnalysis.Text;
using Purview.SourceGeneratorFramework.Generators.Helpers;

namespace Purview.SourceGeneratorFramework.Generators.Model;

partial class SourceEmitter
{
	static SourceText GenerateTypeLibraryAttribute()
	{
		var writer = CreateTypeLibraryWriter(GeneratorTypeLibrary.Attirbutes.GenerateTypeLibraryAttribute);

		return writer
			.XmlSummary("Generates a type-library class from the annotated static partial class.")
			.AttributeClass(
				new(GeneratorTypeLibrary.Attirbutes.GenerateTypeLibraryAttribute),
				AttributeTargets.Class,
				bodyWriter =>
				{
					bodyWriter
						.XmlSummary("Gets or sets the name of the generated type-library class.")
						.Property(
							new(
								"ClassName",
								PurviewTypeLibrary.System.String.MakeNullable(),
								TypeDeclarationAccessibility.Public
							)
							{
								IsInitOnly = true,
							}
						);

					bodyWriter
						.XmlSummary("Gets or sets the namespace of the generated type-library class.")
						.Property(
							new(
								"Namespace",
								PurviewTypeLibrary.System.String.MakeNullable(),
								TypeDeclarationAccessibility.Public
							)
							{
								IsInitOnly = true,
							}
						);
				}
			);
	}

	static SourceText TypeRefAttribute()
	{
		var writer = CreateTypeLibraryWriter(GeneratorTypeLibrary.Attirbutes.TypeRefAttribute);

		return writer
			.XmlSummary("Declares a type identity member for a generated type library.")
			.AttributeClass(
				new(GeneratorTypeLibrary.Attirbutes.TypeRefAttribute),
				AttributeTargets.Field,
				bodyWriter =>
				{
					bodyWriter
						.XmlSummary("Declares a type library member whose type name matches the member name.")
						.XmlParam("namespace", "The namespace of the target type.")
						.XmlParam("arity", "The generic arity of the target type, or 0 for a non-generic type.")
						.XmlParam(
							"includeInGetTypes",
							"Whether the member is included in the namespace's generated GetTypes() call."
						)
						.XmlParam(
							"generateFullNameConst",
							"Whether the member generates a full name constant; useful for compile-time usage, such as in Attribute Data Model generation."
						)
						.Constructor(
							new(GeneratorTypeLibrary.Attirbutes.TypeRefAttribute, TypeDeclarationAccessibility.Public)
							{
								Parameters =
								[
									new("@namespace", PurviewTypeLibrary.System.String),
									new("arity", PurviewTypeLibrary.System.Int32) { DefaultValue = "0" },
									new("includeInGetTypes", PurviewTypeLibrary.System.Boolean)
									{
										DefaultValue = "false",
									},
									new("generateFullNameConst", PurviewTypeLibrary.System.Boolean)
									{
										DefaultValue = "false",
									},
								],
							},
							constructorWriter =>
								constructorWriter
									.Assignment("Namespace", "@namespace")
									.Assignment("Arity", "arity")
									.Assignment("IncludeInGetTypes", "includeInGetTypes")
									.Assignment("GenerateFullNameConst", "generateFullNameConst")
						);

					bodyWriter
						.XmlSummary("Declares a type library member with an explicit target type.")
						.XmlParam("type", "The target type, supplied as a typeof(...) value or a type-name string.")
						.XmlParam("namespace", "The namespace of the target type, or null to infer it from the type.")
						.XmlParam(
							"arity",
							"The generic arity of the target type; -1 infers it from the type when possible."
						)
						.XmlParam(
							"includeInGetTypes",
							"Whether the member is included in the namespace's generated GetTypes() call."
						)
						.XmlParam(
							"generateFullNameConst",
							"Whether the member generates a full name constant; useful for compile-time usage, such as in Attribute Data Model generation."
						)
						.Constructor(
							new(GeneratorTypeLibrary.Attirbutes.TypeRefAttribute, TypeDeclarationAccessibility.Public)
							{
								Parameters =
								[
									new("type", PurviewTypeLibrary.System.Object.MakeNullable()),
									new("@namespace", PurviewTypeLibrary.System.String.MakeNullable())
									{
										DefaultValue = "null",
									},
									new("arity", PurviewTypeLibrary.System.Int32) { DefaultValue = "-1" },
									new("includeInGetTypes", PurviewTypeLibrary.System.Boolean)
									{
										DefaultValue = "false",
									},
									new("generateFullNameConst", PurviewTypeLibrary.System.Boolean)
									{
										DefaultValue = "false",
									},
								],
							},
							constructorWriter =>
								constructorWriter
									.Assignment("TargetType", "type")
									.Assignment("Namespace", "@namespace")
									.Assignment("Arity", "arity")
									.Assignment("IncludeInGetTypes", "includeInGetTypes")
									.Assignment("GenerateFullNameConst", "generateFullNameConst")
						);

					bodyWriter
						.XmlSummary("Gets the target type.")
						.Property(
							"TargetType",
							PurviewTypeLibrary.System.Object.MakeNullable(),
							TypeDeclarationAccessibility.Public
						);

					bodyWriter
						.XmlSummary("Gets or sets the namespace of the target type.")
						.Property(
							new(
								"Namespace",
								PurviewTypeLibrary.System.String.MakeNullable(),
								TypeDeclarationAccessibility.Public
							)
							{
								IsInitOnly = true,
							}
						);

					bodyWriter
						.XmlSummary(
							"Gets or sets the generic arity of the target type; -1 infers it from the type when possible."
						)
						.Property(
							new("Arity", PurviewTypeLibrary.System.Int32, TypeDeclarationAccessibility.Public)
							{
								IsInitOnly = true,
							}
						);

					bodyWriter
						.XmlSummary(
							"Gets or sets whether the member is included in the namespace's generated GetTypes() call."
						)
						.Property(
							new(
								"IncludeInGetTypes",
								PurviewTypeLibrary.System.Boolean,
								TypeDeclarationAccessibility.Public
							)
							{
								IsInitOnly = true,
							}
						);

					bodyWriter
						.XmlSummary("Gets or sets whether the member generates a full name constant.")
						.XmlRemarks(
							"This can be useful for compile-time usage, such as in Attribute Data Model generation."
						)
						.Property(
							new(
								"GenerateFullNameConst",
								PurviewTypeLibrary.System.Boolean,
								TypeDeclarationAccessibility.Public
							)
							{
								IsInitOnly = true,
							}
						);
				}
			);
	}

	static SourceText EnumValueAttribute()
	{
		var writer = CreateTypeLibraryWriter(GeneratorTypeLibrary.Attirbutes.EnumValueAttribute);

		return writer
			.XmlSummary("Declares an enum value member for a generated type library.")
			.AttributeClass(
				new(GeneratorTypeLibrary.Attirbutes.EnumValueAttribute),
				AttributeTargets.Field,
				bodyWriter =>
				{
					bodyWriter
						.XmlSummary("Declares an enum value by its enum type name and namespace.")
						.XmlParam(
							"enumName",
							"The name of the enum type; must match a [TypeRef] member name in the same namespace."
						)
						.XmlParam("namespace", "The namespace of the enum type.")
						.XmlParam(
							"value",
							"The numeric value of the enum member, typed as byte, sbyte, short, ushort, int, uint, long or ulong."
						)
						.XmlParam(
							"aliases",
							"Optional alternate names used when matching, such as for registry-style enums."
						)
						.Constructor(
							new(GeneratorTypeLibrary.Attirbutes.EnumValueAttribute, TypeDeclarationAccessibility.Public)
							{
								Parameters =
								[
									new("enumName", PurviewTypeLibrary.System.String),
									new("@namespace", PurviewTypeLibrary.System.String),
									new("value", PurviewTypeLibrary.System.Object),
									new("aliases", StringArrayReference.Nullable()) { DefaultValue = "null" },
								],
							},
							constructorWriter =>
								constructorWriter
									.Assignment("EnumName", "enumName")
									.Assignment("Namespace", "@namespace")
									.Assignment("Value", "value")
									.Assignment("Aliases", "aliases")
						);

					bodyWriter
						.XmlSummary("Declares an enum value by the enum type's fully qualified name.")
						.XmlParam(
							"enumFullName",
							"The fully qualified name of the enum type, such as 'Namespace.EnumName'."
						)
						.XmlParam(
							"value",
							"The numeric value of the enum member, typed as byte, sbyte, short, ushort, int, uint, long or ulong."
						)
						.XmlParam(
							"aliases",
							"Optional alternate names used when matching, such as for registry-style enums."
						)
						.Constructor(
							new(GeneratorTypeLibrary.Attirbutes.EnumValueAttribute, TypeDeclarationAccessibility.Public)
							{
								Parameters =
								[
									new("enumFullName", PurviewTypeLibrary.System.String),
									new("value", PurviewTypeLibrary.System.Object),
									new("aliases", StringArrayReference.Nullable()) { DefaultValue = "null" },
								],
							},
							constructorWriter =>
								constructorWriter
									.Assignment("EnumName", "enumFullName")
									.Assignment("Value", "value")
									.Assignment("Aliases", "aliases")
						);

					bodyWriter
						.XmlSummary(
							"Gets the name of the enum type, or the fully qualified type name when declared by full name."
						)
						.Property(
							new(
								"EnumName",
								PurviewTypeLibrary.System.String.MakeNullable(),
								TypeDeclarationAccessibility.Public
							)
						);

					bodyWriter
						.XmlSummary("Gets the namespace of the enum type, or null when declared by full name.")
						.Property(
							new(
								"Namespace",
								PurviewTypeLibrary.System.String.MakeNullable(),
								TypeDeclarationAccessibility.Public
							)
						);

					bodyWriter
						.XmlSummary(
							"Gets the numeric value of the enum member, typed as byte, sbyte, short, ushort, int, uint, long or ulong."
						)
						.Property(
							new(
								"Value",
								PurviewTypeLibrary.System.Object.MakeNullable(),
								TypeDeclarationAccessibility.Public
							)
						);

					bodyWriter
						.XmlSummary("Gets the optional alternate names used when matching.")
						.Property(new("Aliases", StringArrayReference.Nullable(), TypeDeclarationAccessibility.Public));
				}
			);
	}

	public static SourceText EmitTypeLibrary(TypeLibraryModel model)
	{
		CodeWriter writer = new(GenerationSettings.Create<TypeLibraryGenerator>());

		writer
			.AutoGeneratedHeader()
			.Using("global::Purview.SourceGeneratorFramework")
			.FileScopedNamespace(model.Namespace);

		WriteDocumentation(
			writer,
			model.Documentation
				?? $"Represents the {model.ClassName} type library, containing type identity/ reference information."
		);

		TypeDeclarationOptions options = new(model.ClassName, TypeDeclarationAccessibility.Public)
		{
			IsStatic = true,
			IsPartial = true,
			IncludeGeneratedAttributes = true,
		};

		using (writer.ClassScope(options))
		{
			NamespaceConst(writer, model.Namespace ?? string.Empty);

			foreach (var namespaceNode in model.Namespaces)
				NamespaceNode(writer, namespaceNode);
		}

		return writer;
	}

	static void NamespaceNode(CodeWriter writer, TypeLibraryNamespaceNode node)
	{
		TypeDeclarationOptions options = new(node.Name, TypeDeclarationAccessibility.Public)
		{
			IsStatic = true,
			IsPartial = true,
			IncludeGeneratedAttributes = true,
		};

		using (
			writer
				.XmlSummary($"Represents the {XmlCommentWriter.XmlInlineCode(node.Name)} namespace.")
				.ClassScope(options)
		)
		{
			NamespaceConst(writer, node.NamespaceValue);

			foreach (var member in node.Members)
				PublicMember(writer, member);

			foreach (var group in node.EnumGroups)
				EnumValuesGroup(writer, group);

			GetTypesMethod(writer, node);

			foreach (var child in node.Children)
				NamespaceNode(writer, child);
		}
	}

	static void GetTypesMethod(CodeWriter writer, TypeLibraryNamespaceNode node)
	{
		var included = node.Members.Where(static m => m.IncludeInGetTypes).ToList();
		if (included.Count == 0)
			return;

		writer.MethodExpression(
			new("GetTypes", ImmutableArrayOfTypeReference, TypeDeclarationAccessibility.Public)
			{
				IsStatic = true,
				ExpressionBody = $"[{string.Join(", ", included.Select(static m => m.MemberName))}]",
				IncludeGeneratedAttributes = true,
			}
		);
	}

	static void EnumValuesGroup(CodeWriter writer, TypeLibraryEnumGroupModel group)
	{
		TypeDeclarationOptions options = new($"{group.EnumName}Values", TypeDeclarationAccessibility.Public)
		{
			IsStatic = true,
			IncludeGeneratedAttributes = true,
		};

		using (
			writer
				.XmlSummary($"Represents the enum values of the {XmlCommentWriter.XmlInlineCode(group.EnumName)} enum.")
				.ClassScope(options)
		)
		{
			foreach (var value in group.Values)
				EnumValue(writer, group, value);

			GetMethod(writer, group);
		}
	}

	static void EnumValue(CodeWriter writer, TypeLibraryEnumGroupModel group, TypeLibraryEnumValueModel value)
	{
		if (group.EnumGeneratesFullNameConstant)
		{
			writer
				.XmlSummary(
					$"Represents the full name of the {XmlCommentWriter.XmlInlineCode(value.MemberName)} enum value."
				)
				.Field(
					new($"{value.MemberName}FullName", StringReference, TypeDeclarationAccessibility.Public)
					{
						IsConst = true,
						Initializer = $"{group.EnumName}FullName + \".\" + \"{value.MemberName}\"",
						IncludeGeneratedAttributes = true,
					}
				);
		}

		WriteDocumentation(
			writer,
			value.Documentation ?? $"Represents the {XmlCommentWriter.XmlInlineCode(value.MemberName)} enum value."
		);

		writer.Field(
			new(value.MemberName, EnumValueDefinitionReference, TypeDeclarationAccessibility.Public)
			{
				IsStatic = true,
				IsReadOnly = true,
				Initializer = EnumValueInitializer(group, value),
				IncludeGeneratedAttributes = true,
			}
		);
	}

	static string EnumValueInitializer(TypeLibraryEnumGroupModel group, TypeLibraryEnumValueModel value)
	{
		var hasAliases = !value.Aliases.AsImmutableArray().IsDefaultOrEmpty;
		var hasUnderlyingType = value.UnderlyingType != EnumUnderlyingType.Int32;

		var arguments = $"{group.EnumName}, \"{value.MemberName}\", {value.Value}";
		if (hasAliases)
			arguments += $", [{string.Join(", ", value.Aliases.Select(static alias => $"\"{alias}\""))}]";
		if (hasUnderlyingType)
		{
			var underlyingType = $"EnumUnderlyingType.{value.UnderlyingType}";
			arguments += hasAliases ? $", {underlyingType}" : $", underlyingType: {underlyingType}";
		}

		return $"new({arguments})";
	}

	static void GetMethod(CodeWriter writer, TypeLibraryEnumGroupModel group)
	{
		writer.Method(
			new("Get", EnumValueDefinitionReference, TypeDeclarationAccessibility.Public)
			{
				IsStatic = true,
				Parameters = [new("name", StringReference)],
				IncludeGeneratedAttributes = true,
			},
			body =>
			{
				foreach (var value in group.Values)
				{
					body.IfElse(
						$"{value.MemberName}.Matches(name)",
						valueBody => valueBody.Return(value.MemberName),
						elseBody: null
					);
				}

				body.Return("global::Purview.SourceGeneratorFramework.EnumValueDefinition.Empty");
			}
		);
	}

	static void NamespaceConst(CodeWriter writer, string namespaceValue)
	{
		var namespaceXml = string.IsNullOrWhiteSpace(namespaceValue)
			? "the global namespace."
			: $"the {XmlCommentWriter.XmlInlineCode(namespaceValue)} namespace.";

		writer
			.XmlSummary($"Represents {namespaceXml}")
			.Field(
				new("Namespace", StringReference, TypeDeclarationAccessibility.Public)
				{
					IsConst = true,
					Initializer = $"\"{namespaceValue}\"",
					IncludeGeneratedAttributes = true,
				}
			);
	}

	static void PublicMember(CodeWriter writer, TypeLibraryMemberModel member)
	{
		if (member.GenerateFullNameConstant)
		{
			writer
				.XmlSummary(
					$"Represents the full name of the {XmlCommentWriter.XmlInlineCode(member.MemberName)} member."
				)
				.Field(
					new($"{member.MemberName}FullName", StringReference, TypeDeclarationAccessibility.Public)
					{
						IsConst = true,
						Initializer = $"\"{member.Namespace}.{member.TypeName}\"",
						IncludeGeneratedAttributes = true,
					}
				);
		}

		WriteDocumentation(
			writer,
			member.Documentation ?? $"Represents the {XmlCommentWriter.XmlInlineCode(member.MemberName)} member."
		);

		writer.Field(
			new(member.MemberName, MemberTypeReference(member), TypeDeclarationAccessibility.Public)
			{
				IsStatic = true,
				IsReadOnly = true,
				Initializer = IdentityInitializer(member),
				IncludeGeneratedAttributes = true,
			}
		);
	}

	static string IdentityInitializer(TypeLibraryMemberModel member) =>
		member.ReferenceInitializer
		?? (
			member.GenericArity == 0
				? $"new(\"{member.TypeName}\", \"{member.Namespace}\")"
				: $"new(\"{member.TypeName}\", \"{member.Namespace}\", {member.GenericArity})"
		);

	static TypeReference MemberTypeReference(TypeLibraryMemberModel member) =>
		member.IsTypeReference ? TypeReferenceReference : TypeIdentityReference;

	static void WriteDocumentation(CodeWriter writer, string? documentation)
	{
		if (string.IsNullOrWhiteSpace(documentation))
			return;

		writer.XmlSummary(documentation!.Replace("\r\n", "\n").Split('\n'));
	}

	static TypeReference StringReference => PurviewTypeLibrary.System.String.AsTypeReference();

	static TypeReference StringArrayReference => PurviewTypeLibrary.System.String.MakeArray();

	static TypeReference EnumValueDefinitionReference => GeneratorTypeLibrary.EnumValueDefinition.AsTypeReference();

	static TypeReference ImmutableArrayOfTypeReference =>
		PurviewTypeLibrary
			.System.Collections.Immutable.ImmutableArray.MakeGeneric(GeneratorTypeLibrary.TypeReference)
			.AsTypeReference();

	static TypeReference TypeIdentityReference => GeneratorTypeLibrary.TypeIdentity.AsTypeReference();

	static TypeReference TypeReferenceReference => GeneratorTypeLibrary.TypeReference.AsTypeReference();

	static CodeWriter CreateTypeLibraryWriter(TypeReference type)
	{
		CodeWriter writer = new(GenerationSettings.Create<TypeLibraryGenerator>());

		return writer.AutoGeneratedHeader().FileScopedNamespace(type);
	}

	public static SourceText EmitTypeRefUsagePartial(TypeLibraryModel model)
	{
		CodeWriter writer = new(GenerationSettings.Create<TypeLibraryGenerator>());

		using (
			writer
				.AutoGeneratedHeader()
				.FileScopedNamespace(model.SpecNamespace)
				.XmlSummary("Contains the [TypeRef] marker identities declared by this spec.")
				.ClassScope(
					new TypeDeclarationOptions(model.SpecClassName, model.SpecAccessibility)
					{
						IsStatic = true,
						IsPartial = true,
						IncludeGeneratedAttributes = true,
					}
				)
		)
		{
			writer
				.XmlSummary(
					"The [TypeRef] marker identities declared by this spec.",
					"The source generator reads them at compile time; this member keeps them visible to the compiler's unused-member analysis."
				)
				.Field(
					new("TypeRefMarkers", TypeIdentityReference.MakeArray(), TypeDeclarationAccessibility.Public)
					{
						IsStatic = true,
						IsReadOnly = true,
						Initializer = $"[{string.Join(", ", GetTypeRefMarkerNames(model))}]",
						IncludeGeneratedAttributes = true,
					}
				);
		}
		return writer;
	}

	static IEnumerable<string> GetTypeRefMarkerNames(TypeLibraryModel model)
	{
		foreach (var namespaceNode in model.Namespaces)
		{
			foreach (var name in GetTypeRefMarkerNames(namespaceNode))
				yield return name;
		}
	}

	static IEnumerable<string> GetTypeRefMarkerNames(TypeLibraryNamespaceNode node)
	{
		foreach (var member in node.Members.Where(static m => !m.IsReference))
			yield return member.MemberName;

		foreach (var group in node.EnumGroups)
		{
			foreach (var value in group.Values)
				yield return value.MemberName;
		}

		foreach (var child in node.Children)
		{
			foreach (var name in GetTypeRefMarkerNames(child))
				yield return name;
		}
	}

	public static string GetTypeLibraryUsageHintName(TypeLibraryModel model) =>
		$"TypeLibrary.{model.ClassName}.{model.Specifier}.TypeRefs.g.cs";

	public static string GetTypeLibraryHintName(TypeLibraryModel model) =>
		$"TypeLibrary.{model.ClassName}.{model.Specifier}.g.cs";
}
