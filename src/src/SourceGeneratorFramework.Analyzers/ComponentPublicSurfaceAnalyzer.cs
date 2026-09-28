using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Purview.SourceGeneratorFramework.Analyzers;

/// <summary>
/// Flags public members of a merged Roslyn component whose signature references a
/// <c>Purview.SourceGeneratorFramework</c> type. The merge internalizes every framework type in the
/// shipped analyzer, so such a member becomes a public signature over an internal type: unusable, and
/// the merge reports it (as <c>PSGFR41</c>) after a full build. This analyzer reports the same finding
/// while the author is editing, so the surface is fixed before the merge ever runs.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ComponentPublicSurfaceAnalyzer : DiagnosticAnalyzer
{
	public const string DiagnosticId = "PSGFR41";

	internal const string FrameworkAssemblyName = "Purview.SourceGeneratorFramework";

	public static readonly DiagnosticDescriptor Rule = new(
		DiagnosticId,
		"Component public surface exposes framework types",
		"'{0}' is public and exposes the Purview.SourceGeneratorFramework type '{1}', which the merge internalizes in the shipped analyzer; make the member or its declaring type non-public",
		"Purview.SourceGeneratorFramework",
		DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		description: "A merged Roslyn component internalizes every Purview.SourceGeneratorFramework type it ships, so a public member whose signature references one leaves a public signature over an internal type in the analyzer. The merge reports the same finding as PSGFR41."
	);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

	public override void Initialize(AnalysisContext context)
	{
		if (context is null)
			throw new ArgumentNullException(nameof(context));

		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterCompilationStartAction(startContext =>
		{
			// Only a component whose framework implementation is merged loses the framework types, so
			// only its public surface can be left unusable.
			if (!FrameworkMergeFacts.WillBeMerged(startContext.Options))
				return;

			if (GetFrameworkAssembly(startContext.Compilation) is not { } frameworkAssembly)
				return;

			startContext.RegisterSymbolAction(
				context => AnalyzeNamedType(context, frameworkAssembly),
				SymbolKind.NamedType
			);
		});
	}

	static void AnalyzeNamedType(SymbolAnalysisContext context, IAssemblySymbol frameworkAssembly)
	{
		if (context.Symbol is not INamedTypeSymbol type)
			return;

		// The framework's own types are what the merge internalizes; the component's are the surface.
		if (SymbolEqualityComparer.Default.Equals(type.ContainingAssembly, frameworkAssembly))
			return;

		// Generated code is not hand-editable: the merge internalizes generator-emitted type libraries
		// and attribute sets itself.
		if (IsGenerated(type))
			return;

		if (!RoslynComponentDiscovery.IsEffectivelyPublic(type))
			return;

		foreach (var (symbol, frameworkType) in FindExposingMembers(type, frameworkAssembly))
		{
			if (symbol.Locations.FirstOrDefault(static location => location.IsInSource) is not { } location)
				continue;

			context.ReportDiagnostic(
				Diagnostic.Create(
					Rule,
					location,
					symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
					frameworkType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)
				)
			);
		}
	}

	static IAssemblySymbol? GetFrameworkAssembly(Compilation compilation)
	{
		foreach (var reference in compilation.References)
		{
			if (
				compilation.GetAssemblyOrModuleSymbol(reference) is IAssemblySymbol assembly
				&& string.Equals(assembly.Identity.Name, FrameworkAssemblyName, StringComparison.OrdinalIgnoreCase)
			)
			{
				return assembly;
			}
		}

		return null;
	}

	/// <summary>
	/// Enumerates the parts of the type's public surface that reference a framework type: the base type,
	/// the implemented interfaces, the generic constraints, and the public or protected members.
	/// </summary>
	static IEnumerable<(ISymbol Symbol, ITypeSymbol FrameworkType)> FindExposingMembers(
		INamedTypeSymbol type,
		IAssemblySymbol frameworkAssembly
	)
	{
		if (FindFrameworkType(type.BaseType, frameworkAssembly) is { } fromBase)
			yield return (type, fromBase);

		foreach (var @interface in type.Interfaces)
		{
			if (FindFrameworkType(@interface, frameworkAssembly) is { } fromInterface)
				yield return (type, fromInterface);
		}

		foreach (var typeParameter in type.TypeParameters)
		{
			if (FindFrameworkType(typeParameter, frameworkAssembly) is { } fromConstraint)
				yield return (typeParameter, fromConstraint);
		}

		foreach (var member in type.GetMembers())
		{
			if (!IsPublicSurfaceMember(member) || IsGenerated(member))
				continue;

			if (FindExposedType(member, frameworkAssembly) is { } fromMember)
				yield return (member, fromMember);
		}
	}

	static ITypeSymbol? FindExposedType(ISymbol member, IAssemblySymbol frameworkAssembly) =>
		member switch
		{
			IFieldSymbol field => FindFrameworkType(field.Type, frameworkAssembly),
			IPropertySymbol property => FindFrameworkType(property.Type, frameworkAssembly)
				?? FirstFrameworkType(property.Parameters, frameworkAssembly),
			IEventSymbol @event => FindFrameworkType(@event.Type, frameworkAssembly),
			IMethodSymbol method => FindFrameworkType(method.ReturnType, frameworkAssembly)
				?? FirstFrameworkType(method.Parameters, frameworkAssembly)
				?? method
					.TypeParameters.Select(typeParameter => FindFrameworkType(typeParameter, frameworkAssembly))
					.FirstOrDefault(static type => type is not null),
			_ => null,
		};

	static ITypeSymbol? FirstFrameworkType(
		ImmutableArray<IParameterSymbol> parameters,
		IAssemblySymbol frameworkAssembly
	) =>
		parameters
			.Select(parameter => FindFrameworkType(parameter.Type, frameworkAssembly))
			.FirstOrDefault(static type => type is not null);

	/// <summary>
	/// Returns the framework type a signature references, if any: the type itself, one of its generic
	/// arguments, an array element type, or a generic constraint.
	/// </summary>
	static ITypeSymbol? FindFrameworkType(ITypeSymbol? type, IAssemblySymbol frameworkAssembly)
	{
		switch (type)
		{
			case null:
				return null;
			case IArrayTypeSymbol array:
				return FindFrameworkType(array.ElementType, frameworkAssembly);
			case IPointerTypeSymbol pointer:
				return FindFrameworkType(pointer.PointedAtType, frameworkAssembly);
			case ITypeParameterSymbol typeParameter:
				foreach (var constraint in typeParameter.ConstraintTypes)
				{
					if (FindFrameworkType(constraint, frameworkAssembly) is { } fromConstraint)
						return fromConstraint;
				}

				return null;
			case INamedTypeSymbol named:
				if (IsFrameworkType(named, frameworkAssembly))
					return named;

				foreach (var argument in named.TypeArguments)
				{
					if (FindFrameworkType(argument, frameworkAssembly) is { } fromArgument)
						return fromArgument;
				}

				return null;
			default:
				return IsFrameworkType(type, frameworkAssembly) ? type : null;
		}
	}

	static bool IsFrameworkType(ITypeSymbol type, IAssemblySymbol frameworkAssembly) =>
		SymbolEqualityComparer.Default.Equals(type.ContainingAssembly, frameworkAssembly);

	/// <summary>
	/// Detects a type the framework's generators emitted into the component (the generated type library,
	/// attribute data models, marker attribute sets). Those members are not hand-editable, and the merge
	/// internalizes them itself.
	/// </summary>
	static bool IsGenerated(ISymbol symbol)
	{
		for (var current = symbol; current is not null; current = current.ContainingType)
		{
			foreach (var attribute in current.GetAttributes())
			{
				var attributeName = attribute.AttributeClass?.ToDisplayString();

				if (
					string.Equals(
						attributeName,
						"System.CodeDom.Compiler.GeneratedCodeAttribute",
						StringComparison.Ordinal
					)
					|| string.Equals(
						attributeName,
						"System.Runtime.CompilerServices.CompilerGeneratedAttribute",
						StringComparison.Ordinal
					)
				)
				{
					return true;
				}
			}
		}

		return false;
	}

	static bool IsPublicSurfaceMember(ISymbol member)
	{
		if (member.IsImplicitlyDeclared)
			return false;

		if (
			member.DeclaredAccessibility
			is not (Accessibility.Public or Accessibility.Protected or Accessibility.ProtectedOrInternal)
		)
		{
			return false;
		}

		// Property and event accessors are reported through their property or event.
		return member switch
		{
			IFieldSymbol or IPropertySymbol or IEventSymbol => true,
			IMethodSymbol method => method.MethodKind
				is MethodKind.Ordinary
					or MethodKind.Constructor
					or MethodKind.UserDefinedOperator
					or MethodKind.Conversion,
			_ => false,
		};
	}
}
