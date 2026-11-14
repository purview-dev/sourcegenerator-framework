using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Purview.SourceGeneratorFramework.Helpers;

/// <summary>
/// Provides helpers for type analysis and identifier generation during source generation.
/// </summary>
public static class TypeHelpers
{
	/// <summary>
	/// The suffix used to identify attribute types.
	/// </summary>
	public const string AttributeSuffix = nameof(Attribute);

	/// <summary>
	/// Determines whether the specified compilation contains a type with the given identity.
	/// </summary>
	/// <param name="compilation">The compilation to check.</param>
	/// <param name="typeIdentity">The type identity to look for.</param>
	/// <returns><c>true</c> if the compilation contains the type; otherwise, <c>false</c>.</returns>
	/// <exception cref="ArgumentNullException">Thrown if the compilation or typeIdentity is null.</exception>
	public static bool HasType(Compilation compilation, TypeIdentity typeIdentity) =>
		compilation == null ? throw new ArgumentNullException(nameof(compilation))
		: typeIdentity == null ? throw new ArgumentNullException(nameof(typeIdentity))
		: compilation.GetTypeByMetadataName(typeIdentity.MetadataFullName) is not null;

	/// <summary>
	/// Determines whether the specified type is a C# keyword type.
	/// </summary>
	public static bool IsKeywordType(ITypeSymbol type) =>
		type == null
			? throw new ArgumentNullException(nameof(type))
			: KnownLangTypes.Get(type.SpecialType) != TypeMapping.Empty;

	/// <summary>
	/// Determines whether the specified keyword is a recognized C# keyword type.
	/// </summary>
	public static bool IsKeywordType(string keyword) => KnownLangTypes.Get(keyword) != TypeMapping.Empty;

	/// <summary>
	/// Determines whether the supplied type name ends with the 'Attribute' suffix.
	/// </summary>
	public static bool IsAttribute(string typeName)
	{
		if (typeName == null)
			throw new ArgumentNullException(nameof(typeName));

		var typeNameEnd = GetGenericTypeNameEnd(typeName);
		return typeNameEnd > AttributeSuffix.Length
			&& string.Compare(
				typeName,
				typeNameEnd - AttributeSuffix.Length,
				AttributeSuffix,
				0,
				AttributeSuffix.Length,
				StringComparison.Ordinal
			) == 0;
	}

	/// <summary>
	/// Determines whether the specified type symbol is compatible with any of the expected base types or interfaces.
	/// </summary>
	/// <param name="identity">The type symbol to check.</param>
	/// <param name="expectedBases">The expected base types or interfaces to check against.</param>
	/// <returns><c>true</c> if the type symbol is compatible with any of the expected base types or interfaces; otherwise, <c>false</c>.</returns>
	/// <exception cref="ArgumentNullException">Thrown if the identity or expectedBases parameters are null.</exception>
	/// <remarks>
	/// An expected open generic definition matches any construction with the same name, namespace and arity. An
	/// expected constructed generic validates its arguments; an actual argument may also satisfy an expected
	/// argument by implementing or inheriting from that expected contract.
	/// </remarks>
	public static bool Is(INamedTypeSymbol identity, params TypeIdentity[] expectedBases)
	{
		if (identity == null)
			throw new ArgumentNullException(nameof(identity));
		if (expectedBases == null)
			throw new ArgumentNullException(nameof(expectedBases));

		foreach (var expectedBase in expectedBases)
		{
			if (IsCompatibleExpectedBase(identity, expectedBase))
				return true;

			if (InheritsFrom(identity, expectedBase) || Implements(identity, expectedBase))
				return true;
		}

		return false;
	}

	/// <summary>
	/// Gets the type name without the 'Attribute' suffix, if present.
	/// </summary>
	public static string GetTypeName(string typeName)
	{
		if (typeName == null)
			throw new ArgumentNullException(nameof(typeName));

		var typeNameEnd = GetGenericTypeNameEnd(typeName);
		if (!IsAttribute(typeName))
			return typeName;

		// Remove the 'Attribute' suffix while preserving any generic arity or type arguments
		return typeName.Substring(0, typeNameEnd - AttributeSuffix.Length) + typeName.Substring(typeNameEnd);
	}

