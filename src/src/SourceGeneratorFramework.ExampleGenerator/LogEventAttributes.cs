namespace Purview.SourceGeneratorFramework.Examples;

/// <summary>
/// Marks a method as a log event with an explicit level.
/// </summary>
/// <remarks>
/// <see cref="InfoEventAttribute"/> is a level shortcut for this attribute. It does not derive from it, but
/// declares the same constructor parameter and property names minus <c>level</c>, so its model can reuse this
/// attribute's model through <c>[NestedModel]</c>.
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class LogEventAttribute : Attribute
{
	/// <summary>
	/// Initializes a new instance of the <see cref="LogEventAttribute"/> class.
	/// </summary>
	public LogEventAttribute() { }

	/// <summary>
	/// Initializes a new instance of the <see cref="LogEventAttribute"/> class.
	/// </summary>
	/// <param name="level">The log level.</param>
	/// <param name="messageTemplate">The message template.</param>
	/// <param name="eventId">The event identifier.</param>
	public LogEventAttribute(LogLevel level, string? messageTemplate = null, int eventId = -1)
	{
		Level = level;
		MessageTemplate = messageTemplate;
		EventId = eventId;
	}

	/// <summary>
	/// Gets or sets the log level.
	/// </summary>
	public LogLevel Level { get; set; } = LogLevel.Information;

	/// <summary>
	/// Gets or sets the message template.
	/// </summary>
	public string? MessageTemplate { get; set; }

	/// <summary>
	/// Gets or sets the event identifier. <c>-1</c> means one is generated.
	/// </summary>
	public int EventId { get; set; } = -1;

	/// <summary>
	/// Gets or sets the event name.
	/// </summary>
	public string? Name { get; set; }
}

/// <summary>
/// Marks a method as an <see cref="LogLevel.Information"/> log event.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class InfoEventAttribute : Attribute
{
	/// <summary>
	/// Initializes a new instance of the <see cref="InfoEventAttribute"/> class.
	/// </summary>
	public InfoEventAttribute() { }

	/// <summary>
	/// Initializes a new instance of the <see cref="InfoEventAttribute"/> class.
	/// </summary>
	/// <param name="messageTemplate">The message template.</param>
	public InfoEventAttribute(string? messageTemplate)
	{
		MessageTemplate = messageTemplate;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="InfoEventAttribute"/> class.
	/// </summary>
	/// <param name="messageTemplate">The message template.</param>
	/// <param name="eventId">The event identifier.</param>
	public InfoEventAttribute(string? messageTemplate, int eventId)
	{
		MessageTemplate = messageTemplate;
		EventId = eventId;
	}

	/// <summary>
	/// Gets or sets the message template.
	/// </summary>
	public string? MessageTemplate { get; set; }

	/// <summary>
	/// Gets or sets the event identifier. <c>-1</c> means one is generated.
	/// </summary>
	public int EventId { get; set; } = -1;

	/// <summary>
	/// Gets or sets the event name.
	/// </summary>
	public string? Name { get; set; }
}
