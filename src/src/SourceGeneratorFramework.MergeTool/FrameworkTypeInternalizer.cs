using System.Collections.Immutable;
using Mono.Cecil;

/// <summary>
/// Forces framework-owned types in a merged Roslyn component to non-public visibility.
/// <para>
/// ILRepack's <c>Internalize</c> is best-effort: a framework type that reaches the merged component's
/// public API surface stays public (for example a component type library exposing
/// <c>TypeIdentity</c>/<c>TypeReference</c> members), and the types the framework's own generators
/// emit into the component (the <c>Generators</c> attribute set, generated type libraries, marker
/// attributes) are not part of the merged framework assembly at all, so they are never internalized.
/// Either gap leaks <c>Purview.SourceGeneratorFramework</c> types out of what must be a
/// self-contained analyzer, and any project that loads the component alongside the real framework
/// assembly then reports CS0433 ambiguity for every leaked type.
/// </para>
/// <para>
/// This pass therefore rewrites every framework-owned type to non-public after the merge, and reports
/// what it could not internalize so the build fails instead of shipping a broken package. Roslyn
/// component entry points (generators, analyzers, code fix providers, refactoring providers) are
/// never internalized, including the framework's own bundled components whose entry points live under
/// the framework namespace: Roslyn can only instantiate public component types.
/// </para>
/// <para>
/// Ownership is evaluated through the declaring chain. Mono.Cecil reports an empty namespace for a
/// nested type, so matching on the type's own namespace alone never recognizes the framework's nested
/// containers (the generated type-library namespace classes, nested operator/enum groups, the
/// <c>CodeWriter</c> scopes) as owned, and never internalizes them. A nested type is therefore owned
/// when it or any of its declaring types is owned.
/// </para>
/// <para>
/// Public visibility is likewise evaluated through the declaring chain. A public nested type of an
/// internal type is unreachable from outside the component, so it is not part of the public surface
/// and must not be reported. This matters for the compiler-synthesised extension containers
/// (<c>&lt;G&gt;$</c>/<c>&lt;M&gt;$</c> types) the C# compiler emits for extension blocks: they are
/// nested public inside an internal static class, and every component that extends a framework type
/// has several.
/// </para>
/// <para>
/// The types the framework's own generators emit into a component (the generated type library and its
/// namespace classes, the attribute data models) are not part of the merged framework assembly, so
/// ILRepack never sees them; they are recognized by their
/// <c>System.CodeDom.Compiler.GeneratedCodeAttribute</c> tool name and internalized with the rest of
/// the framework surface. The component's unmerged output keeps the generated accessibility, so
/// in-repo consumers (code fixers, sibling assemblies, test harnesses) are unaffected.
/// </para>
/// </summary>
static class FrameworkTypeInternalizer
{
	/// <summary>
	/// Namespaces the framework owns in a merged component. A type is owned when its namespace equals
	/// the prefix or sits beneath it.
	/// </summary>
	public static readonly ImmutableArray<string> DefaultOwnedNamespaces = ["Purview.SourceGeneratorFramework"];

	/// <summary>
	/// Marker types the framework's generators emit into components and that must never be public.
	/// </summary>
	public static readonly ImmutableArray<string> DefaultOwnedTypeFullNames =
	[
		"Microsoft.CodeAnalysis.EmbeddedAttribute",
	];

	/// <summary>
	/// The attribute the framework's generators stamp on every type they emit into a component.
	/// </summary>
	const string GeneratedCodeAttributeFullName = "System.CodeDom.Compiler.GeneratedCodeAttribute";

	/// <summary>
	/// Generator tool names whose emitted types are framework-generated. Those types live in the
	/// component's own namespace (the framework's generated type library is emitted into the spec's
	/// namespace, or the global namespace when the spec sets none), so they are only recognizable
	/// through the tool name on their <c>GeneratedCodeAttribute</c>.
	/// </summary>
	static readonly ImmutableArray<string> s_frameworkGeneratorToolNames =
	[
		"TypeLibraryGenerator",
		"AttributeDataModelGenerator",
	];

	/// <summary>
	/// Assembly-level attributes that grant other assemblies access to a component's internals.
	/// </summary>
	static readonly ImmutableArray<string> s_internalsGrantAttributeFullNames =
	[
		"System.Runtime.CompilerServices.InternalsVisibleToAttribute",
		"System.Runtime.CompilerServices.IgnoresAccessChecksToAttribute",
	];

