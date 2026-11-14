using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Purview.SourceGeneratorFramework;

/// <summary>
/// Low-level syntax helpers shared by the matching extensions, exposed as pure static functions so they can be
/// unit tested without a compilation.
/// </summary>
public static class TypeSyntaxFacts
{
	public const string AttributeSuffix = "Attribute";

	/// <summary>
	/// Peels array, pointer, nullable and <c>ref</c> composition from a type syntax.
	/// </summary>
	/// <param name="typeSyntax">The syntax to peel.</param>
	/// <param name="core">The innermost name or predefined type.</param>
	/// <param name="modifiers">
	/// The significant modifiers, outermost-first, or <see langword="null"/> when the type carries none.
	/// Nullable annotations are omitted. The list is allocated lazily because this method runs in the
	/// predicate stage against every candidate node, and the overwhelming majority of types are uncomposed.
	/// </param>
	/// <returns><see langword="false"/> for tuples, function pointers and other unrepresentable forms.</returns>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1021:Avoid out parameters")]
	public static bool TryGetCore(TypeSyntax? typeSyntax, out TypeSyntax core, out IList<TypeModifier>? modifiers)
	{
		core = null!;
		modifiers = null;

		while (true)
		{
#pragma warning disable format
			switch (typeSyntax)
			{
				case null:
					return false;

				case RefTypeSyntax refType:
					typeSyntax = refType.Type;

					continue;

				// Syntax cannot distinguish Nullable<T> from a nullable reference annotation, so nullability is
				// not treated as a significant modifier at this tier.
				case NullableTypeSyntax nullableType:
					typeSyntax = nullableType.ElementType;

					continue;

				case ArrayTypeSyntax arrayType:
				{
					// `int[][,]` parses as a single node with a rank-specifier list, outermost-first.
					modifiers ??= [with(arrayType.RankSpecifiers.Count)];

					foreach (var rankSpecifier in arrayType.RankSpecifiers)
						modifiers.Add(TypeModifier.Array(rankSpecifier.Rank));

					typeSyntax = arrayType.ElementType;

					continue;
				}

				case PointerTypeSyntax pointerType:
					modifiers ??= [];
					modifiers.Add(TypeModifier.PointerModifier);
					typeSyntax = pointerType.ElementType;

					continue;

				case TupleTypeSyntax:
				case FunctionPointerTypeSyntax:
					return false;

				default:
					core = typeSyntax;

					return true;
			}
#pragma warning restore format
		}
	}

	/// <summary>
	/// Splits a name into its rightmost simple name and its optional left-hand qualifier.
	/// </summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1021:Avoid out parameters")]
	public static void Split(NameSyntax name, out SimpleNameSyntax simpleName, out NameSyntax? qualifier)
	{
		if (name is null)
			throw new ArgumentNullException(nameof(name));

		switch (name)
		{
			case QualifiedNameSyntax qualified:
				simpleName = qualified.Right;
				qualifier = qualified.Left;

				return;

			case AliasQualifiedNameSyntax aliasQualified:
				simpleName = aliasQualified.Name;
				// `global::Foo` is rooted but contributes no segments; `Alias::Foo` is opaque to syntax.
				qualifier = null;

				return;

			case SimpleNameSyntax simple:
				simpleName = simple;
				qualifier = null;

				return;

			default:
				simpleName = SyntaxFactory.IdentifierName(name.ToString());
				qualifier = null;

				return;
		}
	}

	/// <summary>
	/// Gets the generic arity written at a name site.
	/// </summary>
	public static int GetArity(SimpleNameSyntax simpleName) =>
		simpleName is GenericNameSyntax generic ? generic.TypeArgumentList.Arguments.Count : 0;

	/// <summary>
	/// Collects the dotted segments of a qualifier, outermost first.
	/// </summary>
	/// <returns><see langword="true"/> when the qualifier is rooted with <c>global::</c>.</returns>
	public static bool CollectQualifierSegments(NameSyntax? qualifier, ImmutableArray<string>.Builder segments)
	{
		if (segments is null)
			throw new ArgumentNullException(nameof(segments));

#pragma warning disable format
		switch (qualifier)
		{
			case null:
				return false;

			case QualifiedNameSyntax qualified:
			{
				var rooted = CollectQualifierSegments(qualified.Left, segments);
				segments.Add(qualified.Right.Identifier.ValueText);

				return rooted;
			}

			case AliasQualifiedNameSyntax aliasQualified:
			{
				segments.Add(aliasQualified.Name.Identifier.ValueText);

				return aliasQualified.Alias.Identifier.IsKind(SyntaxKind.GlobalKeyword)
					|| string.Equals(aliasQualified.Alias.Identifier.ValueText, "global", StringComparison.Ordinal);
			}

			case SimpleNameSyntax simple:
				segments.Add(simple.Identifier.ValueText);

				return false;

			default:
				segments.Add(qualifier.ToString());

				return false;
		}
#pragma warning restore format
	}