	/// <summary>
	/// Gets the exclusive end index of a type's non-generic name.
	/// </summary>
	/// <remarks>
	/// Supports both CLR metadata names such as <c>MarkerAttribute`1</c> and C# rendered names
	/// such as <c>global::Example.MarkerAttribute&lt;string&gt;</c>.
	/// </remarks>
	static int GetGenericTypeNameEnd(string typeName)
	{
		var metadataGenericStart = typeName.IndexOf('`');
		var renderedGenericStart = typeName.IndexOf('<');
		if (metadataGenericStart < 0)
			return renderedGenericStart < 0 ? typeName.Length : renderedGenericStart;
		if (renderedGenericStart < 0)
			return metadataGenericStart;

		// If both generic indicators are present, return the earliest one to get the non-generic name end.
		return Math.Min(metadataGenericStart, renderedGenericStart);
	}

	/// <summary>
	/// Determines whether the target syntax has an explicit base type declaration.
	/// </summary>
	public static bool HasExplicitBaseType(BaseTypeDeclarationSyntax syntaxNode)
	{
		if (syntaxNode == null)
			throw new ArgumentNullException(nameof(syntaxNode));

		// Check if the declaration has a base list with at least one type specified
		return syntaxNode.BaseList is { Types.Count: > 0 };
	}

	/// <summary>
	/// Determines whether the <see cref="ITypeSymbol"/> has a base type. Not that
	/// if the base type is <see cref="object"/>, it is considered to not have an explicit base type.
	/// </summary>
	public static bool HasExplicitBaseType(ITypeSymbol symbol)
	{
		if (symbol == null)
			throw new ArgumentNullException(nameof(symbol));

		// Check if the declaration has a base list with at least one type specified
		return symbol.BaseType is null or { SpecialType: not SpecialType.System_Object };
	}

	/// <summary>
	/// Determines whether the target syntax is derived from the expected base type.
	/// </summary>
	public static bool IsDerivedFromExpectedBase(BaseTypeDeclarationSyntax syntax, TypeIdentity expectedBase)
	{
		if (syntax == null)
			throw new ArgumentNullException(nameof(syntax));

		var declaredBaseTypes = syntax.BaseList?.Types;
		if (declaredBaseTypes is null)
			return false;

		foreach (var baseType in declaredBaseTypes)
		{
			if (string.Equals(GetUnqualifiedTypeName(baseType.Type), expectedBase.Name, StringComparison.Ordinal))
				return true;
		}

		return false;
	}

	/// <summary>
	/// Determines whether the target symbol is derived from the expected base type.
	/// </summary>
	/// <remarks>
	/// Open expected generic definitions match constructed base types. Constructed expected identities validate
	/// generic arguments semantically, including interface implementation and inheritance compatibility.
	/// </remarks>
	public static bool IsDerivedFromExpectedBase(ITypeSymbol symbol, TypeIdentity expectedBase)
	{
		if (symbol == null)
			throw new ArgumentNullException(nameof(symbol));

		if (symbol.BaseType is not null)
		{
			if (IsCompatibleExpectedBase(symbol.BaseType, expectedBase))
				return true;
		}

		return false;
	}

	static bool IsCompatibleExpectedBase(INamedTypeSymbol actualBase, TypeIdentity expectedBase)
	{
		if (expectedBase.Equals(actualBase))
			return true;

		var actualDefinition = actualBase.OriginalDefinition;
		var actualNamespace = actualDefinition.ContainingNamespace.IsGlobalNamespace
			? null
			: actualDefinition.ContainingNamespace.ToDisplayString();
		if (actualDefinition.Name != expectedBase.Name || actualNamespace != expectedBase.Namespace)
			return false;

		// An identity without constructed type arguments represents its type definition. Match
		// any construction with the same name and arity. A name-only identity has arity zero and
		// intentionally retains the historical name-only wildcard behavior.
		if (expectedBase.TypeArguments.IsDefaultOrEmpty)
			return expectedBase.GenericArity == 0 || actualDefinition.Arity == expectedBase.GenericArity;

		if (
			actualDefinition.Arity != expectedBase.GenericArity
			|| expectedBase.TypeArguments.IsDefaultOrEmpty
			|| actualBase.TypeArguments.Length != expectedBase.TypeArguments.Length
		)
			return false;

		// This helper validates a generator contract rather than CLR generic assignability. For
		// example, ResourceKitBase<ConcreteResource> satisfies ResourceKitBase<IResourceKit> when
		// ConcreteResource implements IResourceKit, despite constructed generic invariance.
		for (var index = 0; index < expectedBase.TypeArguments.Length; index++)
		{
			var expectedArgument = expectedBase.TypeArguments[index];
			var actualArgument = actualBase.TypeArguments[index];
			if (
				!expectedArgument.Equals(actualArgument)
				&& !Implements(actualArgument, expectedArgument)
				&& !InheritsFrom(actualArgument, expectedArgument)
			)
				return false;
		}

		return true;
	}