	static readonly ImmutableArray<string> s_roslynComponentAttributeFullNames =
	[
		"Microsoft.CodeAnalysis.GeneratorAttribute",
		"Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzerAttribute",
		"Microsoft.CodeAnalysis.CodeFixes.ExportCodeFixProviderAttribute",
		"Microsoft.CodeAnalysis.CodeRefactorings.ExportCodeRefactoringProviderAttribute",
	];

	static readonly ImmutableArray<string> s_roslynComponentInterfaceFullNames =
	[
		"Microsoft.CodeAnalysis.IIncrementalGenerator",
		"Microsoft.CodeAnalysis.ISourceGenerator",
		"Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer",
		"Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider",
		"Microsoft.CodeAnalysis.CodeRefactorings.CodeRefactoringProvider",
	];

	/// <summary>
	/// Reads every type full name (nested types included) declared by an assembly. The merge uses it
	/// to treat every type that came from the framework assembly as framework-owned, whatever
	/// namespace it lives in: the framework's public surface is not confined to
	/// <c>Purview.SourceGeneratorFramework</c> (for example the
	/// <c>Microsoft.CodeAnalysis.*Extensions</c> and <c>System.StringExtensions</c> extension
	/// classes), and ILRepack's internalize is best effort, so ownership cannot rely on the namespace
	/// alone.
	/// </summary>
	/// <param name="assemblyPath">The assembly to read type names from.</param>
	public static ImmutableArray<string> CollectTypeFullNames(string assemblyPath)
	{
		using var assembly = AssemblyDefinition.ReadAssembly(assemblyPath);

		return [.. assembly.MainModule.Types.SelectMany(MergeToolRunner.Flatten).Select(static type => type.FullName)];
	}

	/// <summary>
	/// Internalizes every framework-owned type in the merged component, strips the assembly-level
	/// internals grants the merge copied in, and returns a report of the changes plus anything that
	/// could not be internalized.
	/// </summary>
	/// <param name="assemblyPath">The merged component to rewrite in place.</param>
	/// <param name="searchDirectories">Assembly resolution paths for the merged component.</param>
	/// <param name="warn">Optional sink for non-blocking findings, such as component members that expose framework types.</param>
	/// <param name="ownedNamespaces">Namespace prefixes to internalize; defaults to the framework's own namespaces.</param>
	/// <param name="ownedTypeFullNames">Additional type full names to internalize (normally the framework assembly's own type names).</param>
	public static FrameworkInternalizationReport Apply(
		string assemblyPath,
		IEnumerable<string> searchDirectories,
		Action<string>? warn = null,
		ImmutableArray<string>? ownedNamespaces = null,
		ImmutableArray<string>? ownedTypeFullNames = null
	)
	{
		var namespaces = ownedNamespaces ?? DefaultOwnedNamespaces;
		var typeFullNames = ownedTypeFullNames ?? DefaultOwnedTypeFullNames;
		var hasSymbols = File.Exists(Path.ChangeExtension(assemblyPath, ".pdb"));

		using var resolver = MergeToolRunner.CreateResolver(searchDirectories);
		using var assembly = AssemblyDefinition.ReadAssembly(
			assemblyPath,
			new ReaderParameters
			{
				AssemblyResolver = resolver,
				ReadSymbols = hasSymbols,
				InMemory = true,
			}
		);

		var allTypes = assembly.MainModule.Types.SelectMany(MergeToolRunner.Flatten).ToList();

		var internalizedTypeCount = 0;
		foreach (var type in allTypes)
		{
			if (!IsOwned(type, namespaces, typeFullNames) || !IsPubliclyVisible(type))
				continue;

			// Roslyn ignores non-public components, so an internalized entry point would silently
			// stop running. Component types are always left alone.
			if (IsRoslynComponent(type))
				continue;

			MakeNonPublic(type);
			internalizedTypeCount++;
		}

		List<string> remainingPublicTypes = [];
		foreach (var type in allTypes)
		{
			// A framework type reachable only through a non-public declaring type is not part of the
			// public surface, so it cannot leak and must not fail the merge.
			if (IsOwned(type, namespaces, typeFullNames) && IsExternallyVisible(type) && !IsRoslynComponent(type))
				remainingPublicTypes.Add(type.FullName);
		}

		List<(string Owner, string Member)> exposingMembers = [];
		foreach (var type in allTypes)
		{
			// Framework types and types reachable only through a non-public declaring type are not
			// part of the component's public surface, so they cannot expose anything to a consumer.
			if (IsOwned(type, namespaces, typeFullNames) || !IsExternallyVisible(type))
				continue;

			CollectExposingMembers(type, namespaces, typeFullNames, exposingMembers);
		}

		var strippedGrantCount = StripInternalsGrants(assembly);

		if (internalizedTypeCount > 0 || strippedGrantCount > 0)
			assembly.Write(assemblyPath, new WriterParameters { WriteSymbols = hasSymbols });

		if (warn is not null)
		{
			// Group by declaring type: a component whose type library exposes framework types has many
			// such members, and one actionable line per type is more useful than one per member.
			foreach (var group in exposingMembers.GroupBy(static entry => entry.Owner, StringComparer.Ordinal))
			{
				var samples = group.Take(3).Select(static entry => entry.Member);
				warn(
					$"'{group.Key}' is public and exposes Purview.SourceGeneratorFramework types ({group.Count()} member(s), e.g. {string.Join(", ", samples)}). The exposed types were internalized in the merged component, so make the declaring type or its members non-public to keep the component's public surface self-contained."
				);
			}
		}

		return new(
			internalizedTypeCount,
			[.. remainingPublicTypes],
			[.. exposingMembers.Select(static entry => $"{entry.Owner}.{entry.Member}")],
			strippedGrantCount
		);
	}

