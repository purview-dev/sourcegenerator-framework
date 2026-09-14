using Microsoft.CodeAnalysis;

namespace Purview.SourceGeneratorFramework.Helpers;

/// <summary>
/// Represents a mapping between a .NET type, its corresponding SpecialType, and its C# keyword representation.
/// </summary>
public readonly record struct TypeMapping
{
	internal TypeMapping(Type type, SpecialType specialType, string keyword)
	{
		Type = type;
		SpecialType = specialType;
		Keyword = keyword;
	}

	/// <summary>
	/// Gets the .NET type associated with this mapping.
	/// </summary>
	public Type Type { get; }

	/// <summary>
	/// Gets the SpecialType associated with this mapping.
	/// </summary>
	public SpecialType SpecialType { get; }

	/// <summary>
	/// Gets the C# keyword representation of the type associated with this mapping.
	/// </summary>
	public string Keyword { get; }

	/// <summary>
	/// Determines whether this instance is empty (i.e., has no associated type, special type, or keyword).
	/// </summary>
	public bool IsEmpty => this == Empty;

	/// <summary>
	/// Defines an implicit conversion from TypeMapping to TypeIdentity. If the SpecialType is None, it returns an empty TypeIdentity; otherwise, it creates a new TypeIdentity with the specified SpecialType.
	/// </summary>
	/// <param name="mapping">The TypeMapping instance to convert.</param>
	public static implicit operator TypeIdentity(TypeMapping mapping) =>
		mapping.SpecialType == SpecialType.None ? TypeIdentity.Empty : new(mapping.SpecialType);

	/// <summary>
	/// Represents an empty TypeMapping instance with no associated type, special type, or keyword.
	/// </summary>
	public static readonly TypeMapping Empty;
}