	/// <summary>
	/// Gets the unqualified type name from a <see cref="TypeSyntax"/>.
	/// </summary>
	public static string GetUnqualifiedTypeName(TypeSyntax typeSyntax) =>
		typeSyntax switch
		{
			IdentifierNameSyntax identifierName => identifierName.Identifier.ValueText,
			GenericNameSyntax genericName => genericName.Identifier.ValueText,
			QualifiedNameSyntax qualifiedName => GetUnqualifiedTypeName(qualifiedName.Right),
			AliasQualifiedNameSyntax aliasQualifiedName => GetUnqualifiedTypeName(aliasQualifiedName.Name),
			NullableTypeSyntax nullableType => GetUnqualifiedTypeName(nullableType.ElementType),
			_ => typeSyntax.ToString(),
		};

	/// <summary>
	/// Determines whether the supplied name is a valid C# identifier.
	/// </summary>
	public static bool IsValidIdentifier(string? name)
	{
		if (string.IsNullOrEmpty(name))
			return false;
		if (!char.IsLetter(name![0]) && name[0] != '_')
			return false;
		for (var i = 1; i < name.Length; i++)
		{
			if (!char.IsLetterOrDigit(name[i]) && name[i] != '_')
				return false;
		}

		return true;
	}

	/// <summary>
	/// Creates a <see cref="TypeIdentity"/> for the embedded compiler attribute used by source generators.
	/// </summary>
	public static readonly TypeIdentity EmbeddedAttribute = new(nameof(EmbeddedAttribute), "Microsoft.CodeAnalysis");

	/// <summary>
	/// Determines whether the type declaration is marked <see langword="partial"/>.
	/// </summary>
	public static bool IsPartial(TypeDeclarationSyntax declaration) =>
		declaration == null
			? throw new ArgumentNullException(nameof(declaration))
			: declaration.Modifiers.Any(SyntaxKind.PartialKeyword);

	/// <summary>
	/// Determines whether the type declaration has non-empty constructors.
	/// </summary>
	public static bool HasNonEmptyConstructors(TypeDeclarationSyntax declaration, string className)
	{
		if (declaration == null)
			throw new ArgumentNullException(nameof(declaration));

		if (declaration.ParameterList is { Parameters.Count: > 0 })
			return true;

		foreach (
			var constructor in declaration
				.Members.OfType<ConstructorDeclarationSyntax>()
				.Where(c => string.Equals(c.Identifier.ValueText, className, StringComparison.Ordinal))
		)
		{
			if (constructor.ParameterList.Parameters.Count > 0)
				return true;

			if (constructor.ExpressionBody is not null || constructor.Initializer is not null)
				return true;

			if (constructor.Body is not null && constructor.Body.Statements.Count > 0)
				return true;
		}

		return false;
	}

	static bool MatchesFullyQualifiedName(ISymbol symbol, string fullyQualifiedName)
	{
		if (
			string.Equals(
				symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
				$"global::{fullyQualifiedName}",
				StringComparison.Ordinal
			)
		)
			return true;

		if (symbol is INamedTypeSymbol namedType)
		{
			var original = namedType.OriginalDefinition;
			var metadataName = original.MetadataName;
			var namespaceName = original.ContainingNamespace?.ToDisplayString();
			var originalFullName = string.IsNullOrEmpty(namespaceName)
				? metadataName
				: $"{namespaceName}.{metadataName}";

			if (string.Equals(originalFullName, fullyQualifiedName, StringComparison.Ordinal))
				return true;
		}

		return false;
	}

	/// <summary>
	/// Determines whether the symbol has an attribute with the specified metadata name.
	/// </summary>
	public static ImmutableArray<AttributeData> GetAttributes(ISymbol symbol, TypeIdentity attributeType) =>
		GetAttributes(symbol, attributeType.MetadataFullName);

	/// <summary>
	/// Determines whether the symbol has an attribute with the specified metadata name.
	/// </summary>
	public static ImmutableArray<AttributeData> GetAttributes(ISymbol symbol, string fullyQualifiedName)
	{
		if (symbol == null)
			throw new ArgumentNullException(nameof(symbol));
		if (string.IsNullOrWhiteSpace(fullyQualifiedName))
		{
			throw new ArgumentException(
				"Fully qualified name cannot be null or whitespace.",
				nameof(fullyQualifiedName)
			);
		}

		// All valid...
		return
		[
			.. symbol
				.GetAttributes()
				.Where(attr =>
					attr.AttributeClass is not null
					&& MatchesFullyQualifiedName(attr.AttributeClass, fullyQualifiedName)
				),
		];
	}

