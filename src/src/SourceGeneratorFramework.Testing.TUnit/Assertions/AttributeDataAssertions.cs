using System.ComponentModel;
using Microsoft.CodeAnalysis;
using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace Purview.SourceGeneratorFramework.Testing.TUnit.Assertions;

/// <summary>
/// Contains assertion methods for the arguments of an <see cref="AttributeData"/> application: constructor
/// arguments by ordinal or parameter name, and named arguments by property or field name.
/// </summary>
/// <remarks>
/// Names are matched case-sensitively, as generated attribute-data model code matches them. Values are compared
/// with <see cref="AttributeArgument.ValueEquals(object?)"/>, so enums match by value or member name and
/// <c>typeof</c> arguments match a <see cref="Type"/>, <see cref="TypeIdentity"/> or type name. A failure lists
/// every argument of the application.
/// </remarks>
public static partial class AttributeDataAssertions
{
	/// <summary>
	/// Asserts that the constructor argument at <paramref name="ordinal"/> has the <paramref name="expected"/> value.
	/// </summary>
	/// <param name="attribute">The attribute application.</param>
	/// <param name="ordinal">The zero-based constructor parameter position.</param>
	/// <param name="expected">The expected value.</param>
	/// <returns>The argument when the assertion passes.</returns>
	[GenerateAssertion]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static AssertionResult<AttributeArgument> HasConstructorArgument(
		this AttributeData attribute,
		int ordinal,
		object? expected
	)
	{
		if (attribute is null)
			return AssertionResult.Failed($"expected {nameof(AttributeData)} is null");

		var arguments = AttributeArguments.From(attribute);
		return arguments.TryGetConstructorArgument(ordinal, out var argument)
			? Check(arguments, argument, expected, null)
			: AssertionResult.Failed($"there is no constructor argument #{ordinal} in {arguments}");
	}

	/// <summary>
	/// Asserts that the constructor argument for <paramref name="parameterName"/> has the <paramref name="expected"/>
	/// value, however it was supplied.
	/// </summary>
	/// <param name="attribute">The attribute application.</param>
	/// <param name="parameterName">The constructor parameter name.</param>
	/// <param name="expected">The expected value.</param>
	/// <returns>The argument when the assertion passes.</returns>
	[GenerateAssertion]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static AssertionResult<AttributeArgument> HasConstructorArgument(
		this AttributeData attribute,
		string parameterName,
		object? expected
	) => HasConstructorArgument(attribute, parameterName, expected, null);

	/// <summary>
	/// Asserts that the constructor argument for <paramref name="parameterName"/> has the <paramref name="expected"/>
	/// value and was supplied in the expected <paramref name="form"/>.
	/// </summary>
	/// <param name="attribute">The attribute application.</param>
	/// <param name="parameterName">The constructor parameter name.</param>
	/// <param name="expected">The expected value.</param>
	/// <param name="form">How the argument must have been supplied, or <see langword="null"/> for any form.</param>
	/// <returns>The argument when the assertion passes.</returns>
	[GenerateAssertion]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static AssertionResult<AttributeArgument> HasConstructorArgument(
		this AttributeData attribute,
		string parameterName,
		object? expected,
		AttributeArgumentForm? form
	)
	{
		if (attribute is null)
			return AssertionResult.Failed($"expected {nameof(AttributeData)} is null");

		var arguments = AttributeArguments.From(attribute);
		return arguments.TryGetConstructorArgument(parameterName, out var argument)
			? Check(arguments, argument, expected, form)
			: AssertionResult.Failed($"there is no constructor parameter '{parameterName}' in {arguments}");
	}

	/// <summary>
	/// Asserts that the constructor argument for <paramref name="parameterName"/> was not written at the application
	/// site: the parameter is optional and defaulted, or the bound constructor does not have it.
	/// </summary>
	/// <param name="attribute">The attribute application.</param>
	/// <param name="parameterName">The constructor parameter name.</param>
	/// <returns>The attribute when the assertion passes.</returns>
	[GenerateAssertion]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static AssertionResult<AttributeData> DoesNotHaveConstructorArgument(
		this AttributeData attribute,
		string parameterName
	)
	{
		if (attribute is null)
			return AssertionResult.Failed($"expected {nameof(AttributeData)} is null");

		var arguments = AttributeArguments.From(attribute);
		return arguments.TryGetConstructorArgument(parameterName, out var argument) && argument.IsSupplied
			? AssertionResult.Failed($"constructor parameter '{parameterName}' was supplied in {arguments}")
			: AssertionResult<AttributeData>.Passed(attribute);
	}

	/// <summary>
	/// Asserts that the named argument <paramref name="name"/> was supplied with the <paramref name="expected"/> value.
	/// </summary>
	/// <param name="attribute">The attribute application.</param>
	/// <param name="name">The property or field name.</param>
	/// <param name="expected">The expected value.</param>
	/// <returns>The argument when the assertion passes.</returns>
	[GenerateAssertion]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static AssertionResult<AttributeArgument> HasNamedArgument(
		this AttributeData attribute,
		string name,
		object? expected
	)
	{
		if (attribute is null)
			return AssertionResult.Failed($"expected {nameof(AttributeData)} is null");

		var arguments = AttributeArguments.From(attribute);
		return arguments.TryGetNamedArgument(name, out var argument)
			? Check(arguments, argument, expected, null)
			: AssertionResult.Failed($"there is no named argument '{name}' in {arguments}");
	}

	/// <summary>
	/// Asserts that the named argument <paramref name="name"/> was not supplied.
	/// </summary>
	/// <param name="attribute">The attribute application.</param>
	/// <param name="name">The property or field name.</param>
	/// <returns>The attribute when the assertion passes.</returns>
	[GenerateAssertion]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static AssertionResult<AttributeData> DoesNotHaveNamedArgument(this AttributeData attribute, string name)
	{
		if (attribute is null)
			return AssertionResult.Failed($"expected {nameof(AttributeData)} is null");

		var arguments = AttributeArguments.From(attribute);
		return arguments.TryGetNamedArgument(name, out _)
			? AssertionResult.Failed($"named argument '{name}' was supplied in {arguments}")
			: AssertionResult<AttributeData>.Passed(attribute);
	}

	static AssertionResult<AttributeArgument> Check(
		AttributeArguments arguments,
		AttributeArgument argument,
		object? expected,
		AttributeArgumentForm? form
	)
	{
		if (!argument.ValueEquals(expected))
			return AssertionResult.Failed($"'{argument.Name}' was {argument} in {arguments}");

		if (form is not null && argument.Form != form)
			return AssertionResult.Failed($"'{argument.Name}' was supplied as {argument.Form} in {arguments}");

		return AssertionResult<AttributeArgument>.Passed(argument);
	}
}
