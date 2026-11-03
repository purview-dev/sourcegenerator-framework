namespace Purview.SourceGeneratorFramework.Examples;

/// <summary>
/// Attribute data model for <see cref="LogEventAttribute"/>.
/// </summary>
/// <remarks>
/// Each value can be supplied positionally or as a named argument, so both sources are mapped. The named
/// argument is read first because it is assigned after the constructor runs.
/// </remarks>
[Generate(typeof(LogEventAttribute))]
public readonly partial record struct LogEventAttributeData(
	[Argument("level")] [Property(DefaultValue = LogLevel.Information)] LogLevel Level,
	[Argument("messageTemplate")] [Property] string? MessageTemplate,
	[Argument("eventId")] [Property(DefaultValue = -1)] int EventId,
	[Property] string? Name
);

/// <summary>
/// Attribute data model for <see cref="InfoEventAttribute"/>.
/// </summary>
/// <remarks>
/// <see cref="InfoEventAttribute"/> does not derive from <see cref="LogEventAttribute"/>, yet <c>[NestedModel]</c>
/// reads it into <see cref="LogEventAttributeData"/>'s shape: the nested model is populated from this attribute's
/// arguments through <c>LogEventAttributeData.FromAttributeDataUnchecked</c>, skipping its own target-attribute
/// check. <c>level</c> is not declared on <see cref="InfoEventAttribute"/>, so it takes the mapped default.
/// </remarks>
[Generate(typeof(InfoEventAttribute))]
public readonly partial record struct InfoEventAttributeData([NestedModel] LogEventAttributeData Log);
