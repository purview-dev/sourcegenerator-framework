using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Purview.SourceGeneratorFramework.Testing;

/// <summary>
/// The arguments of an attribute application, by constructor parameter ordinal, constructor parameter name and
/// named argument, with how each was supplied.
/// </summary>
/// <remarks>
/// <see cref="AttributeData"/> exposes constructor arguments by position only and does not say whether an
/// optional parameter was written or defaulted. This view pairs each constructor argument with its parameter name
/// and <see cref="AttributeArgumentForm"/>, which is what a test of an attribute-data model usually needs to
/// state.
/// </remarks>
public sealed class AttributeArguments
{
	AttributeArguments(AttributeData attribute, ImmutableArray<AttributeArgument> all)
	{
		Attribute = attribute;
		All = all;
		Constructor = [.. all.Where(static a => a.IsConstructorArgument)];
		Named = [.. all.Where(static a => !a.IsConstructorArgument)];
	}

	/// <summary>
	/// Gets the attribute application.
	/// </summary>
	public AttributeData Attribute { get; }

	/// <summary>
	/// Gets every argument: constructor arguments in parameter order, then named arguments in source order.
	/// </summary>
	public ImmutableArray<AttributeArgument> All { get; }

	/// <summary>
	/// Gets the constructor arguments in parameter order, including defaulted optional parameters.
	/// </summary>
	public ImmutableArray<AttributeArgument> Constructor { get; }

	/// <summary>
	/// Gets the named arguments in source order.
	/// </summary>
	public ImmutableArray<AttributeArgument> Named { get; }

	/// <summary>
	/// Creates the view for an attribute application.
	/// </summary>
	/// <param name="attribute">The attribute application.</param>
	/// <param name="cancellationToken">Cancels reading the application syntax.</param>
	/// <returns>The arguments of <paramref name="attribute"/>.</returns>
	public static AttributeArguments From(AttributeData attribute, CancellationToken cancellationToken = default)
	{
		if (attribute is null)
			throw new ArgumentNullException(nameof(attribute));

		var builder = ImmutableArray.CreateBuilder<AttributeArgument>();
		var parameters = attribute.AttributeConstructor?.Parameters ?? [];
		var syntax = attribute.ApplicationSyntaxReference?.GetSyntax(cancellationToken) as AttributeSyntax;

		var positionalCount = 0;
		HashSet<string> namedParameters = new(StringComparer.Ordinal);
		if (syntax?.ArgumentList is { } argumentList)
		{
			foreach (var argument in argumentList.Arguments)
			{
				if (argument.NameColon is { } nameColon)
					namedParameters.Add(nameColon.Name.Identifier.ValueText);
				else if (argument.NameEquals is null)
					positionalCount++;
			}
		}

		for (var i = 0; i < attribute.ConstructorArguments.Length; i++)
		{
			var name = i < parameters.Length ? parameters[i].Name : $"arg{i}";
			var form =
				syntax is null || i < positionalCount ? AttributeArgumentForm.Positional
				: namedParameters.Contains(name) ? AttributeArgumentForm.NamedParameter
				: AttributeArgumentForm.Defaulted;

			builder.Add(new(form, name, i, attribute.ConstructorArguments[i]));
		}

		foreach (var named in attribute.NamedArguments)
			builder.Add(new(AttributeArgumentForm.NamedArgument, named.Key, null, named.Value));

		return new(attribute, builder.ToImmutable());
	}

	/// <summary>
	/// Gets the constructor argument at a parameter position.
	/// </summary>
	/// <param name="ordinal">The zero-based constructor parameter position.</param>
	/// <param name="argument">The argument, when the constructor has that many parameters.</param>
	/// <returns><see langword="true"/> when the argument exists.</returns>
	public bool TryGetConstructorArgument(int ordinal, out AttributeArgument argument)
	{
		argument = Constructor.FirstOrDefault(a => a.Ordinal == ordinal)!;
		return argument is not null;
	}

	/// <summary>
	/// Gets the constructor argument for a parameter name. The name is matched case-sensitively, as the generated
	/// attribute-data model code matches it.
	/// </summary>
	/// <param name="parameterName">The constructor parameter name.</param>
	/// <param name="argument">The argument, when the bound constructor has that parameter.</param>
	/// <returns><see langword="true"/> when the argument exists.</returns>
	public bool TryGetConstructorArgument(string parameterName, out AttributeArgument argument)
	{
		argument = Constructor.FirstOrDefault(a => string.Equals(a.Name, parameterName, StringComparison.Ordinal))!;
		return argument is not null;
	}

	/// <summary>
	/// Gets the named argument for a property or field name, matched case-sensitively.
	/// </summary>
	/// <param name="name">The property or field name.</param>
	/// <param name="argument">The argument, when it was supplied.</param>
	/// <returns><see langword="true"/> when the argument was supplied.</returns>
	public bool TryGetNamedArgument(string name, out AttributeArgument argument)
	{
		argument = Named.FirstOrDefault(a => string.Equals(a.Name, name, StringComparison.Ordinal))!;
		return argument is not null;
	}

	/// <summary>
	/// Formats the application with every argument, for example
	/// <c>[InfoEvent(#0 messageTemplate: "text", EventId = 7)]</c>. Used in assertion failure messages.
	/// </summary>
	public override string ToString() => $"[{Attribute.AttributeClass?.Name}({string.Join(", ", All)})]";
}