	/// <summary>
	/// Determines whether the symbol has an attribute with the specified metadata name.
	/// </summary>
	public static AttributeData? GetAttribute(ISymbol symbol, TypeIdentity attributeType) =>
		GetAttribute(symbol, attributeType.MetadataFullName);

	/// <summary>
	/// Determines whether the symbol has an attribute with the specified metadata name.
	/// </summary>
	public static AttributeData? GetAttribute(ISymbol symbol, string fullyQualifiedName)
	{
		if (symbol == null)
			throw new ArgumentNullException(nameof(symbol));
		if (string.IsNullOrWhiteSpace(fullyQualifiedName))
		{
			throw new ArgumentException(
				"Fully qualified name cannot be null or whitespace.",
				nameof(fullyQualifiedName)
			);
		}

		// All valid...
		return symbol
			.GetAttributes()
			.FirstOrDefault(attr =>
				attr.AttributeClass is not null && MatchesFullyQualifiedName(attr.AttributeClass, fullyQualifiedName)
			);
	}

	/// <summary>
	/// Determines whether the symbol has the specified attribute.
	/// </summary>
	public static bool HasAttribute(ISymbol symbol, TypeIdentity attributeType) =>
		HasAttribute(symbol, attributeType.MetadataFullName);

	/// <summary>
	/// Determines whether the symbol has an attribute with the specified metadata name.
	/// </summary>
	public static bool HasAttribute(ISymbol symbol, string fullyQualifiedName)
	{
		if (symbol == null)
			throw new ArgumentNullException(nameof(symbol));
		if (string.IsNullOrWhiteSpace(fullyQualifiedName))
		{
			throw new ArgumentException(
				"Fully qualified name cannot be null or whitespace.",
				nameof(fullyQualifiedName)
			);
		}

		// All valid...
		return symbol
			.GetAttributes()
			.Any(attr =>
				attr.AttributeClass is not null && MatchesFullyQualifiedName(attr.AttributeClass, fullyQualifiedName)
			);
	}

	/// <summary>
	/// Determines whether the type inherits from the specified base type.
	/// </summary>
	public static bool InheritsFrom(ITypeSymbol typeSymbol, string fullyQualifiedName)
	{
		if (typeSymbol == null)
			throw new ArgumentNullException(nameof(typeSymbol));

		var baseType = typeSymbol.BaseType;
		while (baseType is not null)
		{
			if (MatchesFullyQualifiedName(baseType, fullyQualifiedName))
				return true;

			baseType = baseType.BaseType;
		}

		return false;
	}

	/// <summary>
	/// Determines whether the type inherits from the specified base type.
	/// </summary>
	public static bool InheritsFrom(ITypeSymbol typeSymbol, TypeIdentity baseType) =>
		InheritsFrom(typeSymbol, baseType.MetadataFullName);

	/// <summary>
	/// Determines whether the type implements the specified interface.
	/// </summary>
	public static bool Implements(ITypeSymbol typeSymbol, string fullyQualifiedName) =>
		typeSymbol == null
			? throw new ArgumentNullException(nameof(typeSymbol))
			: typeSymbol.AllInterfaces.Any(i => MatchesFullyQualifiedName(i, fullyQualifiedName));

	/// <summary>
	/// Determines whether the type implements the specified interface.
	/// </summary>
	public static bool Implements(ITypeSymbol typeSymbol, TypeIdentity interfaceType) =>
		Implements(typeSymbol, interfaceType.MetadataFullName);

	/// <summary>
	/// Returns the fully qualified display string for a type symbol, optionally including nullable annotations.
	/// </summary>
	public static string ToFullyQualifiedDisplayString(ITypeSymbol typeSymbol, bool includeNullable = true)
	{
		if (typeSymbol == null)
			throw new ArgumentNullException(nameof(typeSymbol));

		var format = includeNullable
			? SymbolDisplayFormat.FullyQualifiedFormat.WithMiscellaneousOptions(
				SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier
			)
			: SymbolDisplayFormat.FullyQualifiedFormat;

		return typeSymbol.ToDisplayString(format);
	}