	/// <summary>
	/// Removes the assembly-level internals grants the merge copied into the artifact. The merged
	/// component is a shipped analyzer, not the component's own assembly: a leftover grant would let
	/// an unrelated assembly (such as the framework's own test assemblies) reach the internalized
	/// framework types, while the component's bin output keeps its grants for in-repo tests.
	/// </summary>
	static int StripInternalsGrants(AssemblyDefinition assembly)
	{
		var removed = 0;
		for (var index = assembly.CustomAttributes.Count - 1; index >= 0; index--)
		{
			if (
				!s_internalsGrantAttributeFullNames.Contains(
					assembly.CustomAttributes[index].AttributeType.FullName,
					StringComparer.Ordinal
				)
			)
			{
				continue;
			}

			assembly.CustomAttributes.RemoveAt(index);
			removed++;
		}

		return removed;
	}

	static void CollectExposingMembers(
		TypeDefinition type,
		ImmutableArray<string> ownedNamespaces,
		ImmutableArray<string> ownedTypeFullNames,
		List<(string Owner, string Member)> exposingMembers
	)
	{
		if (ReferencesOwnedType(type.BaseType, ownedNamespaces, ownedTypeFullNames))
			exposingMembers.Add((type.FullName, $"base type {type.BaseType.FullName}"));

		foreach (var @interface in type.Interfaces)
		{
			if (ReferencesOwnedType(@interface.InterfaceType, ownedNamespaces, ownedTypeFullNames))
				exposingMembers.Add((type.FullName, $"interface {@interface.InterfaceType.FullName}"));
		}

		// A generic constraint is part of the public surface even though it is not a member.
		foreach (var genericParameter in type.GenericParameters)
		{
			if (ReferencesOwnedType(genericParameter, ownedNamespaces, ownedTypeFullNames))
				exposingMembers.Add((type.FullName, $"generic parameter {genericParameter.Name}"));
		}

		foreach (var field in type.Fields)
		{
			if (IsPubliclyVisible(field) && ReferencesOwnedType(field.FieldType, ownedNamespaces, ownedTypeFullNames))
				exposingMembers.Add((type.FullName, field.Name));
		}

		foreach (var property in type.Properties)
		{
			if (
				IsPubliclyVisible(property)
				&& (
					ReferencesOwnedType(property.PropertyType, ownedNamespaces, ownedTypeFullNames)
					|| property.Parameters.Any(parameter =>
						ReferencesOwnedType(parameter.ParameterType, ownedNamespaces, ownedTypeFullNames)
					)
				)
			)
			{
				exposingMembers.Add((type.FullName, property.Name));
			}
		}

		foreach (var @event in type.Events)
		{
			if (IsPubliclyVisible(@event) && ReferencesOwnedType(@event.EventType, ownedNamespaces, ownedTypeFullNames))
				exposingMembers.Add((type.FullName, @event.Name));
		}

		foreach (var method in type.Methods)
		{
			if (!IsPubliclyVisible(method))
				continue;

			if (
				ReferencesOwnedType(method.ReturnType, ownedNamespaces, ownedTypeFullNames)
				|| method.Parameters.Any(parameter =>
					ReferencesOwnedType(parameter.ParameterType, ownedNamespaces, ownedTypeFullNames)
				)
			)
			{
				exposingMembers.Add((type.FullName, method.Name));
			}

			foreach (var genericParameter in method.GenericParameters)
			{
				if (ReferencesOwnedType(genericParameter, ownedNamespaces, ownedTypeFullNames))
					exposingMembers.Add((type.FullName, $"{method.Name}<{genericParameter.Name}>"));
			}
		}
	}

