using System.Collections.Immutable;

namespace Purview.SourceGeneratorFramework;

/// <summary>
/// Describes a single enum value declared in a generated type library, carrying the enum type, the
/// member name, its numeric value and optional aliases used when matching names.
/// </summary>
public readonly record struct EnumValueDefinition
{
	/// <summary>
	/// Initializes an enum value definition.
	/// </summary>
	/// <param name="enumType">The enum type that declares the value.</param>
	/// <param name="name">The enum member name.</param>
	/// <param name="value">The numeric value of the member.</param>
	/// <param name="aliases">Optional alternate names used when matching, such as for registry-style enums.</param>
	/// <param name="underlyingType">
	/// The CLR underlying type of the enum, inferred from the declared value literal when available.
	/// </param>
	/// <exception cref="ArgumentException">Thrown when the enum type is empty or the name is null/whitespace.</exception>
	public EnumValueDefinition(
		TypeIdentity enumType,
		string name,
		decimal value,
		ImmutableArray<string> aliases = default,
		EnumUnderlyingType underlyingType = EnumUnderlyingType.Int32
	)
	{
		if (enumType == TypeIdentity.Empty)
			throw new ArgumentException("The enum type cannot be empty.", nameof(enumType));
		if (string.IsNullOrWhiteSpace(name))
			throw new ArgumentException("Enum value name cannot be null or whitespace.", nameof(name));

		EnumType = enumType;
		Name = name;
		Value = value;
		Aliases = aliases.IsDefault ? [] : aliases;
		UnderlyingType = underlyingType;
	}

	/// <summary>
	/// Gets the enum type that declares the value.
	/// </summary>
	public TypeIdentity EnumType { get; }

	/// <summary>
	/// Gets the enum member name.
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// Gets the numeric value of the member. Stored as a <see cref="decimal"/> so every enum underlying
	/// type (<c>byte</c> through <c>ulong</c>) is represented exactly.
	/// </summary>
	public decimal Value { get; }

	/// <summary>
	/// Gets the CLR underlying type of the enum.
	/// </summary>
	public EnumUnderlyingType UnderlyingType { get; }

	/// <summary>
	/// Gets alternate names used when matching.
	/// </summary>
	public ImmutableArray<string> Aliases { get; }

	/// <summary>
	/// Gets the fully qualified member name, in the form <c>Namespace.Enum.Member</c>.
	/// </summary>
	public string FullName => $"{EnumType.MetadataFullName}.{Name}";

	/// <summary>
	/// Determines whether the specified name refers to this enum value: the member name, the fully
	/// qualified name, a trailing <c>Enum.Member</c> form, or any alias.
	/// </summary>
	public bool Matches(string name)
	{
		if (string.IsNullOrWhiteSpace(name))
			return false;

		if (string.Equals(name, Name, StringComparison.Ordinal))
			return true;

		if (string.Equals(name, FullName, StringComparison.Ordinal))
			return true;

		if (name.EndsWith("." + Name, StringComparison.Ordinal))
			return true;

		if (!Aliases.IsDefaultOrEmpty)
		{
			foreach (var alias in Aliases)
			{
				if (string.Equals(name, alias, StringComparison.Ordinal))
					return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Gets an empty <see cref="EnumValueDefinition"/> used when no value matches.
	/// </summary>
	public static readonly EnumValueDefinition Empty;
}
