namespace Purview.SourceGeneratorFramework.Examples;

/// <summary>
/// Defines the severity of a diagram element.
/// </summary>
public enum SeverityLevel
{
	/// <summary>
	/// Inherits the severity from the containing element.
	/// </summary>
	Inherit = 0,

	/// <summary>
	/// Informational severity.
	/// </summary>
	Info = 1,

	/// <summary>
	/// Warning severity.
	/// </summary>
	Warning = 2,

	/// <summary>
	/// Error severity.
	/// </summary>
	Error = 3,
}

/// <summary>
/// Marks a type with a severity level.
/// </summary>
/// <remarks>
/// Demonstrates a marker attribute whose enum-typed constructor parameter and properties feed the
/// <c>[Generate]</c> attribute-data model: the model's <c>IsEnum</c> defaults can be supplied as bare member
/// names (<c>"Inherit"</c>) and the generator expands them to the fully-qualified enum member name derived from
/// the resolved target attribute.
/// </remarks>
/// <remarks>
/// Initializes a new instance of the <see cref="SeverityAttribute"/> class.
/// </remarks>
/// <param name="severity">The severity level.</param>
[AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
public sealed class SeverityAttribute(SeverityLevel severity) : Attribute
{
	/// <summary>
	/// Gets or sets the severity level.
	/// </summary>
	public SeverityLevel Severity { get; } = severity;

	/// <summary>
	/// Gets or sets the severity level read from the named <c>Level</c> argument.
	/// </summary>
	public SeverityLevel Level { get; set; }
}
