using System.Globalization;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Purview.SourceGeneratorFramework.Generators.Helpers;

static class TypeLibraryModelLibrary
{
	public static IncrementalValuesProvider<GeneratorResult<TypeLibraryModel>> GetTypeLibraryTargetPipeline(
		IncrementalGeneratorInitializationContext context
	)
	{
		// The framework PurviewTypeLibrary shape is fixed for a given compilation, so it is walked once per
		// compilation and cached as a value-equatable model instead of being re-walked for every
		// [GenerateTypeLibrary] spec in the compilation. The framework's public type names travel with it
		// because copied documentation renders framework-type cref references as inline code.
		var frameworkTree = context
			.CompilationProvider.Select(static (compilation, _) => BuildFrameworkModel(compilation))
			.WithTrackingName("GetFrameworkTypeLibraryTree");

		return IncrementalPipeline
			.ForAttributeWithMetadataName(
				context,
				GeneratorTypeLibrary.Attirbutes.GenerateTypeLibraryAttribute,
				static (ctx, _) => (INamedTypeSymbol)ctx.TargetSymbol,
				predicate: static (ctx, _) => ctx is ClassDeclarationSyntax
			)
			.CombineWith(frameworkTree, static (specSymbol, framework, ct) => BuildTarget(specSymbol, framework, ct))
			.WithTrackingName("GetTypeLibraryTargets");
	}

	/// <summary>
	/// The framework information a type-library target needs: the fixed <c>PurviewTypeLibrary</c> shape
	/// and the framework assembly's public type names.
	/// </summary>
	sealed record FrameworkTypeLibraryModel(
		EquatableArray<TypeLibraryNamespaceNode> Tree,
		EquatableArray<string> PublicTypeNames
	);

	static GeneratorResult<TypeLibraryModel> BuildTarget(
		INamedTypeSymbol specSymbol,
		FrameworkTypeLibraryModel framework,
		CancellationToken cancellationToken
	)
	{
		// Validation diagnostics are reported by TypeLibraryValidationAnalyzer; the generator carries them
		// on the result so it can gate generation on ShouldProcess without emitting the diagnostics itself.
		List<ReportableDiagnostic> diagnostics = [];
		var generateAttribute = GetAttribute(specSymbol, GeneratorTypeLibrary.Attirbutes.GenerateTypeLibraryAttribute);
		if (generateAttribute is null)
			return GeneratorResult<TypeLibraryModel>.Empty;

		var className = GetNamedArgument(generateAttribute, "ClassName", (string?)null) ?? "TypeLibrary";
		var outputNamespace = GetNamedArgument(generateAttribute, "Namespace", (string?)null);
		var specDocumentation = ExtractDocumentation(specSymbol);

		List<NamespaceNodeBuilder> root = [];
		Dictionary<string, NamespaceNodeBuilder> nodeLookup = new(StringComparer.Ordinal);

		// The generated type library inherits the full framework PurviewTypeLibrary shape, so every
		// framework member is present before the user's [TypeRef] members are merged in. The shape was
		// already walked (once per compilation) by the GetFrameworkTypeLibraryTree stage.
		AddFrameworkTree(framework.Tree, root, nodeLookup);

		foreach (var field in specSymbol.GetMembers().OfType<IFieldSymbol>())
			ProcessTypeRefField(field, root, nodeLookup, diagnostics, cancellationToken);

		if (diagnostics.Any(d => d.IsBlocking))
			return GeneratorResult<TypeLibraryModel>.Create([.. diagnostics]);

		ProcessEnumValueMembers(specSymbol, nodeLookup, diagnostics, cancellationToken);

		if (diagnostics.Any(d => d.IsBlocking))
			return GeneratorResult<TypeLibraryModel>.Create([.. diagnostics]);

		ProcessEnumValuesMembers(specSymbol, nodeLookup, diagnostics, cancellationToken);

		if (diagnostics.Any(d => d.IsBlocking))
			return GeneratorResult<TypeLibraryModel>.Create([.. diagnostics]);

		// Author documentation is copied into generated files whose namespace and using set differ from
		// the source, so framework-type cref references are rendered as inline code before they are
		// emitted. Non-framework crefs keep their original form.
		RewriteFrameworkCrefs(root, framework.PublicTypeNames);
		specDocumentation = FrameworkCrefRewriter.RewriteDocumentation(specDocumentation, framework.PublicTypeNames);

		TypeLibraryModel model = new(
			Specifier: specSymbol.ContainingNamespace.IsGlobalNamespace
				? specSymbol.Name
				: $"{specSymbol.ContainingNamespace.ToDisplayString()}.{specSymbol.Name}",
			SpecClassName: specSymbol.Name,
			SpecNamespace: specSymbol.ContainingNamespace.IsGlobalNamespace
				? null
				: specSymbol.ContainingNamespace.ToDisplayString(),
			SpecAccessibility: specSymbol.DeclaredAccessibility.ToTypeDeclarationAccessibility(),
			Namespace: string.IsNullOrWhiteSpace(outputNamespace) ? null : outputNamespace,
			ClassName: className,
			Documentation: specDocumentation,
			Namespaces: new EquatableArray<TypeLibraryNamespaceNode>([
				.. root.OrderBy(static n => n.Name, StringComparer.Ordinal).Select(static n => n.ToModel()),
			])
		);

		return GeneratorResult<TypeLibraryModel>.Create(
			model,
			System.Collections.Immutable.ImmutableArray.CreateRange(diagnostics)
		);
	}

