using System.Collections;
using System.Globalization;
using Microsoft.CodeAnalysis;

namespace Purview.SourceGeneratorFramework.Testing;

/// <summary>
/// A single argument of an attribute application: a constructor argument or a named argument.
/// </summary>
/// <param name="Form">How the argument was supplied.</param>
/// <param name="Name">The constructor parameter name, or the property or field name for a named argument.</param>
/// <param name="Ordinal">The constructor parameter position, or <see langword="null"/> for a named argument.</param>
/// <param name="Value">The argument value as Roslyn sees it.</param>
public sealed record AttributeArgument(AttributeArgumentForm Form, string Name, int? Ordinal, TypedConstant Value)
{
	/// <summary>
	/// Gets whether this is a constructor argument, including a <see cref="AttributeArgumentForm.Defaulted"/> one.
	/// </summary>
	public bool IsConstructorArgument => Form != AttributeArgumentForm.NamedArgument;

	/// <summary>
	/// Gets whether the argument was written at the application site, rather than defaulted.
	/// </summary>
	public bool IsSupplied => Form != AttributeArgumentForm.Defaulted;

	/// <summary>
	/// Compares the argument value with <paramref name="expected"/>.
	/// </summary>
	/// <param name="expected">The expected value.</param>
	/// <returns><see langword="true"/> when the value matches.</returns>
	/// <remarks>
	/// <list type="bullet">
	/// <item>Numbers compare by value across numeric types, so <c>42</c> matches a <c>long</c> argument.</item>
	/// <item>An enum argument matches an enum value with the same underlying value, or a member name in any of the
	/// forms <c>Member</c>, <c>Enum.Member</c> or <c>Namespace.Enum.Member</c>.</item>
	/// <item>A <c>typeof</c> argument matches a <see cref="Type"/>, a <see cref="TypeIdentity"/>, or a type name in
	/// display, fully-qualified or metadata form.</item>
	/// <item>An array argument matches any non-string sequence with matching elements.</item>
	/// </list>
	/// </remarks>
	public bool ValueEquals(object? expected) => ValueEquals(Value, expected);

	/// <summary>
	/// Formats the argument as it would be written at the application site, followed by how it was supplied.
	/// </summary>
	public override string ToString()
	{
		var value = FormatValue(Value);
		return Form switch
		{
			AttributeArgumentForm.NamedArgument => $"{Name} = {value}",
			AttributeArgumentForm.Defaulted => $"#{Ordinal} {Name}: {value} (defaulted)",
			AttributeArgumentForm.NamedParameter => $"#{Ordinal} {Name}: {value} (by name)",
			_ => $"#{Ordinal} {Name}: {value}",
		};
	}

	internal static bool ValueEquals(TypedConstant value, object? expected)
	{
		if (expected is null)
			return value.IsNull;

		if (value.IsNull)
			return false;

		if (value.Kind == TypedConstantKind.Array)
		{
			if (expected is string || expected is not IEnumerable sequence)
				return false;

			var items = sequence.Cast<object?>().ToArray();
			return items.Length == value.Values.Length
				&& items.Zip(value.Values, static (item, element) => ValueEquals(element, item)).All(static e => e);
		}

		if (value.Kind == TypedConstantKind.Type)
			return value.Value is ITypeSymbol type && TypeEquals(type, expected);

		if (value.Kind == TypedConstantKind.Enum && expected is string memberName)
		{
			return EnumMemberName(value) is { } member
				&& (
					string.Equals(memberName, member, StringComparison.Ordinal)
					|| string.Equals(memberName, $"{value.Type!.Name}.{member}", StringComparison.Ordinal)
					|| string.Equals(memberName, $"{value.Type!.ToDisplayString()}.{member}", StringComparison.Ordinal)
				);
		}

		// Primitive and enum values, compared by value across numeric types.
		return Equals(value.Value, expected) || NumericEquals(value.Value, expected);
	}

	internal static string FormatValue(TypedConstant value)
	{
		if (value.IsNull)
			return "null";

		return value.Kind switch
		{
			TypedConstantKind.Array => $"[{string.Join(", ", value.Values.Select(FormatValue))}]",
			TypedConstantKind.Type => $"typeof({((ITypeSymbol)value.Value!).ToDisplayString()})",
			TypedConstantKind.Enum => EnumMemberName(value) is { } member
				? $"{value.Type!.Name}.{member}"
				: $"({value.Type!.Name}){ToInvariantString(value.Value)}",
			_ => value.Value switch
			{
				string text => $"\"{text}\"",
				char character => $"'{character}'",
				bool flag => flag ? "true" : "false",
				var other => ToInvariantString(other),
			},
		};
	}

	static string? EnumMemberName(TypedConstant value)
	{
		if (value.Type is not INamedTypeSymbol enumType)
			return null;

		foreach (var member in enumType.GetMembers())
		{
			if (member is IFieldSymbol { HasConstantValue: true } field && Equals(field.ConstantValue, value.Value))
				return field.Name;
		}

		return null;
	}

	static bool TypeEquals(ITypeSymbol type, object expected)
	{
		var metadataName = MetadataName(type);
		return expected switch
		{
			Type runtimeType => string.Equals(metadataName, runtimeType.FullName, StringComparison.Ordinal),
			TypeIdentity identity => string.Equals(metadataName, identity.MetadataFullName, StringComparison.Ordinal),
			string name => string.Equals(name, metadataName, StringComparison.Ordinal)
				|| string.Equals(name, type.ToDisplayString(), StringComparison.Ordinal)
				|| string.Equals(
					name,
					type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
					StringComparison.Ordinal
				),
			_ => false,
		};
	}

	static string MetadataName(ITypeSymbol type)
	{
		if (type.ContainingType is { } containing)
			return MetadataName(containing) + "+" + type.MetadataName;

		return type.ContainingNamespace is { IsGlobalNamespace: false } ns
			? ns.ToDisplayString() + "." + type.MetadataName
			: type.MetadataName;
	}

	static bool IsNumeric(object? value) =>
		value is byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal or Enum;

	// Floating-point values compare as double: decimal cannot hold their range.
	static bool NumericEquals(object? actual, object expected)
	{
		if (!IsNumeric(actual) || !IsNumeric(expected))
			return false;

		return actual is float or double || expected is float or double
			? Convert.ToDouble(actual, CultureInfo.InvariantCulture)
				== Convert.ToDouble(expected, CultureInfo.InvariantCulture)
			: Convert.ToDecimal(actual, CultureInfo.InvariantCulture)
				== Convert.ToDecimal(expected, CultureInfo.InvariantCulture);
	}

	static string ToInvariantString(object? value) => Convert.ToString(value, CultureInfo.InvariantCulture) ?? "null";
}
