namespace Purview.SourceGeneratorFramework.ExampleGenerator;

/// <summary>
/// Demonstrates the <c>[GenerateTypeLibrary]</c> DSL. The generator emits a self-contained
/// <c>SampleTypeLibrary</c> whose nested <c>public static partial</c> classes mirror the namespaces of
/// the declared members.
/// </summary>
[GenerateTypeLibrary(ClassName = "SampleTypeLibrary", Namespace = "Purview.SourceGeneratorFramework.ExampleGenerator")]
static partial class TypeLibrarySpec
{
	/// <summary>
	/// A type emitted by the sample generator itself, declared with the namespace-only overload — the
	/// type name defaults to the member name.
	/// </summary>
	[TypeRef("Purview.SourceGeneratorFramework.Examples")]
	static readonly TypeIdentity GenerateTypeLibrarySampleAttribute = default;

	/// <summary>
	/// The <see cref="SeverityAttribute"/> marker attribute, declared with <c>GenerateFullNameConst</c> so the
	/// generated <c>SeverityAttributeFullName</c> constant can be used as the target of the
	/// <see cref="Examples.SeverityAttributeData"/> attribute-data model.
	/// </summary>
	[TypeRef("Purview.SourceGeneratorFramework.Examples", GenerateFullNameConst = true)]
	static readonly TypeIdentity SeverityAttribute = default;

	/// <summary>
	/// The <see cref="SeverityLevel"/> enum type, declared with <c>GenerateFullNameConst</c> so the generated
	/// <c>SeverityLevelFullName</c> constant is available.
	/// </summary>
	[TypeRef("Purview.SourceGeneratorFramework.Examples", GenerateFullNameConst = true)]
	static readonly TypeIdentity SeverityLevel = default;

	/// <summary>
	/// A framework type resolved through <c>typeof(...)</c>.
	/// </summary>
	[TypeRef(typeof(System.Diagnostics.Debug))]
	static readonly TypeIdentity Debug = default;

	/// <summary>
	/// Types declared explicitly, forming the <c>SampleTypeLibrary.Microsoft.Extensions.Logging</c>
	/// nested class that represents the logging namespace's classes. <c>ILogger</c> is included in the
	/// namespace's generated <c>GetTypes()</c> call.
	/// </summary>
	[TypeRef("Microsoft.Extensions.Logging", IncludeInGetTypes = true)]
	static readonly TypeIdentity ILogger = default;

	[TypeRef("Microsoft.Extensions.Logging")]
	static readonly TypeIdentity LogLevel = default;

	[TypeRef("Microsoft.Extensions.Logging")]
	static readonly TypeIdentity EventId = default;

	[TypeRef("Microsoft.Extensions.Logging")]
	static readonly TypeIdentity LoggerMessage = default;

	/// <summary>
	/// A composed <c>TypeReference</c> value member: the initializer expression becomes the generated
	/// value, exposing <c>SampleTypeLibrary.System.Collections.Generic.SampleItems</c>.
	/// </summary>
	[TypeRef("System.Collections.Generic")]
	internal static readonly TypeReference SampleItems =
		SourceGeneratorFramework.PurviewTypeLibrary.System.Collections.Generic.IEnumerable.MakeGeneric(
			SourceGeneratorFramework.PurviewTypeLibrary.System.String
		);

	/// <summary>
	/// The <c>ServiceLifetime</c> enum type, declared with <c>GenerateFullNameConst</c> so the generated
	/// <c>ServiceLifetimeFullName</c> constant and the per-value full-name constants are available. The
	/// enum values are declared inline on the same field: each <c>[EnumValue]</c> names the enum member
	/// and the enum type is inferred from the sibling <c>[TypeRef]</c>.
	/// </summary>
	[TypeRef("ServiceLifetime", "Purview.SourceGeneratorFramework.Examples", GenerateFullNameConst = true)]
	[EnumValue("Singleton", 0)]
	[EnumValue("Scoped", 1)]
	[EnumValue("Transient", 2)]
	static readonly TypeIdentity ServiceLifetime = default;
}