	static bool ReferencesOwnedType(
		TypeReference? type,
		ImmutableArray<string> ownedNamespaces,
		ImmutableArray<string> ownedTypeFullNames
	)
	{
		while (type is not null)
		{
			switch (type)
			{
				case RequiredModifierType requiredModifier:
					return ReferencesOwnedType(requiredModifier.ModifierType, ownedNamespaces, ownedTypeFullNames)
						|| ReferencesOwnedType(requiredModifier.ElementType, ownedNamespaces, ownedTypeFullNames);
				case OptionalModifierType optionalModifier:
					return ReferencesOwnedType(optionalModifier.ModifierType, ownedNamespaces, ownedTypeFullNames)
						|| ReferencesOwnedType(optionalModifier.ElementType, ownedNamespaces, ownedTypeFullNames);
				case GenericInstanceType genericInstance:
					return ReferencesOwnedType(genericInstance.ElementType, ownedNamespaces, ownedTypeFullNames)
						|| genericInstance.GenericArguments.Any(argument =>
							ReferencesOwnedType(argument, ownedNamespaces, ownedTypeFullNames)
						);
				case FunctionPointerType functionPointer:
					return ReferencesOwnedType(functionPointer.ReturnType, ownedNamespaces, ownedTypeFullNames)
						|| functionPointer.Parameters.Any(parameter =>
							ReferencesOwnedType(parameter.ParameterType, ownedNamespaces, ownedTypeFullNames)
						);
				case GenericParameter genericParameter:
					return genericParameter.Constraints.Any(constraint =>
						ReferencesOwnedType(constraint.ConstraintType, ownedNamespaces, ownedTypeFullNames)
					);
				case TypeSpecification specification:
					type = specification.ElementType;
					continue;
				default:
					break;
			}

			if (ownedTypeFullNames.Contains(type.FullName, StringComparer.Ordinal))
				return true;

			// Mono.Cecil reports an empty namespace for nested types, so the check walks the declaring chain: a nested type is owned when any of its declaring types is.
			return IsOwnedNamespace(type.Namespace, ownedNamespaces);
		}

		return false;
	}