	/// <summary>
	/// Processes a single <c>[TypeRef]</c> field, adding its generated member to the tree or reporting a
	/// diagnostic. Fields that carry a <c>[TypeRef]</c> are processed here even when they also declare
	/// <c>[EnumValue]</c>/<c>[EnumValues]</c> markers; fields without a <c>[TypeRef]</c> are handled by
	/// the enum-value processing passes.
	/// </summary>
	static void ProcessTypeRefField(
		IFieldSymbol field,
		List<NamespaceNodeBuilder> root,
		Dictionary<string, NamespaceNodeBuilder> nodeLookup,
		List<ReportableDiagnostic> diagnostics,
		CancellationToken cancellationToken
	)
	{
		var typeRef = GetAttribute(field, GeneratorTypeLibrary.Attirbutes.TypeRefAttribute);
		if (typeRef is null)
			return;

		var isTypeIdentity = string.Equals(field.Type.Name, "TypeIdentity", StringComparison.Ordinal);
		var isTypeReference = string.Equals(field.Type.Name, "TypeReference", StringComparison.Ordinal);
		var documentation = ExtractDocumentation(field);
		var includeInGetTypes = GetIncludeInGetTypes(typeRef);
		var generateFullNameConstant = GetGenerateFullNameConstant(typeRef);

		TypeLibraryMemberModel member;

		if (isTypeReference || (isTypeIdentity && HasRealInitializer(field, cancellationToken)))
		{
			// A TypeReference, or an initialised TypeIdentity, follows the value-member rules: it
			// requires a value and internal accessibility.
			var initializer = ReadInitializerExpression(field, cancellationToken);
			if (initializer is null)
			{
				diagnostics.Add(
					ReportableDiagnostic.Create(
						TypeLibraryDiagnosticRules.ReferenceMemberMissingInitializer,
						isBlocking: true,
						field,
						field.Name
					)
				);
				return;
			}

			if (field.DeclaredAccessibility != Accessibility.Internal)
			{
				diagnostics.Add(
					ReportableDiagnostic.Create(
						TypeLibraryDiagnosticRules.MemberAccessibilityInvalid,
						isBlocking: true,
						field,
						field.Name
					)
				);
				return;
			}

			var placementNamespace = GetPlacementNamespace(typeRef);
			member = new(
				field.Name,
				null,
				null,
				0,
				initializer,
				IsTypeReference: isTypeReference,
				documentation,
				includeInGetTypes,
				generateFullNameConstant
			);
			AddToTree(root, nodeLookup, placementNamespace ?? string.Empty, member);
		}
		else if (isTypeIdentity)
		{
			// A plain TypeIdentity is a generation marker: private accessibility.
			if (field.DeclaredAccessibility != Accessibility.Private)
			{
				diagnostics.Add(
					ReportableDiagnostic.Create(
						TypeLibraryDiagnosticRules.MemberAccessibilityInvalid,
						isBlocking: true,
						field,
						field.Name
					)
				);
				return;
			}

			if (HasNoInitializer(field, cancellationToken))
			{
				diagnostics.Add(
					ReportableDiagnostic.Create(
						TypeLibraryDiagnosticRules.MarkerMissingDefaultInitializer,
						isBlocking: false,
						field,
						field.Name
					)
				);
			}

			var before = diagnostics.Count;
			var (typeName, memberNamespace, genericArity) = ReadTypeRef(typeRef, field, diagnostics);
			if (diagnostics.Count > before)
				return;

			member = new(
				field.Name,
				typeName,
				memberNamespace,
				genericArity,
				null,
				IsTypeReference: false,
				documentation,
				includeInGetTypes,
				generateFullNameConstant
			);
			AddToTree(root, nodeLookup, memberNamespace, member);
		}
		else
		{
			diagnostics.Add(
				ReportableDiagnostic.Create(
					TypeLibraryDiagnosticRules.MemberTypeInvalid,
					isBlocking: true,
					field,
					field.Name,
					field.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)
				)
			);
		}
	}

	/// <summary>
	/// Resolves the placement namespace for a value member from the <c>[TypeRef]</c> namespace argument
	/// or named property.
	/// </summary>
	static string? GetPlacementNamespace(AttributeData typeRef) =>
		GetNamedArgument(typeRef, "Namespace", (string?)null)
		?? GetConstructorArgument(typeRef, 0, (string?)null)
		?? GetConstructorArgument(typeRef, 1, (string?)null);

	/// <summary>
	/// Resolves the <c>IncludeInGetTypes</c> flag from the named property, the named constructor argument,
	/// or the positional <c>includeInGetTypes</c> constructor argument (the final parameter of either ctor).
	/// </summary>
	static bool GetIncludeInGetTypes(AttributeData typeRef)
	{
		var named =
			GetNamedArgument(typeRef, "IncludeInGetTypes", (bool?)null)
			?? GetNamedArgument(typeRef, "includeInGetTypes", (bool?)null);
		if (named is not null)
			return named.Value;

		if (typeRef.AttributeConstructor is { } constructor)
		{
			for (var index = 0; index < constructor.Parameters.Length; index++)
			{
				if (constructor.Parameters[index].Name == "includeInGetTypes")
					return index < typeRef.ConstructorArguments.Length
						&& typeRef.ConstructorArguments[index].Value is true;
			}
		}

		return false;
	}

	/// <summary>
	/// Resolves the <c>GenerateFullNameConstant</c> flag from the named property, the named constructor argument,
	/// or the positional <c>generateFullNameConstant</c> constructor argument (the final parameter of either ctor).
	/// </summary>
	static bool GetGenerateFullNameConstant(AttributeData typeRef)
	{
		var named =
			GetNamedArgument(typeRef, "GenerateFullNameConst", (bool?)null)
			?? GetNamedArgument(typeRef, "generateFullNameConst", (bool?)null);
		if (named is not null)
			return named.Value;

		if (typeRef.AttributeConstructor is { } constructor)
		{
			for (var index = 0; index < constructor.Parameters.Length; index++)
			{
				if (constructor.Parameters[index].Name == "generateFullNameConst")
					return index < typeRef.ConstructorArguments.Length
						&& typeRef.ConstructorArguments[index].Value is true;
			}
		}

		return false;
	}

	/// <summary>
	/// Builds the framework model consumed by every type-library target: the fixed
	/// <c>PurviewTypeLibrary</c> shape plus the public type names the framework owns.
	/// </summary>
	static FrameworkTypeLibraryModel BuildFrameworkModel(Compilation compilation) =>
		new(BuildFrameworkTree(compilation), BuildFrameworkOwnedPublicTypeNames(compilation));

	/// <summary>
	/// Collects the public type names of the framework assembly and of the framework's
	/// <c>Generators</c> namespace, whose attribute types the framework emits into the component itself.
	/// </summary>
	static EquatableArray<string> BuildFrameworkOwnedPublicTypeNames(Compilation compilation)
	{
		List<string> names = [];

		foreach (var reference in compilation.References)
		{
			if (compilation.GetAssemblyOrModuleSymbol(reference) is not IAssemblySymbol assembly)
				continue;

			if (string.Equals(assembly.Identity.Name, "Purview.SourceGeneratorFramework", StringComparison.Ordinal))
				CollectPublicTypeNames(assembly.GlobalNamespace, names);
		}

		var generatedNamespace = compilation
			.GlobalNamespace.GetNamespaceMembers()
			.FirstOrDefault(static @namespace => @namespace.Name == "Purview")
			?.GetNamespaceMembers()
			.FirstOrDefault(static @namespace => @namespace.Name == "SourceGeneratorFramework")
			?.GetNamespaceMembers()
			.FirstOrDefault(static @namespace => @namespace.Name == "Generators");

		if (generatedNamespace is not null)
			CollectPublicTypeNames(generatedNamespace, names);

		return new EquatableArray<string>([
			.. names.Distinct(StringComparer.Ordinal).OrderBy(static name => name, StringComparer.Ordinal),
		]);
	}

	/// <summary>
	/// Collects public type names from a namespace. Nested types are skipped: <c>PurviewTypeLibrary</c>
	/// mirrors the BCL as nested classes, none of which are framework types.
	/// </summary>
	static void CollectPublicTypeNames(INamespaceSymbol @namespace, List<string> names)
	{
		foreach (var member in @namespace.GetMembers())
		{
			if (member is INamespaceSymbol childNamespace)
			{
				CollectPublicTypeNames(childNamespace, names);
				continue;
			}

			if (member is INamedTypeSymbol { DeclaredAccessibility: Accessibility.Public } namedType)
				names.Add(namedType.Name);
		}
	}

	/// <summary>
	/// Renders framework-type cref references in copied documentation as inline code for every member
	/// and enum value in the type-library tree.
	/// </summary>
	static void RewriteFrameworkCrefs(List<NamespaceNodeBuilder> nodes, EquatableArray<string> frameworkTypeNames)
	{
		if (frameworkTypeNames.IsEmpty)
			return;

		foreach (var node in nodes)
		{
			for (var index = 0; index < node.Members.Count; index++)
			{
				var member = node.Members[index];
				node.Members[index] = member with
				{
					Documentation = FrameworkCrefRewriter.RewriteDocumentation(
						member.Documentation,
						frameworkTypeNames
					),
				};
			}

			foreach (var group in node.EnumGroups)
			{
				for (var index = 0; index < group.Values.Count; index++)
				{
					var value = group.Values[index];
					group.Values[index] = value with
					{
						Documentation = FrameworkCrefRewriter.RewriteDocumentation(
							value.Documentation,
							frameworkTypeNames
						),
					};
				}
			}

			RewriteFrameworkCrefs(node.Children, frameworkTypeNames);
		}
	}

	/// <summary>
	/// Walks the framework <c>PurviewTypeLibrary</c> nested namespace classes and their members once,
	/// producing a value-equatable model that is cached by the <c>GetFrameworkTypeLibraryTree</c> pipeline
	/// stage. Each member is emitted as an alias reference to the framework value so arity and generic
	/// construction are preserved exactly.
	/// </summary>
	static EquatableArray<TypeLibraryNamespaceNode> BuildFrameworkTree(Compilation compilation)
	{
		var frameworkType = compilation.GetTypeByMetadataName("Purview.SourceGeneratorFramework.PurviewTypeLibrary");
		if (frameworkType is null)
			return EquatableArray<TypeLibraryNamespaceNode>.Empty;

		List<NamespaceNodeBuilder> root = [];
		Dictionary<string, NamespaceNodeBuilder> lookup = new(StringComparer.Ordinal);

		foreach (var nestedType in frameworkType.GetTypeMembers())
			AddFrameworkNode(root, lookup, nestedType);

		return new EquatableArray<TypeLibraryNamespaceNode>([
			.. root.OrderBy(static n => n.Name, StringComparer.Ordinal).Select(static n => n.ToModel()),
		]);
	}

	static void AddFrameworkNode(
		List<NamespaceNodeBuilder> nodes,
		Dictionary<string, NamespaceNodeBuilder> lookup,
		INamedTypeSymbol nestedType
	)
	{
		var namespaceConst =
			nestedType
				.GetMembers()
				.OfType<IFieldSymbol>()
				.FirstOrDefault(static field => field.IsConst && field.ConstantValue is string)
				?.ConstantValue as string
			?? nestedType.Name;

		var node = GetOrAddNode(nodes, lookup, namespaceConst, nestedType.Name);

		var nestedDisplayName = nestedType.ToDisplayString();
		foreach (var member in nestedType.GetMembers().OfType<IFieldSymbol>())
		{
			if (member.IsConst)
				continue;

			var reference = $"global::{nestedDisplayName}.{member.Name}";
			var isTypeReference = string.Equals(member.Type.Name, "TypeReference", StringComparison.Ordinal);
			node.Members.Add(new TypeLibraryMemberModel(member.Name, null, null, 0, reference, isTypeReference, null));
		}

		foreach (var child in nestedType.GetTypeMembers())
			AddFrameworkNode(node.Children, lookup, child);
	}

	/// <summary>
	/// Materializes the cached framework tree into mutable builder nodes so user <c>[TypeRef]</c> members can
	/// be merged in without any further semantic queries.
	/// </summary>
	static void AddFrameworkTree(
		EquatableArray<TypeLibraryNamespaceNode> frameworkTree,
		List<NamespaceNodeBuilder> root,
		Dictionary<string, NamespaceNodeBuilder> lookup
	)
	{
		foreach (var node in frameworkTree)
			AddFrameworkNode(root, lookup, node);
	}

	static void AddFrameworkNode(
		List<NamespaceNodeBuilder> nodes,
		Dictionary<string, NamespaceNodeBuilder> lookup,
		TypeLibraryNamespaceNode node
	)
	{
		var built = GetOrAddNode(nodes, lookup, node.NamespaceValue, node.Name);
		foreach (var member in node.Members)
			built.Members.Add(member);

		foreach (var child in node.Children)
			AddFrameworkNode(built.Children, lookup, child);
	}

	static void AddToTree(
		List<NamespaceNodeBuilder> root,
		Dictionary<string, NamespaceNodeBuilder> lookup,
		string @namespace,
		TypeLibraryMemberModel member
	)
	{
		NamespaceNodeBuilder? leaf = null;
		var accumulated = string.Empty;
		var nodes = root;
		foreach (var segment in @namespace.Split('.'))
		{
			accumulated = accumulated.Length == 0 ? segment : accumulated + "." + segment;
			leaf = GetOrAddNode(nodes, lookup, accumulated, segment);
			nodes = leaf.Children;
		}

		// A user member shadows an inherited framework member of the same name in the same leaf.
		leaf!.Members.RemoveAll(m => m.MemberName == member.MemberName);
		leaf.Members.Add(member);
	}

	/// <summary>
	/// Processes an <c>[EnumValue]</c> marker field, resolving its enum type and attaching the value to
	/// the matching <c>{EnumName}Values</c> group in the enum's namespace node.
	/// </summary>
	static void ProcessEnumValueMembers(
		INamedTypeSymbol specSymbol,
		Dictionary<string, NamespaceNodeBuilder> nodeLookup,
		List<ReportableDiagnostic> diagnostics,
		CancellationToken cancellationToken
	)
	{
		foreach (var field in specSymbol.GetMembers().OfType<IFieldSymbol>())
		{
			cancellationToken.ThrowIfCancellationRequested();

			var enumValues = GetAttributes(field, GeneratorTypeLibrary.Attirbutes.EnumValueAttribute);
			if (enumValues.Count == 0)
				continue;

			foreach (var enumValue in enumValues)
				ProcessEnumValue(field, enumValue, nodeLookup, diagnostics, cancellationToken);
		}
	}

	static void ProcessEnumValue(
		IFieldSymbol field,
		AttributeData enumValue,
		Dictionary<string, NamespaceNodeBuilder> nodeLookup,
		List<ReportableDiagnostic> diagnostics,
		CancellationToken cancellationToken
	)
	{
		var fieldName = field.Name;

		if (field.DeclaredAccessibility != Accessibility.Private)
		{
			diagnostics.Add(
				ReportableDiagnostic.Create(
					TypeLibraryDiagnosticRules.MemberAccessibilityInvalid,
					isBlocking: true,
					field,
					fieldName
				)
			);
			return;
		}

		if (HasNoInitializer(field, cancellationToken))
		{
			diagnostics.Add(
				ReportableDiagnostic.Create(
					TypeLibraryDiagnosticRules.MarkerMissingDefaultInitializer,
					isBlocking: false,
					field,
					fieldName
				)
			);
		}

		var isTypeIdentity = string.Equals(field.Type.Name, "TypeIdentity", StringComparison.Ordinal);
		var isEnumValueDefinition = string.Equals(field.Type.Name, "EnumValueDefinition", StringComparison.Ordinal);
		if (!isTypeIdentity && !isEnumValueDefinition)
		{
			diagnostics.Add(
				ReportableDiagnostic.Create(
					TypeLibraryDiagnosticRules.EnumValueMemberTypeInvalid,
					isBlocking: true,
					field,
					fieldName,
					field.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)
				)
			);
			return;
		}

		// When the field also declares a [TypeRef], the attribute's first argument is the enum member
		// name and the enum type is inferred from the sibling [TypeRef] on the same field.
		var typeRef = GetAttribute(field, GeneratorTypeLibrary.Attirbutes.TypeRefAttribute);

		var (declaredName, declaredNamespace, value, underlyingType, aliases) = ReadEnumValue(enumValue);
		if (declaredName is null)
		{
			diagnostics.Add(
				ReportableDiagnostic.Create(
					TypeLibraryDiagnosticRules.EnumValueEnumTypeNotDeclared,
					isBlocking: true,
					field,
					fieldName,
					string.Empty,
					string.Empty
				)
			);
			return;
		}

		string enumName;
		string enumNamespace;
		string memberName;

		if (typeRef is not null)
		{
			var before = diagnostics.Count;
			var (typeName, memberNamespace, _) = ReadTypeRef(typeRef, field, diagnostics);
			if (diagnostics.Count > before)
				return;

			memberName = declaredName;
			enumName = typeName;
			enumNamespace = memberNamespace ?? string.Empty;
		}
		else
		{
			memberName = fieldName;
			enumName = declaredName;
			enumNamespace = declaredNamespace ?? string.Empty;
		}

		if (!nodeLookup.TryGetValue(enumNamespace, out var leaf))
		{
			diagnostics.Add(
				ReportableDiagnostic.Create(
					TypeLibraryDiagnosticRules.EnumValueEnumTypeNotDeclared,
					isBlocking: true,
					field,
					fieldName,
					enumName,
					enumNamespace
				)
			);
			return;
		}

		// The enum type must be declared by a sibling [TypeRef] TypeIdentity marker in the same node.
		var enumMember = leaf.Members.FirstOrDefault(m => m.MemberName == enumName && !m.IsReference);
		if (enumMember is null)
		{
			diagnostics.Add(
				ReportableDiagnostic.Create(
					TypeLibraryDiagnosticRules.EnumValueEnumTypeNotDeclared,
					isBlocking: true,
					field,
					fieldName,
					enumName,
					enumNamespace
				)
			);
			return;
		}

		var group = leaf.EnumGroups.FirstOrDefault(g => g.EnumName == enumName);
		if (group is null)
		{
			group = new EnumGroupBuilder(enumName, enumNamespace, enumMember.GenerateFullNameConstant);
			leaf.EnumGroups.Add(group);
		}

		if (group.Values.Any(v => v.MemberName == memberName))
		{
			diagnostics.Add(
				ReportableDiagnostic.Create(
					TypeLibraryDiagnosticRules.EnumValueDuplicateMember,
					isBlocking: true,
					field,
					fieldName,
					enumName
				)
			);
			return;
		}

		if (group.Values.Any(v => v.Value == value))
		{
			diagnostics.Add(
				ReportableDiagnostic.Create(
					TypeLibraryDiagnosticRules.EnumValueDuplicateValue,
					isBlocking: false,
					field,
					fieldName,
					enumName
				)
			);
		}

		var documentation = ExtractDocumentation(field);
		// The [TypeRef] field that carries the marker is already referenced through the node members;
		// only standalone [EnumValue] marker fields need to be added to the marker reference list.
		if (typeRef is null)
			group.MarkerFieldNames.Add(fieldName);
		group.Values.Add(
			new TypeLibraryEnumValueModel(
				memberName,
				value,
				underlyingType,
				aliases is null or { Length: 0 }
					? EquatableArray<string>.Empty
					: new EquatableArray<string>([.. aliases]),
				documentation
			)
		);
	}

	/// <summary>
	/// Processes an <c>[EnumValues]</c> marker field, resolving its enum type and attaching every member
	/// of the enum to the matching <c>{EnumName}Values</c> group in the enum's namespace node.
	/// </summary>
	static void ProcessEnumValuesMembers(
		INamedTypeSymbol specSymbol,
		Dictionary<string, NamespaceNodeBuilder> nodeLookup,
		List<ReportableDiagnostic> diagnostics,
		CancellationToken cancellationToken
	)
	{
		foreach (var field in specSymbol.GetMembers().OfType<IFieldSymbol>())
		{
			cancellationToken.ThrowIfCancellationRequested();

			var enumValues = GetAttribute(field, GeneratorTypeLibrary.Attirbutes.EnumValuesAttribute);
			if (enumValues is null)
				continue;

			ProcessEnumValues(field, enumValues, nodeLookup, diagnostics, cancellationToken);
		}
	}

	static void ProcessEnumValues(
		IFieldSymbol field,
		AttributeData enumValues,
		Dictionary<string, NamespaceNodeBuilder> nodeLookup,
		List<ReportableDiagnostic> diagnostics,
		CancellationToken cancellationToken
	)
	{
		var fieldName = field.Name;

		if (field.DeclaredAccessibility != Accessibility.Private)
		{
			diagnostics.Add(
				ReportableDiagnostic.Create(
					TypeLibraryDiagnosticRules.MemberAccessibilityInvalid,
					isBlocking: true,
					field,
					fieldName
				)
			);
			return;
		}

		if (HasNoInitializer(field, cancellationToken))
		{
			diagnostics.Add(
				ReportableDiagnostic.Create(
					TypeLibraryDiagnosticRules.MarkerMissingDefaultInitializer,
					isBlocking: false,
					field,
					fieldName
				)
			);
		}

		var isTypeIdentity = string.Equals(field.Type.Name, "TypeIdentity", StringComparison.Ordinal);
		var isEnumValueDefinition = string.Equals(field.Type.Name, "EnumValueDefinition", StringComparison.Ordinal);
		if (!isTypeIdentity && !isEnumValueDefinition)
		{
			diagnostics.Add(
				ReportableDiagnostic.Create(
					TypeLibraryDiagnosticRules.EnumValueMemberTypeInvalid,
					isBlocking: true,
					field,
					fieldName,
					field.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)
				)
			);
			return;
		}

		var enumType = ReadEnumValuesType(enumValues);
		if (enumType is null || enumType.TypeKind != TypeKind.Enum)
		{
			diagnostics.Add(
				ReportableDiagnostic.Create(
					TypeLibraryDiagnosticRules.EnumValuesTypeNotEnum,
					isBlocking: true,
					field,
					fieldName,
					enumType?.ToDisplayString() ?? string.Empty
				)
			);
			return;
		}

		var enumName = enumType.Name;
		var enumNamespace = enumType.ContainingNamespace.IsGlobalNamespace
			? string.Empty
			: enumType.ContainingNamespace.ToDisplayString();

		if (!nodeLookup.TryGetValue(enumNamespace, out var leaf))
		{
			diagnostics.Add(
				ReportableDiagnostic.Create(
					TypeLibraryDiagnosticRules.EnumValueEnumTypeNotDeclared,
					isBlocking: true,
					field,
					fieldName,
					enumName,
					enumNamespace
				)
			);
			return;
		}

		// The enum type must be declared by a sibling [TypeRef] TypeIdentity marker in the same node.
		var enumMember = leaf.Members.FirstOrDefault(m => m.MemberName == enumName && !m.IsReference);
		if (enumMember is null)
		{
			diagnostics.Add(
				ReportableDiagnostic.Create(
					TypeLibraryDiagnosticRules.EnumValueEnumTypeNotDeclared,
					isBlocking: true,
					field,
					fieldName,
					enumName,
					enumNamespace
				)
			);
			return;
		}

		var group = leaf.EnumGroups.FirstOrDefault(g => g.EnumName == enumName);
		if (group is null)
		{
			group = new EnumGroupBuilder(enumName, enumNamespace, enumMember.GenerateFullNameConstant);
			leaf.EnumGroups.Add(group);
		}

		if (group.BulkMarkerFieldName is not null)
		{
			diagnostics.Add(
				ReportableDiagnostic.Create(
					TypeLibraryDiagnosticRules.EnumValueDuplicateMember,
					isBlocking: true,
					field,
					fieldName,
					enumName
				)
			);
			return;
		}

		var underlyingType = EnumUnderlyingTypeFromSymbol(enumType);
		foreach (var enumMemberSymbol in enumType.GetMembers().OfType<IFieldSymbol>())
		{
			cancellationToken.ThrowIfCancellationRequested();

			if (!enumMemberSymbol.HasConstantValue)
				continue;

			var memberName = enumMemberSymbol.Name;
			if (group.Values.Any(v => v.MemberName == memberName))
			{
				diagnostics.Add(
					ReportableDiagnostic.Create(
						TypeLibraryDiagnosticRules.EnumValueDuplicateMember,
						isBlocking: true,
						field,
						fieldName,
						enumName
					)
				);
				continue;
			}

			var value = ReadEnumConstant(enumMemberSymbol.ConstantValue);
			if (group.Values.Any(v => v.Value == value))
			{
				diagnostics.Add(
					ReportableDiagnostic.Create(
						TypeLibraryDiagnosticRules.EnumValueDuplicateValue,
						isBlocking: false,
						field,
						fieldName,
						enumName
					)
				);
			}

			group.Values.Add(
				new TypeLibraryEnumValueModel(
					memberName,
					value,
					underlyingType,
					EquatableArray<string>.Empty,
					ExtractDocumentation(enumMemberSymbol)
				)
			);
		}

		group.BulkMarkerFieldName = fieldName;
		group.MarkerFieldNames.Add(fieldName);
	}

	/// <summary>
	/// Resolves the enum type from an <c>[EnumValues(typeof(...))]</c> constructor argument.
	/// </summary>
	static INamedTypeSymbol? ReadEnumValuesType(AttributeData enumValues)
	{
		if (enumValues.ConstructorArguments.Length == 0)
			return null;

		// The first constructor argument is the enum type, which is a System.Type reference. The value is an ITypeSymbol.
		return enumValues.ConstructorArguments[0].Value as INamedTypeSymbol;
	}

	/// <summary>
	/// Maps an enum symbol's underlying type to the <see cref="EnumUnderlyingType"/> model value.
	/// </summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0072:Add missing cases")]
	static EnumUnderlyingType EnumUnderlyingTypeFromSymbol(INamedTypeSymbol enumType) =>
		enumType.EnumUnderlyingType?.SpecialType switch
		{
			SpecialType.System_Byte => EnumUnderlyingType.Byte,
			SpecialType.System_SByte => EnumUnderlyingType.SByte,
			SpecialType.System_Int16 => EnumUnderlyingType.Int16,
			SpecialType.System_UInt16 => EnumUnderlyingType.UInt16,
			SpecialType.System_Int32 => EnumUnderlyingType.Int32,
			SpecialType.System_UInt32 => EnumUnderlyingType.UInt32,
			SpecialType.System_Int64 => EnumUnderlyingType.Int64,
			SpecialType.System_UInt64 => EnumUnderlyingType.UInt64,
			_ => EnumUnderlyingType.Int32,
		};

	/// <summary>
	/// Reads an enum member's constant value as a <see cref="decimal"/>, preserving every underlying type.
	/// </summary>
	static decimal ReadEnumConstant(object? constantValue) =>
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
			_ => Convert.ToDecimal(constantValue, CultureInfo.InvariantCulture),
		};

	/// <summary>
	/// Reads an <c>[EnumValue]</c> attribute, supporting the explicit enum-name/namespace form and the
	/// single fully-qualified enum type name form. Returns <see langword="null"/> names for an empty or
	/// unresolvable fully-qualified form.
	/// </summary>
	static (
		string? EnumName,
		string? Namespace,
		decimal Value,
		EnumUnderlyingType UnderlyingType,
		string[]? Aliases
	) ReadEnumValue(AttributeData enumValue)
	{
		var constructor = enumValue.AttributeConstructor;
		var isFullNameForm =
			constructor is { Parameters.Length: > 0 } && constructor.Parameters[0].Name == "enumFullName";

		if (isFullNameForm)
		{
			var fullName = GetConstructorArgument(enumValue, 0, (string?)null);
			if (string.IsNullOrWhiteSpace(fullName))
				return (null, null, 0, EnumUnderlyingType.Int32, null);

			var lastDot = fullName!.LastIndexOf('.');
			var enumName = lastDot < 0 ? fullName : fullName.Substring(lastDot + 1);
			var enumNamespace = lastDot < 0 ? null : fullName.Substring(0, lastDot);

			var (value, underlyingType) = ReadEnumValueConstant(enumValue, 1);

			return (enumName, enumNamespace, value, underlyingType, GetStringArrayArgument(enumValue, 2));
		}

		var (explicitValue, explicitUnderlyingType) = ReadEnumValueConstant(enumValue, 2);

		return (
			GetConstructorArgument(enumValue, 0, (string?)null),
			GetConstructorArgument(enumValue, 1, (string?)null),
			explicitValue,
			explicitUnderlyingType,
			GetStringArrayArgument(enumValue, 3)
		);
	}

	/// <summary>
	/// Reads an enum value constructor argument, preserving the declared numeric type so the enum's
	/// underlying type (<c>byte</c> through <c>ulong</c>) is inferred from the literal.
	/// </summary>
	static (decimal Value, EnumUnderlyingType UnderlyingType) ReadEnumValueConstant(
		AttributeData attributeData,
		int index
	)
	{
		if (index < 0 || index >= attributeData.ConstructorArguments.Length)
			return (0, EnumUnderlyingType.Int32);

		var raw = attributeData.ConstructorArguments[index].Value;
		var value = raw switch
		{
			byte b => b,
			sbyte sb => sb,
			short s => s,
			ushort us => us,
			int i => i,
			uint ui => ui,
			long l => l,
			ulong ul => ul,
			_ => Convert.ToDecimal(raw, CultureInfo.InvariantCulture),
		};
		var underlyingType = raw switch
		{
			byte => EnumUnderlyingType.Byte,
			sbyte => EnumUnderlyingType.SByte,
			short => EnumUnderlyingType.Int16,
			ushort => EnumUnderlyingType.UInt16,
			int => EnumUnderlyingType.Int32,
			uint => EnumUnderlyingType.UInt32,
			long => EnumUnderlyingType.Int64,
			ulong => EnumUnderlyingType.UInt64,
			_ => EnumUnderlyingType.Int32,
		};

		return (value, underlyingType);
	}

	static NamespaceNodeBuilder GetOrAddNode(
		List<NamespaceNodeBuilder> nodes,
		Dictionary<string, NamespaceNodeBuilder> lookup,
		string namespaceValue,
		string segment
	)
	{
		if (lookup.TryGetValue(namespaceValue, out var existing))
			return existing;

		NamespaceNodeBuilder node = new(segment, namespaceValue);
		lookup.Add(namespaceValue, node);
		nodes.Add(node);
		return node;
	}

	static (string TypeName, string Namespace, int Arity) ReadTypeRef(
		AttributeData typeRef,
		IFieldSymbol field,
		List<ReportableDiagnostic> diagnostics
	)
	{
		var fieldName = field.Name;
		var namedNamespace = GetNamedArgument(typeRef, "Namespace", (string?)null);
		var namedArity = GetNamedArgument(typeRef, "Arity", -1);

		// The namespace-only overload declares a single string parameter; when used, the type name
		// defaults to the member (field) name.
		var isNamespaceOnlyForm =
			typeRef.AttributeConstructor is { Parameters.Length: > 0 }
			&& typeRef.AttributeConstructor.Parameters[0].Type.SpecialType == SpecialType.System_String;

		if (isNamespaceOnlyForm)
		{
			var memberName = fieldName;
			var memberNamespace = namedNamespace ?? GetConstructorArgument(typeRef, 0, (string?)null);
			if (string.IsNullOrWhiteSpace(memberNamespace))
			{
				diagnostics.Add(
					ReportableDiagnostic.Create(
						TypeLibraryDiagnosticRules.MemberTypeNotResolved,
						isBlocking: true,
						field,
						fieldName
					)
				);
				return (memberName, string.Empty, 0);
			}

			var memberArity = namedArity != -1 ? namedArity : GetConstructorArgument(typeRef, 1, 0);
			if (memberArity < 0)
			{
				diagnostics.Add(
					ReportableDiagnostic.Create(
						TypeLibraryDiagnosticRules.MemberTypeNotResolved,
						isBlocking: true,
						field,
						fieldName
					)
				);
				return (memberName, memberNamespace!, 0);
			}

			return (memberName, memberNamespace!, memberArity);
		}

		if (typeRef.ConstructorArguments.Length == 0)
		{
			diagnostics.Add(
				ReportableDiagnostic.Create(
					TypeLibraryDiagnosticRules.MemberTypeNotResolved,
					isBlocking: true,
					field,
					fieldName
				)
			);
			return (string.Empty, string.Empty, 0);
		}

		var typeArgument = typeRef.ConstructorArguments[0];
		string typeName;
		string? inferredNamespace = null;
		int? detectedArity;

#pragma warning disable format
		switch (typeArgument.Value)
		{
			case ITypeSymbol typeSymbol:
			{
				if (typeSymbol.ContainingNamespace.IsGlobalNamespace)
				{
					diagnostics.Add(
						ReportableDiagnostic.Create(
							TypeLibraryDiagnosticRules.MemberTypeNotResolved,
							isBlocking: true,
							field,
							fieldName
						)
					);
					return (typeSymbol.Name, string.Empty, 0);
				}

				typeName = typeSymbol.Name;
				inferredNamespace = typeSymbol.ContainingNamespace.ToDisplayString();
				detectedArity = (typeSymbol as INamedTypeSymbol)?.Arity;
				break;
			}
			case string typeNameString:
			{
				(typeName, detectedArity) = ParseStringTypeName(typeNameString);
				break;
			}
			default:
				diagnostics.Add(
					ReportableDiagnostic.Create(
						TypeLibraryDiagnosticRules.MemberTypeNotResolved,
						isBlocking: true,
						field,
						fieldName
					)
				);
				return (string.Empty, string.Empty, 0);
		}
#pragma warning restore format

		var resolvedNamespace =
			namedNamespace ?? GetConstructorArgument(typeRef, 1, (string?)null) ?? inferredNamespace;
		if (string.IsNullOrWhiteSpace(resolvedNamespace))
		{
			diagnostics.Add(
				ReportableDiagnostic.Create(
					TypeLibraryDiagnosticRules.MemberTypeNotResolved,
					isBlocking: true,
					field,
					fieldName
				)
			);
			return (typeName, string.Empty, 0);
		}

		var explicitArity = namedArity != -1 ? namedArity : GetConstructorArgument(typeRef, 2, -1);
		if (explicitArity is < 0 and not -1)
		{
			diagnostics.Add(
				ReportableDiagnostic.Create(
					TypeLibraryDiagnosticRules.MemberTypeNotResolved,
					isBlocking: true,
					field,
					fieldName
				)
			);
			return (typeName, resolvedNamespace!, 0);
		}

		var arity = explicitArity >= 0 ? explicitArity : (detectedArity ?? 0);
		return (typeName, resolvedNamespace!, arity);
	}

	/// <summary>
	/// Parses a type-name string, extracting any CLR generic-arity suffix such as
	/// <c>List`1</c> into the arity.
	/// </summary>
	static (string TypeName, int Arity) ParseStringTypeName(string typeName)
	{
		var backtick = typeName.IndexOf('`');
		if (backtick < 0 || backtick == typeName.Length - 1)
			return (typeName, 0);

		if (int.TryParse(typeName.Substring(backtick + 1), out var arity))
			return (typeName.Substring(0, backtick), arity);

		return (typeName, 0);
	}

	/// <summary>
	/// Determines whether the field declares a meaningful value initializer rather than a plain
	/// <c>default</c> marker.
	/// </summary>
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

	/// <summary>
	/// Reads a field's initializer expression, rewriting bare <c>PurviewTypeLibrary</c> references to
	/// the fully-qualified framework type so the copied expression resolves inside the generated class.
	/// </summary>
	static string? ReadInitializerExpression(IFieldSymbol field, CancellationToken cancellationToken)
	{
		foreach (var reference in field.DeclaringSyntaxReferences)
		{
			if (
				reference.GetSyntax(cancellationToken) is VariableDeclaratorSyntax declarator
				&& declarator.Initializer?.Value is { } value
			)
			{
				var rewritten = (ExpressionSyntax)new PurviewTypeLibraryReferenceRewriter().Visit(value);
				return rewritten.NormalizeWhitespace().ToString();
			}
		}

		return null;
	}

	/// <summary>
	/// Captures a symbol's XML documentation, returning the inner elements (summary, remarks, ...)
	/// without the surrounding <c>&lt;member&gt;</c> wrapper.
	/// </summary>
	static string? ExtractDocumentation(ISymbol symbol)
	{
		var xml = symbol.GetDocumentationCommentXml();
		if (string.IsNullOrWhiteSpace(xml))
			return null;

		try
		{
			var document = XDocument.Parse(xml, LoadOptions.PreserveWhitespace);
			var elements = document.Root?.Elements().ToList();
			if (elements is null || elements.Count == 0)
				return null;

			// Return the inner XML of the <member> element, preserving whitespace and formatting.
			var result = string.Join(
					"\n",
					elements.Select(static element => element.ToString(SaveOptions.DisableFormatting))
				)
				.Trim();

			return result.Length == 0 ? null : result;
		}
		catch
		{
			return null;
		}
	}

	sealed class PurviewTypeLibraryReferenceRewriter : CSharpSyntaxRewriter
	{
		public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
		{
			// Only rewrite PurviewTypeLibrary when used as a standalone value or the root of a member
			// access, never as a member-access name such as Foo.PurviewTypeLibrary.
			var isMemberAccessName =
				node.Parent is MemberAccessExpressionSyntax memberAccess && memberAccess.Name == node;

			if (!isMemberAccessName && node.Identifier.ValueText == "PurviewTypeLibrary")
			{
				return SyntaxFactory
					.ParseExpression("global::Purview.SourceGeneratorFramework.PurviewTypeLibrary")
					.WithTriviaFrom(node);
			}

			// If the identifier is not a standalone PurviewTypeLibrary, continue visiting its children.
			return base.VisitIdentifierName(node);
		}
	}

	static AttributeData? GetAttribute(ISymbol symbol, TypeIdentity attributeType)
	{
		foreach (var attribute in symbol.GetAttributes())
		{
			if (attribute.AttributeClass is not null && attributeType.Equals(attribute.AttributeClass))
				return attribute;
		}

		return null;
	}

	static List<AttributeData> GetAttributes(ISymbol symbol, TypeIdentity attributeType)
	{
		List<AttributeData> result = [];
		foreach (var attribute in symbol.GetAttributes())
		{
			if (attribute.AttributeClass is not null && attributeType.Equals(attribute.AttributeClass))
				result.Add(attribute);
		}

		return result;
	}

	static T? GetConstructorArgument<T>(AttributeData attributeData, int index, T? defaultValue)
	{
		if (index < 0 || index >= attributeData.ConstructorArguments.Length)
			return defaultValue;

		var constant = attributeData.ConstructorArguments[index];
		if (constant.Kind == TypedConstantKind.Array)
			return defaultValue;

		var value = constant.Value;
		if (value is T typedValue)
			return typedValue;
		if (value is null)
			return defaultValue;

		try
		{
			return (T?)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
		}
		catch
		{
			return defaultValue;
		}
	}

	/// <summary>
	/// Reads a <c>string[]</c> constructor argument, which Roslyn models as an array
	/// <see cref="TypedConstant"/> whose elements live on <see cref="TypedConstant.Values"/>.
	/// </summary>
	static string[]? GetStringArrayArgument(AttributeData attributeData, int index)
	{
		if (index < 0 || index >= attributeData.ConstructorArguments.Length)
			return null;

		var constant = attributeData.ConstructorArguments[index];
		if (constant.Kind != TypedConstantKind.Array || constant.IsNull)
			return null;

		var values = constant.Values;
		if (values.IsDefaultOrEmpty)
			return null;

		var result = new string[values.Length];
		for (var i = 0; i < values.Length; i++)
			result[i] = values[i].Value as string ?? string.Empty;

		return result;
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
				return (T?)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
			}
			catch
			{
				return defaultValue;
			}
		}
		return defaultValue;
	}

	sealed class NamespaceNodeBuilder(string name, string namespaceValue)
	{
		public string Name { get; } = name;

		public string NamespaceValue { get; } = namespaceValue;

		public List<TypeLibraryMemberModel> Members { get; } = [];

		public List<EnumGroupBuilder> EnumGroups { get; } = [];

		public List<NamespaceNodeBuilder> Children { get; } = [];

		public TypeLibraryNamespaceNode ToModel() =>
			new(
				Name,
				NamespaceValue,
				new EquatableArray<TypeLibraryMemberModel>([.. Members]),
				new EquatableArray<TypeLibraryEnumGroupModel>([.. EnumGroups.Select(static g => g.ToModel())]),
				new EquatableArray<TypeLibraryNamespaceNode>([.. Children.Select(static c => c.ToModel())])
			);
	}

	sealed class EnumGroupBuilder(string enumName, string enumNamespace, bool enumGeneratesFullNameConstant)
	{
		public string EnumName { get; } = enumName;

		public string EnumNamespace { get; } = enumNamespace;

		public bool EnumGeneratesFullNameConstant { get; } = enumGeneratesFullNameConstant;

		public string? BulkMarkerFieldName { get; set; }

		public List<string> MarkerFieldNames { get; } = [];

		public List<TypeLibraryEnumValueModel> Values { get; } = [];

		public TypeLibraryEnumGroupModel ToModel() =>
			new(
				EnumName,
				EnumNamespace,
				EnumGeneratesFullNameConstant,
				new EquatableArray<TypeLibraryEnumValueModel>([.. Values]),
				new EquatableArray<string>([.. MarkerFieldNames])
			);
	}
}
