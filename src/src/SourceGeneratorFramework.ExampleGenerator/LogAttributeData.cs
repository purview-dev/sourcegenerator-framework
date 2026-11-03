namespace Purview.SourceGeneratorFramework.Examples;

/// <summary>
/// Attribute data model for <see cref="LogAttribute"/>.
/// </summary>
/// <remarks>
/// <c>MatchByInheritance = true</c> makes this model accept derived attribute types too, so it can read a
/// <see cref="DebugAttribute"/> application through the same extraction logic.
/// <para>
/// <see cref="LogAttribute.Message"/> has a private setter, so it can only be supplied through the
/// <c>message</c> constructor parameter; a <c>[Property]</c> mapping would never be read (ADM0012).
/// </para>
/// </remarks>
[Generate(typeof(LogAttribute), MatchByInheritance = true)]
public readonly partial record struct LogAttributeData(
	[Argument("message")] string? Message,
	[Property] int EventId,
	[Property] string? CategoryName,
	[Property(DefaultValue = LogLevel.Information)] LogLevel Level
);

/// <summary>
/// Attribute data model for <see cref="DebugAttribute"/>.
/// </summary>
/// <remarks>
/// Reuses the <see cref="LogAttributeData"/> mapping for the inherited properties via <c>[NestedModel]</c> and
/// overrides <see cref="Level"/> to default to <see cref="LogLevel.Debug"/>. Roslyn's
/// <c>AttributeData</c> does not surface values assigned inside the attribute's constructor body, so the
/// "Debug through inheritance" default is declared here on the model.
/// </remarks>
[Generate(typeof(DebugAttribute))]
public readonly partial record struct DebugAttributeData(
	[NestedModel] LogAttributeData Log,
	[Property(DefaultValue = LogLevel.Debug)] LogLevel Level
);
