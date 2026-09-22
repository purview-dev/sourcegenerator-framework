using Microsoft.CodeAnalysis;

namespace Purview.SourceGeneratorFramework;

/// <summary>
/// Provides diagnostic descriptors shared between the source generator and the analyzer for the
/// type-library feature.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
	"MicrosoftCodeAnalysisReleaseTracking",
	"RS2008:Enable analyzer release tracking",
	Justification = "The descriptors are shared between the source generator and the analyzer project; release tracking is maintained in the consuming analyzer project."
)]
public static class TypeLibraryDiagnosticRules
{
	/// <summary>
	/// Diagnostic raised when GenerateTypeLibrary is applied to a non-static class.
	/// </summary>
	public static readonly DiagnosticDescriptor SpecNotStaticClass = new(
		"TLB0001",
		"GenerateTypeLibrary can only be applied to a static class",
		"GenerateTypeLibrary can only be applied to a static class",
		"TypeLibrary",
		DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	/// <summary>
	/// Diagnostic raised when a type library member type is not TypeIdentity or TypeReference.
	/// </summary>
	public static readonly DiagnosticDescriptor MemberTypeInvalid = new(
		"TLB0002",
		"Type library member type must be TypeIdentity or TypeReference",
		"Type library member '{0}' type '{1}' must be Purview.SourceGeneratorFramework.TypeIdentity or TypeReference",
		"TypeLibrary",
		DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	/// <summary>
	/// Diagnostic raised when a type library member requires a resolvable type and namespace.
	/// </summary>
	public static readonly DiagnosticDescriptor MemberTypeNotResolved = new(
		"TLB0003",
		"Type library member requires a resolvable type and namespace",
		"Type library member '{0}' must specify a target type and a namespace, or a fully-qualified type name",
		"TypeLibrary",
		DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	/// <summary>
	/// Diagnostic raised when a type library member is declared more than once.
	/// </summary>
	public static readonly DiagnosticDescriptor DuplicateMember = new(
		"TLB0004",
		"Duplicate type library member",
		"Type library member '{0}' is declared more than once in the generated type library",
		"TypeLibrary",
		DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	/// <summary>
	/// Diagnostic raised when the generated type library class name is not a valid identifier.
	/// </summary>
	public static readonly DiagnosticDescriptor InvalidClassName = new(
		"TLB0005",
		"Generated type library class name is not a valid identifier",
		"The generated type library class name '{0}' is not a valid C# identifier",
		"TypeLibrary",
		DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	/// <summary>
	/// Diagnostic raised when the generated type library namespace is not valid.
	/// </summary>
	public static readonly DiagnosticDescriptor InvalidNamespace = new(
		"TLB0006",
		"Generated type library namespace is not valid",
		"The generated type library namespace '{0}' is not a valid namespace",
		"TypeLibrary",
		DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	/// <summary>
	/// Diagnostic raised when a type library member accessibility is invalid.
	/// </summary>
	public static readonly DiagnosticDescriptor MemberAccessibilityInvalid = new(
		"TLB0008",
		"Type library member accessibility is invalid",
		"Type library member '{0}' must be declared private (TypeIdentity marker) or internal (value member)",
		"TypeLibrary",
		DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	/// <summary>
	/// Diagnostic raised when a type library reference member requires an initializer.
	/// </summary>
	public static readonly DiagnosticDescriptor ReferenceMemberMissingInitializer = new(
		"TLB0009",
		"Type library reference member requires an initializer",
		"Type library reference member '{0}' must declare a value",
		"TypeLibrary",
		DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	/// <summary>
	/// Diagnostic raised when a type library marker member should be initialized to default.
	/// </summary>
	public static readonly DiagnosticDescriptor MarkerMissingDefaultInitializer = new(
		"TLB0010",
		"Type library marker member should be initialized to default",
		"Type library marker member '{0}' should declare '= default' so the marker is explicit",
		"TypeLibrary",
		DiagnosticSeverity.Info,
		isEnabledByDefault: true
	);

	/// <summary>
	/// Diagnostic raised when a GenerateTypeLibrary spec must be declared partial.
	/// </summary>
	public static readonly DiagnosticDescriptor SpecMustBePartial = new(
		"TLB0011",
		"GenerateTypeLibrary spec must be declared partial",
		"GenerateTypeLibrary spec '{0}' must be declared partial so the generated TypeRefMarkers member can be added",
		"TypeLibrary",
		DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	/// <summary>
	/// Diagnostic raised when a type library spec class name clashes with the generated type library class.
	/// </summary>
	public static readonly DiagnosticDescriptor SpecClassNameClashesWithGeneratedClass = new(
		"TLB0012",
		"Type library spec class name clashes with the generated type library class",
		"Type library spec class '{0}' has the same name as the generated type library class '{1}'; rename the spec class so the generated partial declarations do not collide",
		"TypeLibrary",
		DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	/// <summary>
	/// Diagnostic raised when a type library spec class name matches the generated type library class.
	/// </summary>
	public static readonly DiagnosticDescriptor SpecClassNameCollidesAcrossNamespaces = new(
		"TLB0013",
		"Type library spec class name matches the generated type library class",
		"Type library spec class '{0}' matches the generated type library class '{1}'; rename the spec class so the generated and spec types are clearly distinct",
		"TypeLibrary",
		DiagnosticSeverity.Warning,
		isEnabledByDefault: true
	);

	/// <summary>
	/// Diagnostic raised when a partial declaration of a generated type library class is in a different
	/// namespace, so it silently shadows the generated type instead of merging with it.
	/// </summary>
	public static readonly DiagnosticDescriptor GeneratedTypeLibraryPartialInDifferentNamespace = new(
		"TLB0014",
		"Type library partial extension is declared in a different namespace",
		"Type '{0}' does not extend the generated type library '{1}' declared in {2}; declare the partial in the same namespace (the generated type library is in the global namespace by default) or set the Namespace property on [GenerateTypeLibrary] to match",
		"TypeLibrary",
		DiagnosticSeverity.Warning,
		isEnabledByDefault: true
	);

	/// <summary>
	/// Diagnostic raised when a partial declaration of a generated type library class does not match the
	/// generated modifiers, so the declarations cannot merge.
	/// </summary>
	public static readonly DiagnosticDescriptor GeneratedTypeLibraryPartialModifierMismatch = new(
		"TLB0015",
		"Type library partial extension must be declared 'public static partial'",
		"Type '{0}' must be declared 'public static partial' to merge with the generated type library '{1}'",
		"TypeLibrary",
		DiagnosticSeverity.Info,
		isEnabledByDefault: true
	);

	/// <summary>
	/// Diagnostic raised when an enum value member type is not TypeIdentity or EnumValueDefinition.
	/// </summary>
	public static readonly DiagnosticDescriptor EnumValueMemberTypeInvalid = new(
		"TLB0016",
		"Enum value member type must be TypeIdentity or EnumValueDefinition",
		"Enum value member '{0}' type '{1}' must be Purview.SourceGeneratorFramework.TypeIdentity or EnumValueDefinition",
		"TypeLibrary",
		DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	/// <summary>
	/// Diagnostic raised when an enum value member references an enum type that is not declared by a
	/// sibling <c>[TypeRef]</c> member in the same namespace.
	/// </summary>
	public static readonly DiagnosticDescriptor EnumValueEnumTypeNotDeclared = new(
		"TLB0017",
		"Enum value member references an enum type that is not declared",
		"Enum value member '{0}' references enum type '{1}' in namespace '{2}'; declare the enum type with a [TypeRef] member in the same namespace",
		"TypeLibrary",
		DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	/// <summary>
	/// Diagnostic raised when an enum value member is declared more than once for the same enum type.
	/// </summary>
	public static readonly DiagnosticDescriptor EnumValueDuplicateMember = new(
		"TLB0018",
		"Duplicate enum value member",
		"Enum value member '{0}' is declared more than once for enum type '{1}'",
		"TypeLibrary",
		DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	/// <summary>
	/// Diagnostic raised when two enum value members share the same numeric value.
	/// </summary>
	public static readonly DiagnosticDescriptor EnumValueDuplicateValue = new(
		"TLB0019",
		"Duplicate enum value",
		"Enum value '{0}' for enum type '{1}' has a duplicate numeric value",
		"TypeLibrary",
		DiagnosticSeverity.Info,
		isEnabledByDefault: true
	);

	/// <summary>
	/// Diagnostic raised when an <c>[EnumValues]</c> member references a type that is not an enum.
	/// </summary>
	public static readonly DiagnosticDescriptor EnumValuesTypeNotEnum = new(
		"TLB0020",
		"Enum values member references a type that is not an enum",
		"Enum values member '{0}' references type '{1}' which is not an enum type",
		"TypeLibrary",
		DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);
}
