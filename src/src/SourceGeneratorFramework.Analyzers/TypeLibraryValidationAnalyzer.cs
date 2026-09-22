using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Purview.SourceGeneratorFramework.Analyzers;

/// <summary>
/// Reports type-library validation diagnostics (TLB0001-TLB0006, TLB0008-TLB0009) on the static classes
/// annotated with <c>Purview.SourceGeneratorFramework.Generators.GenerateTypeLibraryAttribute</c>. These
/// rules were moved out of the source generator so they run as standard IDE/build analyzers instead of
/// generator-reported diagnostics.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TypeLibraryValidationAnalyzer : DiagnosticAnalyzer
{
	public static DiagnosticDescriptor SpecNotStaticClass => TypeLibraryDiagnosticRules.SpecNotStaticClass;

	public static DiagnosticDescriptor MemberTypeInvalid => TypeLibraryDiagnosticRules.MemberTypeInvalid;

	public static DiagnosticDescriptor MemberTypeNotResolved => TypeLibraryDiagnosticRules.MemberTypeNotResolved;

	public static DiagnosticDescriptor DuplicateMember => TypeLibraryDiagnosticRules.DuplicateMember;

	public static DiagnosticDescriptor InvalidClassName => TypeLibraryDiagnosticRules.InvalidClassName;

	public static DiagnosticDescriptor InvalidNamespace => TypeLibraryDiagnosticRules.InvalidNamespace;

	public static DiagnosticDescriptor MemberAccessibilityInvalid =>
		TypeLibraryDiagnosticRules.MemberAccessibilityInvalid;

	public static DiagnosticDescriptor ReferenceMemberMissingInitializer =>
		TypeLibraryDiagnosticRules.ReferenceMemberMissingInitializer;

	public static DiagnosticDescriptor MarkerMissingDefaultInitializer =>
		TypeLibraryDiagnosticRules.MarkerMissingDefaultInitializer;

	public static DiagnosticDescriptor SpecMustBePartial => TypeLibraryDiagnosticRules.SpecMustBePartial;

	public static DiagnosticDescriptor SpecClassNameClashesWithGeneratedClass =>
		TypeLibraryDiagnosticRules.SpecClassNameClashesWithGeneratedClass;

	public static DiagnosticDescriptor SpecClassNameCollidesAcrossNamespaces =>
		TypeLibraryDiagnosticRules.SpecClassNameCollidesAcrossNamespaces;

	public static DiagnosticDescriptor GeneratedTypeLibraryPartialInDifferentNamespace =>
		TypeLibraryDiagnosticRules.GeneratedTypeLibraryPartialInDifferentNamespace;

	public static DiagnosticDescriptor GeneratedTypeLibraryPartialModifierMismatch =>
		TypeLibraryDiagnosticRules.GeneratedTypeLibraryPartialModifierMismatch;

	public static DiagnosticDescriptor EnumValueMemberTypeInvalid =>
		TypeLibraryDiagnosticRules.EnumValueMemberTypeInvalid;

	public static DiagnosticDescriptor EnumValueEnumTypeNotDeclared =>
		TypeLibraryDiagnosticRules.EnumValueEnumTypeNotDeclared;

	public static DiagnosticDescriptor EnumValueDuplicateMember => TypeLibraryDiagnosticRules.EnumValueDuplicateMember;

	public static DiagnosticDescriptor EnumValueDuplicateValue => TypeLibraryDiagnosticRules.EnumValueDuplicateValue;

	public static DiagnosticDescriptor EnumValuesTypeNotEnum => TypeLibraryDiagnosticRules.EnumValuesTypeNotEnum;

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
		[
			SpecNotStaticClass,
			MemberTypeInvalid,
			MemberTypeNotResolved,
			DuplicateMember,
			InvalidClassName,
			InvalidNamespace,
			MemberAccessibilityInvalid,
			ReferenceMemberMissingInitializer,
			MarkerMissingDefaultInitializer,
			SpecMustBePartial,
			SpecClassNameClashesWithGeneratedClass,
			SpecClassNameCollidesAcrossNamespaces,
			GeneratedTypeLibraryPartialInDifferentNamespace,
			GeneratedTypeLibraryPartialModifierMismatch,
			EnumValueMemberTypeInvalid,
			EnumValueEnumTypeNotDeclared,
			EnumValueDuplicateMember,
			EnumValueDuplicateValue,
			EnumValuesTypeNotEnum,
		];

	public override void Initialize(AnalysisContext context)
	{
		if (context is null)
			throw new ArgumentNullException(nameof(context));

		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();

		context.RegisterCompilationStartAction(context =>
		{
			var generateTypeLibraryAttributeType = context.Compilation.GetTypeByMetadataName(
				"Purview.SourceGeneratorFramework.Generators.GenerateTypeLibraryAttribute"
			);
			var typeRefAttributeType = context.Compilation.GetTypeByMetadataName(
				"Purview.SourceGeneratorFramework.Generators.TypeRefAttribute"
			);
			var typeIdentityType = context.Compilation.GetTypeByMetadataName(
				"Purview.SourceGeneratorFramework.TypeIdentity"
			);
			var typeReferenceType = context.Compilation.GetTypeByMetadataName(
				"Purview.SourceGeneratorFramework.TypeReference"
			);
			var enumValueAttributeType = context.Compilation.GetTypeByMetadataName(
				"Purview.SourceGeneratorFramework.Generators.EnumValueAttribute"
			);
			var enumValuesAttributeType = context.Compilation.GetTypeByMetadataName(
				"Purview.SourceGeneratorFramework.Generators.EnumValuesAttribute"
			);
			var enumValueDefinitionType = context.Compilation.GetTypeByMetadataName(
				"Purview.SourceGeneratorFramework.EnumValueDefinition"
			);

			// The generated type library shape is derived from the specs once per compilation, so the
			// per-symbol action can detect source partial declarations that fail to merge with it.
			var generatedTypeLibraries = CollectGeneratedTypeLibraries(
				context.Compilation,
				generateTypeLibraryAttributeType
			);

			context.RegisterSymbolAction(
				context =>
					AnalyzeNamedType(
						context,
						generateTypeLibraryAttributeType,
						typeRefAttributeType,
						typeIdentityType,
						typeReferenceType,
						enumValueAttributeType,
						enumValuesAttributeType,
						enumValueDefinitionType,
						generatedTypeLibraries
					),
				SymbolKind.NamedType
			);
		});
	}

	static void AnalyzeNamedType(
		SymbolAnalysisContext context,
		INamedTypeSymbol? generateTypeLibraryAttributeType,
		INamedTypeSymbol? typeRefAttributeType,
		INamedTypeSymbol? typeIdentityType,
		INamedTypeSymbol? typeReferenceType,
		INamedTypeSymbol? enumValueAttributeType,
		INamedTypeSymbol? enumValuesAttributeType,
		INamedTypeSymbol? enumValueDefinitionType,
		IReadOnlyList<GeneratedTypeLibraryInfo> generatedTypeLibraries
	)
	{
		if (context.Symbol is not INamedTypeSymbol typeSymbol)
			return;

		if (typeSymbol.TypeKind != TypeKind.Class)
			return;

		var typeLocation = typeSymbol.Locations.FirstOrDefault(static location => location.IsInSource) ?? Location.None;

		AnalyzeGeneratedTypeLibraryPartial(context, typeSymbol, typeLocation, generatedTypeLibraries);

		if (generateTypeLibraryAttributeType is null)
			return;

		var generateAttribute = GetAttribute(typeSymbol, generateTypeLibraryAttributeType);
		if (generateAttribute is null)
			return;

		if (!typeSymbol.IsStatic)
			context.ReportDiagnostic(Diagnostic.Create(SpecNotStaticClass, typeLocation));

		if (!IsPartial(typeSymbol, context.CancellationToken))
			context.ReportDiagnostic(Diagnostic.Create(SpecMustBePartial, typeLocation, typeSymbol.Name));

		var className = GetNamedArgument(generateAttribute, "ClassName", (string?)null);
		var generatedClassName = className ?? "TypeLibrary";
		if (string.Equals(typeSymbol.Name, generatedClassName, StringComparison.Ordinal))
		{
			var generatedNamespace = GetNamedArgument(generateAttribute, "Namespace", (string?)null);
			var specNamespace = typeSymbol.ContainingNamespace.IsGlobalNamespace
				? null
				: typeSymbol.ContainingNamespace.ToDisplayString();
			var sameNamespace = string.Equals(
				generatedNamespace ?? string.Empty,
				specNamespace ?? string.Empty,
				StringComparison.Ordinal
			);

			var descriptor = sameNamespace
				? SpecClassNameClashesWithGeneratedClass
				: SpecClassNameCollidesAcrossNamespaces;
			context.ReportDiagnostic(Diagnostic.Create(descriptor, typeLocation, typeSymbol.Name, generatedClassName));
		}
		if (className is not null && !SyntaxFacts.IsValidIdentifier(className))
			context.ReportDiagnostic(Diagnostic.Create(InvalidClassName, typeLocation, className));

		var outputNamespace = GetNamedArgument(generateAttribute, "Namespace", (string?)null);
		if (outputNamespace is not null && !IsValidNamespace(outputNamespace))
			context.ReportDiagnostic(Diagnostic.Create(InvalidNamespace, typeLocation, outputNamespace));

		Dictionary<string, HashSet<string>> memberNamesByPath = new(StringComparer.Ordinal);
		Dictionary<string, HashSet<string>> enumMemberNamesByGroup = new(StringComparer.Ordinal);
		Dictionary<string, HashSet<decimal>> enumValuesByGroup = new(StringComparer.Ordinal);

		// Plain [TypeRef] TypeIdentity markers declare the enum types that [EnumValue] members reference.
		HashSet<string> typeRefMarkersByPath = new(StringComparer.Ordinal);
		foreach (var field in typeSymbol.GetMembers().OfType<IFieldSymbol>())
		{
			var typeRef = GetAttribute(field, typeRefAttributeType);
			if (typeRef is null)
				continue;

			if (typeIdentityType is null || !SymbolEqualityComparer.Default.Equals(field.Type, typeIdentityType))
				continue;

			if (!TryResolveTypeRef(typeRef, field.Name, out _, out var placementNamespace))
				continue;

			typeRefMarkersByPath.Add((placementNamespace ?? string.Empty) + "\0" + field.Name);
		}

		foreach (var field in typeSymbol.GetMembers().OfType<IFieldSymbol>())
		{
			AnalyzeMember(
				context,
				field,
				typeRefAttributeType,
				typeIdentityType,
				typeReferenceType,
				enumValueAttributeType,
				enumValuesAttributeType,
				enumValueDefinitionType,
				memberNamesByPath,
				enumMemberNamesByGroup,
				enumValuesByGroup,
				typeRefMarkersByPath,
				typeLocation
			);
		}
	}

	static void AnalyzeMember(
		SymbolAnalysisContext context,
		IFieldSymbol field,
		INamedTypeSymbol? typeRefAttributeType,
		INamedTypeSymbol? typeIdentityType,
		INamedTypeSymbol? typeReferenceType,
		INamedTypeSymbol? enumValueAttributeType,
		INamedTypeSymbol? enumValuesAttributeType,
		INamedTypeSymbol? enumValueDefinitionType,
		Dictionary<string, HashSet<string>> memberNamesByPath,
		Dictionary<string, HashSet<string>> enumMemberNamesByGroup,
		Dictionary<string, HashSet<decimal>> enumValuesByGroup,
		HashSet<string> typeRefMarkersByPath,
		Location typeLocation
	)
	{
		var typeRef = GetAttribute(field, typeRefAttributeType);
		var enumValueAttributes = GetAttributes(field, enumValueAttributeType);
		var enumValuesAttribute = GetAttribute(field, enumValuesAttributeType);

		if (typeRef is null && enumValueAttributes.Count == 0 && enumValuesAttribute is null)
			return;

		// A field can declare both [TypeRef] and enum-value markers; validate the [TypeRef] when present.
		if (typeRef is not null)
		{
			AnalyzeTypeRefMember(
				context,
				field,
				typeRef,
				typeIdentityType,
				typeReferenceType,
				memberNamesByPath,
				typeLocation
			);
		}

		foreach (var enumValue in enumValueAttributes)
		{
			AnalyzeEnumValueMember(
				context,
				field,
				enumValue,
				typeRefAttributeType,
				enumValueDefinitionType,
				enumMemberNamesByGroup,
				enumValuesByGroup,
				typeRefMarkersByPath,
				typeLocation
			);
		}

		if (enumValuesAttribute is not null)
		{
			AnalyzeEnumValuesMember(
				context,
				field,
				enumValuesAttribute,
				enumValueDefinitionType,
				enumMemberNamesByGroup,
				enumValuesByGroup,
				typeRefMarkersByPath,
				typeLocation
			);
		}
	}

	/// <summary>
	/// Validates a <c>[TypeRef]</c> member: member type, accessibility, and (for markers) a resolvable
	/// type and namespace.
	/// </summary>
	static void AnalyzeTypeRefMember(
		SymbolAnalysisContext context,
		IFieldSymbol field,
		AttributeData typeRef,
		INamedTypeSymbol? typeIdentityType,
		INamedTypeSymbol? typeReferenceType,
		Dictionary<string, HashSet<string>> memberNamesByPath,
		Location typeLocation
	)
	{
		var memberLocation = field.Locations.FirstOrDefault(static location => location.IsInSource) ?? typeLocation;

		var isTypeIdentity =
			typeIdentityType is not null && SymbolEqualityComparer.Default.Equals(field.Type, typeIdentityType);
		var isTypeReference =
			typeReferenceType is not null && SymbolEqualityComparer.Default.Equals(field.Type, typeReferenceType);

		if (!isTypeIdentity && !isTypeReference)
		{
			context.ReportDiagnostic(Diagnostic.Create(MemberTypeInvalid, memberLocation, field.Name, field.Type));
			return;
		}

		string? placementNamespace;
		var hasRealInitializer = HasRealInitializer(field, context.CancellationToken);

		if (isTypeReference || (isTypeIdentity && hasRealInitializer))
		{
			// Value members (TypeReference or an initialised TypeIdentity) require a value and internal accessibility.
			if (!hasRealInitializer)
			{
				context.ReportDiagnostic(
					Diagnostic.Create(ReferenceMemberMissingInitializer, memberLocation, field.Name)
				);
				return;
			}

			if (field.DeclaredAccessibility != Accessibility.Internal)
			{
				context.ReportDiagnostic(Diagnostic.Create(MemberAccessibilityInvalid, memberLocation, field.Name));
				return;
			}

			placementNamespace = GetPlacementNamespace(typeRef);
		}
		else
		{
			// Plain TypeIdentity markers require private accessibility and a resolvable type/namespace.
			if (field.DeclaredAccessibility != Accessibility.Private)
			{
				context.ReportDiagnostic(Diagnostic.Create(MemberAccessibilityInvalid, memberLocation, field.Name));
				return;
			}

			if (HasNoInitializer(field, context.CancellationToken))
				context.ReportDiagnostic(
					Diagnostic.Create(MarkerMissingDefaultInitializer, memberLocation, field.Name)
				);

			if (!TryResolveTypeRef(typeRef, field.Name, out _, out placementNamespace))
			{
				context.ReportDiagnostic(Diagnostic.Create(MemberTypeNotResolved, memberLocation, field.Name));
				return;
			}
		}

		var pathKey = placementNamespace ?? string.Empty;
		if (!memberNamesByPath.TryGetValue(pathKey, out var names))
		{
			names = new(StringComparer.Ordinal);
			memberNamesByPath[pathKey] = names;
		}

		if (!names.Add(field.Name))
			context.ReportDiagnostic(Diagnostic.Create(DuplicateMember, memberLocation, field.Name));
	}

	/// <summary>
	/// Validates an <c>[EnumValue]</c> marker field: member type, accessibility, the referenced enum
	/// type, and duplicate detection within the enum's value group. When the field also declares a
	/// <c>[TypeRef]</c>, the enum type is inferred from it and the attribute's first argument is the
	/// enum member name.
	/// </summary>
	static void AnalyzeEnumValueMember(
		SymbolAnalysisContext context,
		IFieldSymbol field,
		AttributeData enumValue,
		INamedTypeSymbol? typeRefAttributeType,
		INamedTypeSymbol? enumValueDefinitionType,
		Dictionary<string, HashSet<string>> enumMemberNamesByGroup,
		Dictionary<string, HashSet<decimal>> enumValuesByGroup,
		HashSet<string> typeRefMarkersByPath,
		Location typeLocation
	)
	{
		var memberLocation = field.Locations.FirstOrDefault(static location => location.IsInSource) ?? typeLocation;

		var isTypeIdentity =
			field.Type.Name == "TypeIdentity"
			|| (
				enumValueDefinitionType is not null
				&& SymbolEqualityComparer.Default.Equals(field.Type, enumValueDefinitionType)
			);
		if (!isTypeIdentity)
		{
			context.ReportDiagnostic(
				Diagnostic.Create(EnumValueMemberTypeInvalid, memberLocation, field.Name, field.Type)
			);
			return;
		}

		if (field.DeclaredAccessibility != Accessibility.Private)
		{
			context.ReportDiagnostic(Diagnostic.Create(MemberAccessibilityInvalid, memberLocation, field.Name));
			return;
		}

		if (HasNoInitializer(field, context.CancellationToken))
			context.ReportDiagnostic(Diagnostic.Create(MarkerMissingDefaultInitializer, memberLocation, field.Name));

		var (declaredName, declaredNamespace, value) = ReadEnumValue(enumValue);
		if (declaredName is null)
		{
			context.ReportDiagnostic(
				Diagnostic.Create(EnumValueEnumTypeNotDeclared, memberLocation, field.Name, string.Empty, string.Empty)
			);
			return;
		}

		var typeRef = typeRefAttributeType is null ? null : GetAttribute(field, typeRefAttributeType);

		string enumName;
		string enumNamespace;
		string memberName;

		if (typeRef is not null)
		{
			// The enum type comes from the sibling [TypeRef]; the first attribute argument is the member name.
			if (!TryResolveTypeRef(typeRef, field.Name, out var typeName, out var memberNamespace) || typeName is null)
				return;

			memberName = declaredName;
			enumName = typeName;
			enumNamespace = memberNamespace ?? string.Empty;
		}
		else
		{
			memberName = field.Name;
			enumName = declaredName;
			enumNamespace = declaredNamespace ?? string.Empty;
		}

		if (!typeRefMarkersByPath.Contains(enumNamespace + "\0" + enumName))
		{
			context.ReportDiagnostic(
				Diagnostic.Create(EnumValueEnumTypeNotDeclared, memberLocation, field.Name, enumName, enumNamespace)
			);
			return;
		}

		var groupKey = enumNamespace + "\0" + enumName;

		if (!enumMemberNamesByGroup.TryGetValue(groupKey, out var memberNames))
		{
			memberNames = new(StringComparer.Ordinal);
			enumMemberNamesByGroup[groupKey] = memberNames;
		}

		if (!memberNames.Add(memberName))
		{
			context.ReportDiagnostic(Diagnostic.Create(EnumValueDuplicateMember, memberLocation, field.Name, enumName));
			return;
		}

		if (!enumValuesByGroup.TryGetValue(groupKey, out var values))
		{
			values = [];
			enumValuesByGroup[groupKey] = values;
		}

		if (!values.Add(value))
			context.ReportDiagnostic(Diagnostic.Create(EnumValueDuplicateValue, memberLocation, field.Name, enumName));
	}

	/// <summary>
	/// Validates an <c>[EnumValues]</c> marker field: member type, accessibility, the referenced enum
	/// type, and duplicate detection against every value in the enum's value group.
	/// </summary>
	static void AnalyzeEnumValuesMember(
		SymbolAnalysisContext context,
		IFieldSymbol field,
		AttributeData enumValues,
		INamedTypeSymbol? enumValueDefinitionType,
		Dictionary<string, HashSet<string>> enumMemberNamesByGroup,
		Dictionary<string, HashSet<decimal>> enumValuesByGroup,
		HashSet<string> typeRefMarkersByPath,
		Location typeLocation
	)
	{
		var memberLocation = field.Locations.FirstOrDefault(static location => location.IsInSource) ?? typeLocation;

		var isTypeIdentity =
			field.Type.Name == "TypeIdentity"
			|| (
				enumValueDefinitionType is not null
				&& SymbolEqualityComparer.Default.Equals(field.Type, enumValueDefinitionType)
			);
		if (!isTypeIdentity)
		{
			context.ReportDiagnostic(
				Diagnostic.Create(EnumValueMemberTypeInvalid, memberLocation, field.Name, field.Type)
			);
			return;
		}

		if (field.DeclaredAccessibility != Accessibility.Private)
		{
			context.ReportDiagnostic(Diagnostic.Create(MemberAccessibilityInvalid, memberLocation, field.Name));
			return;
		}

		if (HasNoInitializer(field, context.CancellationToken))
			context.ReportDiagnostic(Diagnostic.Create(MarkerMissingDefaultInitializer, memberLocation, field.Name));

		var enumType = ReadEnumValuesType(enumValues);
		if (enumType is null || enumType.TypeKind != TypeKind.Enum)
		{
			context.ReportDiagnostic(
				Diagnostic.Create(
					EnumValuesTypeNotEnum,
					memberLocation,
					field.Name,
					enumType?.ToDisplayString() ?? string.Empty
				)
			);
			return;
		}

		var enumName = enumType.Name;
		var enumNamespace = enumType.ContainingNamespace.IsGlobalNamespace
			? string.Empty
			: enumType.ContainingNamespace.ToDisplayString();

		if (!typeRefMarkersByPath.Contains(enumNamespace + "\0" + enumName))
		{
			context.ReportDiagnostic(
				Diagnostic.Create(EnumValueEnumTypeNotDeclared, memberLocation, field.Name, enumName, enumNamespace)
			);
			return;
		}

		var groupKey = enumNamespace + "\0" + enumName;
		if (!enumMemberNamesByGroup.TryGetValue(groupKey, out var memberNames))
		{
			memberNames = new(StringComparer.Ordinal);
			enumMemberNamesByGroup[groupKey] = memberNames;
		}

		if (!enumValuesByGroup.TryGetValue(groupKey, out var values))
		{
			values = [];
			enumValuesByGroup[groupKey] = values;
		}

		foreach (var enumMember in enumType.GetMembers().OfType<IFieldSymbol>())
		{
			if (!enumMember.HasConstantValue)
				continue;

			if (!memberNames.Add(enumMember.Name))
			{
				context.ReportDiagnostic(
					Diagnostic.Create(EnumValueDuplicateMember, memberLocation, field.Name, enumMember.Name, enumName)
				);
				continue;
			}

			if (!values.Add(ReadEnumValueConstant(enumMember.ConstantValue)))
			{
				context.ReportDiagnostic(
					Diagnostic.Create(EnumValueDuplicateValue, memberLocation, field.Name, enumMember.Name, enumName)
				);
			}
		}
	}

	/// <summary>
	/// Reads an <c>[EnumValues]</c> attribute, resolving the enum type from its <c>typeof(...)</c>
	/// constructor argument.
	/// </summary>
	static INamedTypeSymbol? ReadEnumValuesType(AttributeData enumValues)
	{
		if (enumValues.ConstructorArguments.Length == 0)
			return null;

		// The first constructor argument is a System.Type, which is represented as an INamedTypeSymbol in the semantic model.
		return enumValues.ConstructorArguments[0].Value as INamedTypeSymbol;
	}

	/// <summary>
	/// Reads an enum member's constant value, normalizing it to a <see cref="decimal"/> so every enum
	/// underlying type (<c>byte</c> through <c>ulong</c>) is compared exactly.
	/// </summary>
	static decimal ReadEnumValueConstant(object? constantValue) =>
		constantValue switch
		{
			byte b => b,
			sbyte sb => sb,
			short s => s,
			ushort us => us,
			int i => i,
			uint ui => ui,
			long l => l,
			ulong ul => ul,
			_ => Convert.ToDecimal(constantValue, System.Globalization.CultureInfo.InvariantCulture),
		};

	/// <summary>
	/// Reads an <c>[EnumValue]</c> attribute, supporting the explicit enum-name/namespace form and the
	/// single fully-qualified enum type name form. The value is normalized to a <see cref="decimal"/> so
	/// every enum underlying type (<c>byte</c> through <c>ulong</c>) is compared exactly.
	/// </summary>
	static (string? EnumName, string? Namespace, decimal Value) ReadEnumValue(AttributeData enumValue)
	{
		var constructor = enumValue.AttributeConstructor;
		var isFullNameForm =
			constructor is { Parameters.Length: > 0 } && constructor.Parameters[0].Name == "enumFullName";

		if (isFullNameForm)
		{
			var fullName = GetConstructorArgument(enumValue, 0, (string?)null);
			if (string.IsNullOrWhiteSpace(fullName))
				return (null, null, 0);

			var lastDot = fullName!.LastIndexOf('.');
			return (
				lastDot < 0 ? fullName : fullName.Substring(lastDot + 1),
				lastDot < 0 ? null : fullName.Substring(0, lastDot),
				ReadEnumValueConstant(enumValue, 1)
			);
		}

		return (
			GetConstructorArgument(enumValue, 0, (string?)null),
			GetConstructorArgument(enumValue, 1, (string?)null),
			ReadEnumValueConstant(enumValue, 2)
		);
	}

	/// <summary>
	/// Reads an enum value constructor argument, preserving the declared numeric type and normalizing
	/// it to a <see cref="decimal"/>.
	/// </summary>
	static decimal ReadEnumValueConstant(AttributeData attributeData, int index)
	{
		if (index < 0 || index >= attributeData.ConstructorArguments.Length)
			return 0;

		// The value is boxed as the underlying type of the enum, so we switch on the known numeric types and
		return attributeData.ConstructorArguments[index].Value switch
		{
			byte b => b,
			sbyte sb => sb,
			short s => s,
			ushort us => us,
			int i => i,
			uint ui => ui,
			long l => l,
			ulong ul => ul,
			_ => Convert.ToDecimal(
				attributeData.ConstructorArguments[index].Value,
				System.Globalization.CultureInfo.InvariantCulture
			),
		};
	}

	/// <summary>
	/// Collects the generated type library shapes derived from every <c>[GenerateTypeLibrary]</c> spec in
	/// the compilation, deduplicated by generated class name and namespace.
	/// </summary>
	static List<GeneratedTypeLibraryInfo> CollectGeneratedTypeLibraries(
		Compilation compilation,
		INamedTypeSymbol? generateTypeLibraryAttributeType
	)
	{
		if (generateTypeLibraryAttributeType is null)
			return [];

		List<GeneratedTypeLibraryInfo> libraries = [];
		HashSet<string> seen = new(StringComparer.Ordinal);

		foreach (var spec in GetNamedTypes(compilation.Assembly.GlobalNamespace))
		{
			if (spec.TypeKind != TypeKind.Class)
				continue;

			var generateAttribute = GetAttribute(spec, generateTypeLibraryAttributeType);
			if (generateAttribute is null)
				continue;

			var generatedName = GetNamedArgument(generateAttribute, "ClassName", (string?)null) ?? "TypeLibrary";
			var generatedNamespace = GetNamedArgument(generateAttribute, "Namespace", (string?)null);
			if (string.IsNullOrWhiteSpace(generatedNamespace))
				generatedNamespace = null;

			// Deduplicate so a user type that collides with several specs reporting the same shape is
			// flagged only once.
			var key = generatedNamespace is null ? generatedName : generatedName + "\0" + generatedNamespace;
			if (!seen.Add(key))
				continue;

			libraries.Add(new GeneratedTypeLibraryInfo(generatedName, generatedNamespace, spec));
		}

		return libraries;
	}

	static IEnumerable<INamedTypeSymbol> GetNamedTypes(INamespaceSymbol @namespace)
	{
		foreach (var member in @namespace.GetMembers())
		{
			switch (member)
			{
				case INamespaceSymbol nestedNamespace:
					foreach (var nestedType in GetNamedTypes(nestedNamespace))
						yield return nestedType;
					break;
				case INamedTypeSymbol type:
					yield return type;
					foreach (var nestedType in GetNamedTypes(type))
						yield return nestedType;
					break;
				default:
					break;
			}
		}
	}

	static IEnumerable<INamedTypeSymbol> GetNamedTypes(INamedTypeSymbol type)
	{
		foreach (var member in type.GetTypeMembers())
		{
			yield return member;
			foreach (var nestedType in GetNamedTypes(member))
				yield return nestedType;
		}
	}

	/// <summary>
	/// Reports <c>TLB0014</c>/<c>TLB0015</c> when a source class shares the name of a generated type
	/// library but its declaration would not merge with it: a different namespace, or modifiers that
	/// differ from <c>public static partial</c>.
	/// </summary>
	static void AnalyzeGeneratedTypeLibraryPartial(
		SymbolAnalysisContext context,
		INamedTypeSymbol typeSymbol,
		Location typeLocation,
		IReadOnlyList<GeneratedTypeLibraryInfo> generatedTypeLibraries
	)
	{
		if (generatedTypeLibraries.Count == 0)
			return;

		// Only top-level types can merge with the generated library; a nested type of the same name is
		// a distinct type.
		if (typeSymbol.ContainingType is not null)
			return;

		foreach (var library in generatedTypeLibraries)
		{
			if (library.GeneratedName != typeSymbol.Name)
				continue;

			// The spec itself is validated against the generated class by TLB0012/TLB0013.
			if (SymbolEqualityComparer.Default.Equals(library.Spec, typeSymbol))
				continue;

			var sameNamespace = library.GeneratedNamespace is null
				? typeSymbol.ContainingNamespace.IsGlobalNamespace
				: !typeSymbol.ContainingNamespace.IsGlobalNamespace
					&& string.Equals(
						typeSymbol.ContainingNamespace.ToDisplayString(),
						library.GeneratedNamespace,
						StringComparison.Ordinal
					);

			if (sameNamespace)
			{
				if (
					IsPartial(typeSymbol, context.CancellationToken)
					&& typeSymbol.IsStatic
					&& typeSymbol.DeclaredAccessibility == Accessibility.Public
				)
					continue;

				context.ReportDiagnostic(
					Diagnostic.Create(
						GeneratedTypeLibraryPartialModifierMismatch,
						typeLocation,
						typeSymbol.Name,
						library.GeneratedName
					)
				);
			}
			else if (IsPartial(typeSymbol, context.CancellationToken))
			{
				context.ReportDiagnostic(
					Diagnostic.Create(
						GeneratedTypeLibraryPartialInDifferentNamespace,
						typeLocation,
						typeSymbol.Name,
						library.GeneratedName,
						DescribeNamespace(library.GeneratedNamespace)
					)
				);
			}
		}
	}

	static string DescribeNamespace(string? @namespace) =>
		string.IsNullOrWhiteSpace(@namespace) ? "the global namespace" : $"the '{@namespace}' namespace";

	/// <summary>
	/// Resolves the placement namespace for a value member from the <c>[TypeRef]</c> namespace argument
	/// or named property.
	/// </summary>
	static string? GetPlacementNamespace(AttributeData typeRef) =>
		GetNamedArgument(typeRef, "Namespace", (string?)null)
		?? GetConstructorArgument(typeRef, 0, (string?)null)
		?? GetConstructorArgument(typeRef, 1, (string?)null);

	static bool HasRealInitializer(IFieldSymbol field, CancellationToken cancellationToken)
	{
		foreach (var reference in field.DeclaringSyntaxReferences)
		{
			if (reference.GetSyntax(cancellationToken) is VariableDeclaratorSyntax declarator)
				return !IsDefaultExpression(declarator.Initializer?.Value);
		}

		return false;
	}

	/// <summary>
	/// Determines whether the field declares no initializer at all, rather than an explicit
	/// <c>= default</c> marker.
	/// </summary>
	static bool HasNoInitializer(IFieldSymbol field, CancellationToken cancellationToken)
	{
		foreach (var reference in field.DeclaringSyntaxReferences)
		{
			if (reference.GetSyntax(cancellationToken) is VariableDeclaratorSyntax declarator)
				return declarator.Initializer is null;
		}

		return false;
	}

	/// <summary>
	/// Determines whether every declaration of the spec type carries the <c>partial</c> modifier, so
	/// the generated <c>TypeRefMarkers</c> partial can be merged into it.
	/// </summary>
	static bool IsPartial(INamedTypeSymbol symbol, CancellationToken cancellationToken)
	{
		var hasDeclarations = false;
		foreach (var reference in symbol.DeclaringSyntaxReferences)
		{
			hasDeclarations = true;
			if (reference.GetSyntax(cancellationToken) is not TypeDeclarationSyntax declaration)
				return false;

			if (!declaration.Modifiers.Any(SyntaxKind.PartialKeyword))
				return false;
		}

		return hasDeclarations;
	}

	static bool IsDefaultExpression(ExpressionSyntax? expression) =>
		expression switch
		{
			null => true,
			LiteralExpressionSyntax { RawKind: (int)SyntaxKind.DefaultLiteralExpression } => true,
			PostfixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.SuppressNullableWarningExpression } suppression =>
				IsDefaultExpression(suppression.Operand),
			DefaultExpressionSyntax => true,
			_ => false,
		};

	static bool TryResolveTypeRef(
		AttributeData typeRef,
		string fieldName,
		out string? typeName,
		out string? memberNamespace
	)
	{
		typeName = null;
		memberNamespace = null;

		var namedNamespace = GetNamedArgument(typeRef, "Namespace", (string?)null);
		var namedArity = GetNamedArgument(typeRef, "Arity", -1);

		// The namespace-only overload declares a single string parameter; when used, the type name
		// defaults to the member (field) name.
		var isNamespaceOnlyForm =
			typeRef.AttributeConstructor is { Parameters.Length: > 0 }
			&& typeRef.AttributeConstructor.Parameters[0].Type.SpecialType == SpecialType.System_String;

		if (isNamespaceOnlyForm)
		{
			typeName = fieldName;
			memberNamespace = namedNamespace ?? GetConstructorArgument(typeRef, 0, (string?)null);
			if (string.IsNullOrWhiteSpace(memberNamespace))
				return false;

			var arity = namedArity != -1 ? namedArity : GetConstructorArgument(typeRef, 1, 0);
			return arity >= 0;
		}

		if (typeRef.ConstructorArguments.Length == 0)
			return false;

#pragma warning disable format
		switch (typeRef.ConstructorArguments[0].Value)
		{
			case ITypeSymbol typeSymbol:
			{
				if (typeSymbol.ContainingNamespace.IsGlobalNamespace)
					return false;

				typeName = typeSymbol.Name;
				memberNamespace =
					namedNamespace
					?? GetConstructorArgument(typeRef, 1, (string?)null)
					?? typeSymbol.ContainingNamespace.ToDisplayString();

				break;
			}
			case string typeNameString:
			{
				typeName = typeNameString;
				memberNamespace = namedNamespace ?? GetConstructorArgument(typeRef, 1, (string?)null);

				break;
			}
			default:
				return false;
		}
#pragma warning restore format

		if (string.IsNullOrWhiteSpace(memberNamespace))
			return false;

		var explicitArity = namedArity != -1 ? namedArity : GetConstructorArgument(typeRef, 2, -1);
		return explicitArity is >= 0 or -1;
	}

	static bool IsValidNamespace(string @namespace)
	{
		if (string.IsNullOrWhiteSpace(@namespace))
			return false;

		// A valid namespace is a series of valid identifiers separated by dots. Empty segments are not allowed.
		return @namespace.Split('.').All(segment => segment.Length > 0 && SyntaxFacts.IsValidIdentifier(segment));
	}

	static AttributeData? GetAttribute(ISymbol symbol, INamedTypeSymbol? attributeType)
	{
		if (attributeType is null)
			return null;

		foreach (var attribute in symbol.GetAttributes())
		{
			if (
				attribute.AttributeClass is not null
				&& SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeType)
			)
				return attribute;
		}

		return null;
	}

	static List<AttributeData> GetAttributes(ISymbol symbol, INamedTypeSymbol? attributeType)
	{
		List<AttributeData> result = [];
		if (attributeType is null)
			return result;

		foreach (var attribute in symbol.GetAttributes())
		{
			if (
				attribute.AttributeClass is not null
				&& SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeType)
			)
				result.Add(attribute);
		}

		return result;
	}

	static T? GetConstructorArgument<T>(AttributeData attributeData, int index, T? defaultValue)
	{
		if (index < 0 || index >= attributeData.ConstructorArguments.Length)
			return defaultValue;

		var value = attributeData.ConstructorArguments[index].Value;
		if (value is T typedValue)
			return typedValue;
		if (value is null)
			return defaultValue;

		try
		{
			return (T?)Convert.ChangeType(value, typeof(T), System.Globalization.CultureInfo.InvariantCulture);
		}
		catch
		{
			return defaultValue;
		}
	}

	static T? GetNamedArgument<T>(AttributeData attributeData, string name, T? defaultValue)
	{
		foreach (var arg in attributeData.NamedArguments)
		{
			if (arg.Key != name)
				continue;

			var value = arg.Value.Value;
			if (value is T typedValue)
				return typedValue;

			if (value is null)
				return defaultValue;

			try
			{
				return (T?)Convert.ChangeType(value, typeof(T), System.Globalization.CultureInfo.InvariantCulture);
			}
			catch
			{
				return defaultValue;
			}
		}
		return defaultValue;
	}

	/// <summary>
	/// Describes the shape of the type library class a <c>[GenerateTypeLibrary]</c> spec generates, so
	/// the analyzer can detect source partial declarations that would not merge with it.
	/// </summary>
	sealed class GeneratedTypeLibraryInfo(string generatedName, string? generatedNamespace, INamedTypeSymbol spec)
	{
		public string GeneratedName { get; } = generatedName;

		public string? GeneratedNamespace { get; } = generatedNamespace;

		public INamedTypeSymbol Spec { get; } = spec;
	}
}
