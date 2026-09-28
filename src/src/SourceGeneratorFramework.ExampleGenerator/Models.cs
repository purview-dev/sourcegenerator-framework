namespace Purview.SourceGeneratorFramework.Examples;

/// <summary>
/// Defines the lifetime of a generated service registration.
/// </summary>
public enum ServiceLifetime
{
	/// <summary>
	/// A single instance is created and reused for the lifetime of the application.
	/// </summary>
	Singleton = 0,

	/// <summary>
	/// A new instance is created once per scope.
	/// </summary>
	Scoped = 1,

	/// <summary>
	/// A new instance is created each time the service is requested.
	/// </summary>
	Transient = 2,
}

/// <summary>
/// Marks a type for service registration generation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GenerateServiceAttribute"/> class.
/// </remarks>
/// <param name="lifetime">The service lifetime.</param>
/// <param name="name">The optional service name.</param>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class GenerateServiceAttribute(ServiceLifetime lifetime = ServiceLifetime.Singleton, string? name = null)
	: Attribute
{
	/// <summary>
	/// Gets the service lifetime.
	/// </summary>
	public ServiceLifetime Lifetime { get; } = lifetime;

	/// <summary>
	/// Gets or sets the optional service name. This property can be supplied either as the constructor's
	/// <c>name</c> argument or as a named argument (<c>[GenerateService(Name = "…")]</c>); the named
	/// argument wins when both are supplied because it is assigned after the constructor runs.
	/// </summary>
	public string? Name { get; init; } = name;
}

/// <summary>
/// Attribute data model for <see cref="GenerateServiceAttribute"/>.
/// </summary>
/// <remarks>
/// <see cref="Name"/> demonstrates a property mapped from both a constructor argument and a named
/// argument: the named argument is read first so an explicitly set property is never shadowed by the
/// constructor parameter's default.
/// </remarks>
[Generate(typeof(GenerateServiceAttribute))]
public readonly partial record struct GenerateServiceAttributeData(
	[Argument(
		"lifetime",
		IsEnum = true,
		DefaultValue = "Purview.SourceGeneratorFramework.Examples.ServiceLifetime.Singleton"
	)]
		string? Lifetime,
	[Argument("name")] [Property] string? Name
);

/// <summary>
/// Describes a discovered service target.
/// </summary>
readonly record struct ServiceTarget(string TypeName, string ClassName, string Name, string LifetimeMemberName)
{
	/// <summary>
	/// An empty <see cref="ServiceTarget"/>.
	/// </summary>
	public static readonly ServiceTarget Empty;
}

/// <summary>
/// Aggregated generation inputs for the service registration generator.
/// </summary>
readonly record struct ServiceRegistrationGenerationModel(
	GenerationContext<EmptyCapabilities> Context,
	EquatableArray<ServiceTarget> Targets,
	bool EmitServiceInfo = false
);
