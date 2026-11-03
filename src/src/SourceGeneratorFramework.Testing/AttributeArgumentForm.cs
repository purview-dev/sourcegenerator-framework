namespace Purview.SourceGeneratorFramework.Testing;

/// <summary>
/// How an attribute argument was supplied at the application site.
/// </summary>
public enum AttributeArgumentForm
{
	/// <summary>
	/// A constructor argument supplied by position: <c>[Log("text")]</c>. Also used for every constructor argument
	/// of an attribute read from metadata, where the application syntax is not available.
	/// </summary>
	Positional,

	/// <summary>
	/// A constructor argument supplied by parameter name: <c>[Log(message: "text")]</c>.
	/// </summary>
	NamedParameter,

	/// <summary>
	/// An optional constructor parameter that was not supplied, so it carries the parameter's default value.
	/// </summary>
	Defaulted,

	/// <summary>
	/// A named argument that sets a property or field: <c>[Log(EventId = 7)]</c>.
	/// </summary>
	NamedArgument,
}