	/// <summary>
	/// Returns the fully qualified display string for a symbol.
	/// </summary>
	public static string ToFullyQualifiedDisplayString(ISymbol symbol) =>
		symbol == null
			? throw new ArgumentNullException(nameof(symbol))
			: symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

	const string GenericIEnumerableName = "global::System.Collections.Generic.IEnumerable<T>";

	/// <summary>
	/// Determines whether the type is an array type.
	/// </summary>
	/// <param name="typeSymbol">The type symbol to check.</param>
	/// <returns>True if the type is an array; otherwise, false.</returns>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="typeSymbol"/> is null.</exception>
	public static bool IsArray(ITypeSymbol typeSymbol) =>
		typeSymbol == null
			? throw new ArgumentNullException(nameof(typeSymbol))
			: typeSymbol.TypeKind == TypeKind.Array;

	/// <summary>
	/// Determines whether the type is a collection-like type (implements <see cref="System.Collections.IEnumerable"/> or <see cref="IEnumerable{T}"/>).
	/// </summary>
	public static bool IsCollectionLike(ITypeSymbol typeSymbol)
	{
		if (typeSymbol == null)
			throw new ArgumentNullException(nameof(typeSymbol));

		if (typeSymbol.SpecialType == SpecialType.System_Collections_IEnumerable)
			return true;

		// Check if the type implements IEnumerable<T>
		return typeSymbol.AllInterfaces.Any(i =>
			string.Equals(
				i.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
				GenericIEnumerableName,
				StringComparison.Ordinal
			)
		);
	}

	/// <summary>
	/// Tries to get the element type of a collection-like type.
	/// </summary>
	public static bool TryGetElementType(ITypeSymbol typeSymbol, out ITypeSymbol? elementType)
	{
		if (typeSymbol == null)
			throw new ArgumentNullException(nameof(typeSymbol));

		var enumerableInterface = typeSymbol.AllInterfaces.FirstOrDefault(i =>
			string.Equals(
				i.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
				GenericIEnumerableName,
				StringComparison.Ordinal
			)
		);

		if (enumerableInterface is not null && enumerableInterface.TypeArguments.Length > 0)
		{
			elementType = enumerableInterface.TypeArguments[0];
			return true;
		}

		elementType = null;
		return false;
	}

	/// <summary>
	/// Derives a name by removing the specified suffix from the type name.
	/// </summary>
	public static string DeriveName(string typeName, string suffix)
	{
		if (string.IsNullOrEmpty(typeName))
			throw new ArgumentException("Type name cannot be null or empty.", nameof(typeName));
		if (string.IsNullOrEmpty(suffix))
			return typeName;

		var idx = typeName.IndexOf('`');
		if (idx >= 0)
			typeName = typeName.Substring(0, idx);

		if (typeName.EndsWith(suffix, StringComparison.Ordinal) && typeName.Length > suffix.Length)
			return typeName.Substring(0, typeName.Length - suffix.Length);

		// If the suffix is not present, return the original type name
		return typeName;
	}

	/// <summary>
	/// Gets the C# accessibility keyword for the specified accessibility.
	/// </summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0072:Add missing cases")]
	public static string GetAccessibilityKeyword(Accessibility accessibility) =>
		accessibility switch
		{
			Accessibility.Public => "public",
			Accessibility.Internal => "internal",
			Accessibility.Protected => "protected",
			Accessibility.Private => "private",
			Accessibility.ProtectedOrInternal => "protected internal",
			Accessibility.ProtectedAndInternal => "private protected",
			_ => string.Empty,
		};

	/// <summary>
	/// Creates the declaration options required to reopen a containing type as a partial type.
	/// </summary>
	/// <param name="containingType">The containing type symbol to describe.</param>
	/// <returns>Options that reproduce the containing type declaration.</returns>
	/// <remarks>
	/// Base types, interfaces, attributes, and primary-constructor parameters are intentionally
	/// omitted because the returned declaration reopens an existing partial type.
	/// </remarks>
	/// <exception cref="ArgumentNullException">
	/// Thrown when <paramref name="containingType"/> is <see langword="null"/>.
	/// </exception>
	/// <exception cref="ArgumentException">
	/// Thrown when the symbol is not a class, struct, record class, or record struct.
	/// </exception>
	public static TypeDeclarationOptions CreatePartialTypeDeclarationOptions(INamedTypeSymbol containingType) =>
		CreatePartialTypeDeclarationOptions(containingType, includeOptionalParts: true);

