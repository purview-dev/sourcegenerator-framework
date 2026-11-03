using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Purview.SourceGeneratorFramework;

/// <summary>
/// Provides diagnostic descriptors shared between the source generator and the analyzer for the
/// attribute-data model feature.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
	"MicrosoftCodeAnalysisReleaseTracking",
	"RS2008:Enable analyzer release tracking",
	Justification = "The descriptors are shared between the source generator and the analyzer project; release tracking is maintained in the consuming analyzer project."
)]
public static class AttributeDataModelDiagnosticRules
{
	/// <summary>
	/// Diagnostic raised when the target attribute type cannot be resolved.
	/// </summary>
	public static readonly DiagnosticDescriptor TargetAttributeNotResolved = new(
		"ADM0001",
		"Target attribute type cannot be resolved",
		"Target attribute type for '{0}' cannot be resolved",
		"Target",
		DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	/// <summary>
	/// Diagnostic raised when a property type is not supported for attribute extraction.
	/// </summary>
	public static readonly DiagnosticDescriptor PropertyTypeNotSupported = new(
		"ADM0002",
		"Property type is not supported for attribute extraction",
		"Property '{0}' type '{1}' is not supported for attribute extraction",
		"Property",
		DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	/// <summary>
	/// Diagnostic raised when a specified constructor index/name does not exist on the target attribute.
	/// </summary>
	public static readonly DiagnosticDescriptor ConstructorMemberNotFound = new(
		"ADM0003",
		"Specified constructor index/name does not exist on the target attribute",
		"Constructor argument '{0}' does not exist on target attribute '{1}'",
		"Source",
		DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	/// <summary>
	/// Diagnostic raised when a nested model type is not annotated with [Generate].
	/// </summary>
	public static readonly DiagnosticDescriptor NestedModelNotGenerated = new(
		"ADM0004",
		"Nested model type is not annotated with [Generate]",
		"Nested model type '{0}' is not annotated with [Generate]",
		"NestedModel",
		DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	/// <summary>
	/// Diagnostic raised when a default value cannot be emitted for the property type.
	/// </summary>
	public static readonly DiagnosticDescriptor DefaultValueNotSupported = new(
		"ADM0005",
		"Default value cannot be emitted for the property type",
		"Default value '{0}' cannot be emitted for property type '{1}'",
		"DefaultValue",
		DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	/// <summary>
	/// Diagnostic raised when a non-nullable reference type property requires a default value.
	/// </summary>
	public static readonly DiagnosticDescriptor NonNullableReferenceTypeRequiresDefault = new(
		"ADM0006",
		"Non-nullable reference type property requires a default value",
		"Non-nullable reference type property '{0}' requires an explicit or inferred default value",
		"DefaultValue",
		DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	/// <summary>
	/// Diagnostic raised when auto-discovery requires a target attribute type.
	/// </summary>
	public static readonly DiagnosticDescriptor AutoDiscoverRequiresType = new(
		"ADM0007",
		"Auto-discovery requires a target attribute type",
		"Auto-discovery requires a target attribute type; use the Type constructor overload instead of the string overload",
		"AutoDiscovery",
		DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	/// <summary>
	/// Diagnostic raised when a type argument property type must be TypeIdentity.
	/// </summary>
	public static readonly DiagnosticDescriptor TypeArgumentPropertyTypeInvalid = new(
		"ADM0008",
		"Type argument property type must be TypeIdentity",
		"Type argument property '{0}' type '{1}' must be Purview.SourceGeneratorFramework.TypeIdentity",
		"TypeArgument",
		DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	/// <summary>
	/// Diagnostic raised when an IsEnum property must be a string type.
	/// </summary>
	public static readonly DiagnosticDescriptor IsEnumRequiresStringType = new(
		"ADM0009",
		"IsEnum property must be a string type",
		"Property '{0}' is marked with IsEnum but its type '{1}' is not a string; IsEnum requires a string or string? property type",
		"Property",
		DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	/// <summary>
	/// Diagnostic raised when an attribute-data model property is declared with a non-cacheable
	/// <see cref="ISymbol"/> or <see cref="Type"/> type.
	/// </summary>
	public static readonly DiagnosticDescriptor SymbolPropertyNotCacheable = new(
		"ADM0010",
		"Attribute data model property type is not cacheable",
		"Property '{0}' type '{1}' is not cacheable for attribute data extraction. Use Purview.SourceGeneratorFramework.TypeIdentity or a string/string? type to capture type identity in a cacheable form.",
		"Property",
		DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	/// <summary>
	/// Diagnostic raised when a mapped constructor parameter or named argument matches a member of the attribute
	/// only when case is ignored. Names are looked up case-sensitively, so the value is never read.
	/// </summary>
	public static readonly DiagnosticDescriptor MappedNameCaseMismatch = new(
		"ADM0011",
		"Mapped name differs from the attribute member only by case",
		"'{0}' does not match the {1} '{2}' on '{3}'; names are matched case-sensitively, so the value is never read",
		"Naming",
		DiagnosticSeverity.Warning,
		isEnabledByDefault: true
	);

	/// <summary>
	/// Diagnostic raised when a property reads a named argument that is not a public settable property or field
	/// of the attribute, or a bare <c>[Argument]</c> implies a constructor parameter the attribute does not declare.
	/// </summary>
	public static readonly DiagnosticDescriptor MappedMemberNotFound = new(
		"ADM0012",
		"Mapped attribute member does not exist",
		"Property '{0}' reads the {1} '{2}', which '{3}' does not have, so it always takes its default value",
		"Naming",
		DiagnosticSeverity.Warning,
		isEnabledByDefault: true
	);

	/// <summary>
	/// Diagnostic raised when a nested model property reads only names the outer attribute does not declare.
	/// </summary>
	public static readonly DiagnosticDescriptor NestedModelMemberNotDeclared = new(
		"ADM0013",
		"Nested model property is not declared on the outer attribute",
		"Nested model '{0}' property '{1}' reads {2}, none of which '{3}' declares, so it always takes its default value",
		"Naming",
		DiagnosticSeverity.Info,
		isEnabledByDefault: true
	);

	/// <summary>
	/// Diagnostic raised when a model property has the name of a member the generator emits.
	/// </summary>
	public static readonly DiagnosticDescriptor PropertyNameConflictsWithGeneratedMember = new(
		"ADM0014",
		"Property name conflicts with a generated member",
		"Property '{0}' has the same name as a generated member; rename the parameter",
		"Naming",
		DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	/// <summary>
	/// Diagnostic raised when a nested model reads a constructor argument by index and that index names a
	/// different parameter on the outer attribute.
	/// </summary>
	public static readonly DiagnosticDescriptor NestedModelConstructorIndexMismatch = new(
		"ADM0015",
		"Nested model constructor index names a different parameter on the outer attribute",
		"Nested model '{0}' property '{1}' reads constructor index {2}, which is '{3}' on '{4}' but '{5}' on '{6}'; map it by name instead",
		"Naming",
		DiagnosticSeverity.Warning,
		isEnabledByDefault: true
	);

	/// <summary>
	/// The members every generated attribute-data model declares. A model property with one of these names
	/// collides with the generated code (<see cref="PropertyNameConflictsWithGeneratedMember"/>).
	/// </summary>
	public static readonly ImmutableArray<string> GeneratedMemberNames =
	[
		"Exists",
		"Empty",
		"TargetAttribute",
		"AllAttributeData",
		"FromAttributeData",
		"TryFromAttributeData",
		"FromAttributeDataUnchecked",
	];
}