	/// <summary>
	/// Determines whether a type belongs to the framework, or was emitted into the component by the
	/// framework's generators. Mono.Cecil reports an empty namespace for nested types, so the check
	/// walks the declaring chain: a nested type is owned when any of its declaring types is.
	/// </summary>
	static bool IsOwned(
		TypeDefinition type,
		ImmutableArray<string> ownedNamespaces,
		ImmutableArray<string> ownedTypeFullNames
	)
	{
		for (var current = type; current is not null; current = current.DeclaringType)
		{
			if (
				ownedTypeFullNames.Contains(current.FullName, StringComparer.Ordinal)
				|| IsOwnedNamespace(current.Namespace, ownedNamespaces)
				|| IsFrameworkGenerated(current)
			)
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Detects a type the framework's generators emitted into the component (the generated type
	/// library, attribute data models). Those types are not part of the merged framework assembly, so
	/// ILRepack cannot internalize them; the tool name on the generated-code attribute is the only
	/// marker that survives the merge and identifies them.
	/// </summary>
	static bool IsFrameworkGenerated(TypeDefinition type)
	{
		foreach (var attribute in type.CustomAttributes)
		{
			if (
				!string.Equals(
					attribute.AttributeType.FullName,
					GeneratedCodeAttributeFullName,
					StringComparison.Ordinal
				)
				|| attribute.ConstructorArguments.Count == 0
				|| attribute.ConstructorArguments[0].Value is not string toolName
			)
			{
				continue;
			}

			if (s_frameworkGeneratorToolNames.Contains(toolName, StringComparer.Ordinal))
				return true;
		}

		return false;
	}

	static bool IsOwnedNamespace(string? @namespace, ImmutableArray<string> ownedNamespaces) =>
		@namespace is not null
		&& ownedNamespaces.Any(prefix =>
			@namespace.Equals(prefix, StringComparison.Ordinal)
			|| @namespace.StartsWith(prefix + ".", StringComparison.Ordinal)
		);

	/// <summary>
	/// Determines whether a type is reachable from outside the component. A nested type is reachable
	/// only when it, and every type that declares it, is public: a public nested type inside an
	/// internal type is not part of the component's public surface.
	/// </summary>
	static bool IsExternallyVisible(TypeDefinition type)
	{
		for (var current = type; current is not null; current = current.DeclaringType)
		{
			if (current.DeclaringType is null)
			{
				if (!current.IsPublic)
					return false;
			}
			else if (!current.IsNestedPublic)
			{
				return false;
			}
		}

		return true;
	}

	/// <summary>
	/// Detects Roslyn component entry points so they are never internalized. Roslyn discovers
	/// components through their attributes and only instantiates public types.
	/// </summary>
	static bool IsRoslynComponent(TypeDefinition type)
	{
		foreach (var attribute in type.CustomAttributes)
		{
			if (s_roslynComponentAttributeFullNames.Contains(attribute.AttributeType.FullName, StringComparer.Ordinal))
				return true;
		}

		for (var current = type; current is not null; current = Resolve(current.BaseType))
		{
			if (IsRoslynComponentType(current.BaseType))
				return true;

			foreach (var @interface in current.Interfaces)
			{
				if (IsRoslynComponentType(@interface.InterfaceType))
					return true;
			}
		}

		return false;
	}

	static bool IsRoslynComponentType(TypeReference? type)
	{
		if (type is null)
			return false;

		// The type may be a generic instance, so check the element type as well.
		return s_roslynComponentInterfaceFullNames.Contains(type.FullName, StringComparer.Ordinal)
			|| s_roslynComponentInterfaceFullNames.Contains(
				Resolve(type)?.FullName ?? string.Empty,
				StringComparer.Ordinal
			);
	}

	static TypeDefinition? Resolve(TypeReference? type)
	{
		try
		{
			return type?.Resolve();
		}
		catch (AssemblyResolutionException)
		{
			// A base type that cannot be resolved (for example a Roslyn interface resolved from a
			// different host) simply cannot be reported; name matching above already covered it.
			return null;
		}
	}

	static bool IsPubliclyVisible(TypeDefinition type) => type.IsNested ? type.IsNestedPublic : type.IsPublic;

	static bool IsPubliclyVisible(FieldDefinition field) =>
		field.IsPublic || field.IsFamily || field.IsFamilyOrAssembly;

	static bool IsPubliclyVisible(PropertyDefinition property) =>
		IsPubliclyVisible(property.GetMethod) || IsPubliclyVisible(property.SetMethod);

	static bool IsPubliclyVisible(EventDefinition @event) =>
		IsPubliclyVisible(@event.AddMethod) || IsPubliclyVisible(@event.RemoveMethod);

	static bool IsPubliclyVisible(MethodDefinition? method) =>
		method is not null && (method.IsPublic || method.IsFamily || method.IsFamilyOrAssembly);

	static void MakeNonPublic(TypeDefinition type) =>
		type.Attributes = type.IsNested
			? (type.Attributes & ~TypeAttributes.VisibilityMask) | TypeAttributes.NestedAssembly
			: (type.Attributes & ~TypeAttributes.VisibilityMask) | TypeAttributes.NotPublic;
}

/// <summary>
/// Describes a framework-type internalization pass over a merged component.
/// </summary>
/// <param name="InternalizedTypeCount">The number of types rewritten to non-public visibility.</param>
/// <param name="PublicFrameworkTypesRemaining">Framework-owned types that are still public; a non-empty value must fail the merge.</param>
/// <param name="PublicMembersExposingFrameworkTypes">Public members of non-framework types whose signature references a framework-owned type.</param>
/// <param name="StrippedInternalsGrantCount">The number of assembly-level internals grants removed from the merged artifact.</param>
sealed record FrameworkInternalizationReport(
	int InternalizedTypeCount,
	ImmutableArray<string> PublicFrameworkTypesRemaining,
	ImmutableArray<string> PublicMembersExposingFrameworkTypes,
	int StrippedInternalsGrantCount = 0
);
