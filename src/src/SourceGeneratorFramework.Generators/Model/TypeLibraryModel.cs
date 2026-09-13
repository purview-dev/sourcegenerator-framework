namespace Purview.SourceGeneratorFramework.Generators.Model;

/// <summary>
/// Describes the generated type-library class for a <c>[GenerateTypeLibrary]</c> spec.
/// </summary>
sealed record TypeLibraryModel(
	string Specifier,
	string SpecClassName,
	string? SpecNamespace,
	TypeDeclarationAccessibility? SpecAccessibility,
	string? Namespace,
	string ClassName,
	string? Documentation,
	EquatableArray<TypeLibraryNamespaceNode> Namespaces
);

/// <summary>
/// Describes a nested namespace class in the generated type library, mirroring the namespace hierarchy
/// of the declared members.
/// </summary>
sealed record TypeLibraryNamespaceNode(
	string Name,
	string NamespaceValue,
	EquatableArray<TypeLibraryMemberModel> Members,
	EquatableArray<TypeLibraryEnumGroupModel> EnumGroups = default,
	EquatableArray<TypeLibraryNamespaceNode> Children = default
);

/// <summary>
/// Describes a nested class in the generated type library holding the enum values for a single enum
/// type, in the form <c>{EnumName}Values</c>.
/// </summary>
sealed record TypeLibraryEnumGroupModel(
	string EnumName,
	string EnumNamespace,
	bool EnumGeneratesFullNameConstant,
	EquatableArray<TypeLibraryEnumValueModel> Values
);

/// <summary>
/// Describes a single enum value member generated in a <see cref="TypeLibraryEnumGroupModel"/>.
/// </summary>
sealed record TypeLibraryEnumValueModel(
	string MemberName,
	decimal Value,
	EnumUnderlyingType UnderlyingType = EnumUnderlyingType.Int32,
	EquatableArray<string> Aliases = default,
	string? Documentation = null
);

/// <summary>
/// Describes a single member declared in the type library.
/// </summary>
sealed record TypeLibraryMemberModel(
	string MemberName,
	string? TypeName,
	string? Namespace,
	int GenericArity,
	string? ReferenceInitializer,
	bool IsTypeReference,
	string? Documentation,
	bool IncludeInGetTypes = false,
	bool GenerateFullNameConstant = false
)
{
	/// <summary>
	/// Gets whether the member's value comes from a copied initializer expression rather than a simple
	/// <c>new("Name", "Namespace")</c> identity.
	/// </summary>
	public bool IsReference => ReferenceInitializer is not null;
}
