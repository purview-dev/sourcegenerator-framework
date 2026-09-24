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
	/// Internalizes every framework-owned type in the merged component and returns a report of the
	/// changes plus anything that could not be internalized.
	/// </summary>
	/// <param name="assemblyPath">The merged component to rewrite in place.</param>
	/// <param name="searchDirectories">Assembly resolution paths for the merged component.</param>
	/// <param name="warn">Optional sink for non-blocking findings, such as component members that expose framework types.</param>
	/// <param name="ownedNamespaces">Namespace prefixes to internalize; defaults to the framework's own namespaces.</param>
	/// <param name="ownedTypeFullNames">Additional type full names to internalize.</param>
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
			if (IsOwned(type, namespaces, typeFullNames) && IsPubliclyVisible(type) && !IsRoslynComponent(type))
				remainingPublicTypes.Add(type.FullName);
		}

		List<string> exposingMembers = [];
		foreach (var type in allTypes)
		{
			if (IsOwned(type, namespaces, typeFullNames) || !IsPubliclyVisible(type))
				continue;

			CollectExposingMembers(type, namespaces, typeFullNames, exposingMembers);
		}

		if (internalizedTypeCount > 0)
			assembly.Write(assemblyPath, new WriterParameters { WriteSymbols = hasSymbols });

		if (warn is not null)
		{
			// Group by declaring type: a component whose type library exposes framework types has many
			// such members, and one actionable line per type is more useful than one per member.
			foreach (var group in exposingMembers.GroupBy(ExposingMemberOwner, StringComparer.Ordinal))
			{
				var samples = group.Take(3).Select(ExposingMemberName);
				warn(
					$"'{group.Key}' is public and exposes Purview.SourceGeneratorFramework types ({group.Count()} member(s), e.g. {string.Join(", ", samples)}). The exposed types were internalized in the merged component, so make the declaring type or its members non-public to keep the component's public surface self-contained."
				);
			}
		}

		return new(internalizedTypeCount, [.. remainingPublicTypes], [.. exposingMembers]);
	}

	static string ExposingMemberOwner(string member)
	{
		var separator = member.LastIndexOf('.');
		return separator <= 0 ? member : member[..separator];
	}

	static string ExposingMemberName(string member)
	{
		var separator = member.LastIndexOf('.');
		return separator <= 0 ? member : member[(separator + 1)..];
	}

	static void CollectExposingMembers(
		TypeDefinition type,
		ImmutableArray<string> ownedNamespaces,
		ImmutableArray<string> ownedTypeFullNames,
		List<string> exposingMembers
	)
	{
		if (ReferencesOwnedType(type.BaseType, ownedNamespaces, ownedTypeFullNames))
			exposingMembers.Add(type.FullName);

		foreach (var @interface in type.Interfaces)
		{
			if (ReferencesOwnedType(@interface.InterfaceType, ownedNamespaces, ownedTypeFullNames))
				exposingMembers.Add($"{type.FullName} : {@interface.InterfaceType.FullName}");
		}

		foreach (var field in type.Fields)
		{
			if (IsPubliclyVisible(field) && ReferencesOwnedType(field.FieldType, ownedNamespaces, ownedTypeFullNames))
				exposingMembers.Add($"{type.FullName}.{field.Name}");
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
				exposingMembers.Add($"{type.FullName}.{property.Name}");
			}
		}

		foreach (var @event in type.Events)
		{
			if (IsPubliclyVisible(@event) && ReferencesOwnedType(@event.EventType, ownedNamespaces, ownedTypeFullNames))
				exposingMembers.Add($"{type.FullName}.{@event.Name}");
		}

		foreach (var method in type.Methods)
		{
			if (
				!IsPubliclyVisible(method)
				|| (
					!ReferencesOwnedType(method.ReturnType, ownedNamespaces, ownedTypeFullNames)
					&& !method.Parameters.Any(parameter =>
						ReferencesOwnedType(parameter.ParameterType, ownedNamespaces, ownedTypeFullNames)
					)
				)
			)
			{
				continue;
			}

			exposingMembers.Add($"{type.FullName}.{method.Name}");
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

			return IsOwnedNamespace(type.Namespace, ownedNamespaces);
		}

		return false;
	}

	static bool IsOwned(
		TypeDefinition type,
		ImmutableArray<string> ownedNamespaces,
		ImmutableArray<string> ownedTypeFullNames
	) =>
		ownedTypeFullNames.Contains(type.FullName, StringComparer.Ordinal)
		|| IsOwnedNamespace(type.Namespace, ownedNamespaces);

	static bool IsOwnedNamespace(string? @namespace, ImmutableArray<string> ownedNamespaces) =>
		@namespace is not null
		&& ownedNamespaces.Any(prefix =>
			@namespace.Equals(prefix, StringComparison.Ordinal)
			|| @namespace.StartsWith(prefix + ".", StringComparison.Ordinal)
		);

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
sealed record FrameworkInternalizationReport(
	int InternalizedTypeCount,
	ImmutableArray<string> PublicFrameworkTypesRemaining,
	ImmutableArray<string> PublicMembersExposingFrameworkTypes
);