	/// <summary>
	/// Creates the declaration options required to reopen a containing type as a partial type.
	/// </summary>
	/// <param name="containingType">The containing type symbol to describe.</param>
	/// <param name="includeOptionalParts">
	/// <see langword="true"/> to reproduce optional accessibility, sealed modifiers, and generic
	/// constraints; <see langword="false"/> to emit only the declaration parts required to reopen
	/// the partial type.
	/// </param>
	/// <returns>Options that reproduce the containing partial type.</returns>
	/// <remarks>
	/// The basic form still preserves the type kind, generic parameter names and order,
	/// <c>static</c>, and <c>readonly</c>, because those affect declaration compatibility.
	/// </remarks>
	public static TypeDeclarationOptions CreatePartialTypeDeclarationOptions(
		INamedTypeSymbol containingType,
		bool includeOptionalParts
	)
	{
		if (containingType == null)
			throw new ArgumentNullException(nameof(containingType));

		var kind = GetTypeDeclarationKind(containingType);
		var isStatic = containingType.IsStatic;
		var isClosed = includeOptionalParts && IsClosedType(containingType);

		return new(containingType.Name)
		{
			Kind = kind,
			Accessibility = includeOptionalParts
				? containingType.IsFileLocal
					? TypeDeclarationAccessibility.File
					: containingType.DeclaredAccessibility.ToTypeDeclarationAccessibility()
				: null,
			IsPartial = true,
			IsStatic = isStatic,
			IsClosed = isClosed,
			IsSealed = includeOptionalParts && !isStatic && !isClosed && containingType.IsSealed,
			IsAbstract = includeOptionalParts && !isStatic && !isClosed && containingType.IsAbstract,
			IsReadOnly = containingType.IsReadOnly,
			GenericTypes =
			[
				.. containingType.TypeParameters.Select(typeParameter =>
					CreateGenericTypeParameterOptions(typeParameter, includeOptionalParts)
				),
			],
		};
	}

	static TypeDeclarationKind GetTypeDeclarationKind(INamedTypeSymbol typeSymbol) =>
		(typeSymbol.TypeKind, typeSymbol.IsRecord) switch
		{
			(TypeKind.Class, false) => TypeDeclarationKind.Class,
			(TypeKind.Class, true) => TypeDeclarationKind.RecordClass,
			(TypeKind.Struct, false) => TypeDeclarationKind.Struct,
			(TypeKind.Struct, true) => TypeDeclarationKind.RecordStruct,
			(TypeKind.Interface, _) => TypeDeclarationKind.Interface,
			_ => throw new ArgumentException(
				$"Type '{typeSymbol.ToDisplayString()}' is not a supported containing type.",
				nameof(typeSymbol)
			),
		};

	static bool IsClosedType(INamedTypeSymbol typeSymbol) =>
		typeSymbol
			.GetAttributes()
			.Any(static attribute =>
				attribute.AttributeClass?.ToDisplayString() == "System.Runtime.CompilerServices.IsClosedTypeAttribute"
			);

	static GenericTypeParameterOptions CreateGenericTypeParameterOptions(
		ITypeParameterSymbol typeParameter,
		bool includeConstraints
	)
	{
		var constraints = ImmutableArray.CreateBuilder<string>();
		if (!includeConstraints)
			return new(typeParameter.Name) { Constraints = constraints.ToImmutable() };

		if (typeParameter.HasUnmanagedTypeConstraint)
			constraints.Add("unmanaged");
		else if (typeParameter.HasValueTypeConstraint)
			constraints.Add("struct");
		else if (typeParameter.HasReferenceTypeConstraint)
			constraints.Add(
				typeParameter.ReferenceTypeConstraintNullableAnnotation == NullableAnnotation.Annotated
					? "class?"
					: "class"
			);
		else if (typeParameter.HasNotNullConstraint)
			constraints.Add("notnull");

		constraints.AddRange(
			typeParameter.ConstraintTypes.Select(static constraint =>
				constraint.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
			)
		);

		if (
			typeParameter.HasConstructorConstraint
			&& !typeParameter.HasValueTypeConstraint
			&& !typeParameter.HasUnmanagedTypeConstraint
		)
			constraints.Add("new()");

		return new(typeParameter.Name) { Constraints = constraints.ToImmutable() };
	}

	/// <summary>
	/// Determines whether the symbol is accessible from public or internal scopes.
	/// </summary>
	public static bool IsAccessibleAsPublicOrInternal(ISymbol symbol) =>
		symbol == null
			? throw new ArgumentNullException(nameof(symbol))
			: symbol.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal;
}