	/// <summary>
	/// Maps a predefined-type keyword to its special type.
	/// </summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0072:Add missing cases")]
	public static SpecialType GetSpecialType(SyntaxKind keyword) =>
		keyword switch
		{
			SyntaxKind.BoolKeyword => SpecialType.System_Boolean,
			SyntaxKind.ByteKeyword => SpecialType.System_Byte,
			SyntaxKind.SByteKeyword => SpecialType.System_SByte,
			SyntaxKind.CharKeyword => SpecialType.System_Char,
			SyntaxKind.DecimalKeyword => SpecialType.System_Decimal,
			SyntaxKind.DoubleKeyword => SpecialType.System_Double,
			SyntaxKind.FloatKeyword => SpecialType.System_Single,
			SyntaxKind.IntKeyword => SpecialType.System_Int32,
			SyntaxKind.UIntKeyword => SpecialType.System_UInt32,
			SyntaxKind.LongKeyword => SpecialType.System_Int64,
			SyntaxKind.ULongKeyword => SpecialType.System_UInt64,
			SyntaxKind.ShortKeyword => SpecialType.System_Int16,
			SyntaxKind.UShortKeyword => SpecialType.System_UInt16,
			SyntaxKind.ObjectKeyword => SpecialType.System_Object,
			SyntaxKind.StringKeyword => SpecialType.System_String,
			SyntaxKind.VoidKeyword => SpecialType.System_Void,
			_ => SpecialType.None,
		};

	/// <summary>
	/// Gets the declared identifier and generic arity for any type-defining declaration node.
	/// </summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1021:Avoid out parameters")]
	public static bool TryGetDeclarationName(SyntaxNode? node, out string identifier, out int arity)
	{
		switch (node)
		{
			case TypeDeclarationSyntax typeDeclaration:
				identifier = typeDeclaration.Identifier.ValueText;
				arity = typeDeclaration.TypeParameterList?.Parameters.Count ?? 0;

				return true;

			case EnumDeclarationSyntax enumDeclaration:
				identifier = enumDeclaration.Identifier.ValueText;
				arity = 0;

				return true;

			case DelegateDeclarationSyntax delegateDeclaration:
				identifier = delegateDeclaration.Identifier.ValueText;
				arity = delegateDeclaration.TypeParameterList?.Parameters.Count ?? 0;

				return true;

			default:
				identifier = null!;
				arity = 0;

				return false;
		}
	}

	/// <summary>
	/// Reconstructs the full namespace of a declaration from its ancestors, handling both block-scoped and
	/// file-scoped namespace declarations.
	/// </summary>
	/// <returns>The dotted namespace, or <see langword="null"/> for the global namespace.</returns>
	public static string? GetDeclaredNamespace(SyntaxNode node)
	{
		if (node is null)
			throw new ArgumentNullException(nameof(node));

		string? result = null;

		for (var parent = node.Parent; parent is not null; parent = parent.Parent)
		{
			if (parent is not BaseNamespaceDeclarationSyntax namespaceDeclaration)
				continue;

			var name = namespaceDeclaration.Name.ToString();
			result = result is null ? name : $"{name}.{result}";
		}

		return result;
	}

	/// <summary>
	/// Resolves the type a node refers to, preferring symbol resolution over type inference.
	/// </summary>
	public static ITypeSymbol? ResolveType(
		SyntaxNode? node,
		SemanticModel semanticModel,
		CancellationToken cancellationToken
	)
	{
		if (node is null)
			return null;

		if (semanticModel == null)
			throw new ArgumentNullException(nameof(semanticModel));

		// Prefer symbol resolution because it is more accurate than type inference, especially for generic type parameters.
		return semanticModel.GetSymbolInfo(node, cancellationToken).Symbol as ITypeSymbol
			?? semanticModel.GetTypeInfo(node, cancellationToken).Type;
	}

	/// <summary>
	/// Resolves the symbol declared by a member node, unwrapping single-variable field and event declarations
	/// to their variable declarator.
	/// </summary>
	public static ISymbol? ResolveDeclaredSymbol(
		SyntaxNode? node,
		SemanticModel semanticModel,
		CancellationToken cancellationToken
	)
	{
		if (node is null)
			return null;

		if (semanticModel == null)
			throw new ArgumentNullException(nameof(semanticModel));

		// Prefer symbol resolution because it is more accurate than type inference, especially for generic type parameters.
		return node switch
		{
			BaseFieldDeclarationSyntax field => field.Declaration.Variables.Count == 1
				? semanticModel.GetDeclaredSymbol(field.Declaration.Variables[0], cancellationToken)
				: null,
			VariableDeclaratorSyntax declarator => semanticModel.GetDeclaredSymbol(declarator, cancellationToken),
			_ => semanticModel.GetDeclaredSymbol(node, cancellationToken),
		};
	}
}
