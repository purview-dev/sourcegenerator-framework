using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Purview.SourceGeneratorFramework.Analyzers;

/// <summary>
/// Reports attribute-data model naming diagnostics (ADM0011-ADM0015): mapped constructor parameter and named
/// argument names that the generated code would never find on the attribute, property names that collide with
/// generated members, and <c>[NestedModel]</c> mappings that do not line up with the outer attribute.
/// </summary>
/// <remarks>
/// The generated code looks names up case-sensitively at run time and falls back to a default when a name is
/// missing, so a naming mistake compiles cleanly and silently produces default values. These rules mirror the
/// generator's source resolution so the mistake is reported where the mapping is declared.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AttributeDataModelNamingAnalyzer : DiagnosticAnalyzer
{
	const string ConstructorParameterKind = "constructor parameter";
	const string NamedArgumentKind = "settable property or field";

	public static DiagnosticDescriptor MappedNameCaseMismatch =>
		AttributeDataModelDiagnosticRules.MappedNameCaseMismatch;

	public static DiagnosticDescriptor MappedMemberNotFound => AttributeDataModelDiagnosticRules.MappedMemberNotFound;

	public static DiagnosticDescriptor NestedModelMemberNotDeclared =>
		AttributeDataModelDiagnosticRules.NestedModelMemberNotDeclared;

	public static DiagnosticDescriptor PropertyNameConflictsWithGeneratedMember =>
		AttributeDataModelDiagnosticRules.PropertyNameConflictsWithGeneratedMember;

	public static DiagnosticDescriptor NestedModelConstructorIndexMismatch =>
		AttributeDataModelDiagnosticRules.NestedModelConstructorIndexMismatch;

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
		[
			MappedNameCaseMismatch,
			MappedMemberNotFound,
			NestedModelMemberNotDeclared,
			PropertyNameConflictsWithGeneratedMember,
			NestedModelConstructorIndexMismatch,
		];

	public override void Initialize(AnalysisContext context)
	{
		if (context is null)
			throw new ArgumentNullException(nameof(context));

		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();

		context.RegisterCompilationStartAction(context =>
		{
			var markers = MarkerTypes.Resolve(context.Compilation);
			if (markers is null)
				return;

			context.RegisterSymbolAction(context => AnalyzeNamedType(context, markers.Value), SymbolKind.NamedType);
		});
	}

	static void AnalyzeNamedType(SymbolAnalysisContext context, MarkerTypes markers)
	{
		if (context.Symbol is not INamedTypeSymbol { TypeKind: TypeKind.Struct } model)
			return;

		var generateAttribute = GetAttribute(model, markers.Generate);
		if (generateAttribute is null)
			return;

		var targetAttribute = ResolveTargetAttribute(context.Compilation, generateAttribute);

		foreach (var property in ReadProperties(model, markers))
		{
			if (property.Parameter.Locations.FirstOrDefault(static l => l.IsInSource) is not { } location)
				continue;

			if (AttributeDataModelDiagnosticRules.GeneratedMemberNames.Contains(property.PropertyName))
			{
				context.ReportDiagnostic(
					Diagnostic.Create(PropertyNameConflictsWithGeneratedMember, location, property.PropertyName)
				);
			}

			if (targetAttribute is null)
				continue;

			if (property.NestedModel is not null)
			{
				AnalyzeNestedModel(
					context,
					location,
					property.NestedModel,
					targetAttribute,
					markers,
					ImmutableHashSet.Create<INamedTypeSymbol>(SymbolEqualityComparer.Default, model)
				);
				continue;
			}

			foreach (var source in property.Sources)
				AnalyzeDirectSource(context, location, property, source, targetAttribute);
		}
	}

	static void AnalyzeDirectSource(
		SymbolAnalysisContext context,
		Location location,
		MappedProperty property,
		MappedSource source,
		INamedTypeSymbol targetAttribute
	)
	{
		// An explicit [Argument(index)] or [Argument("name")] that matches nothing is ADM0003, reported by the
		// validation analyzer. Only the case-only mismatch it accepts, and the names this analyzer infers, are
		// checked here.
		if (source.Kind == MappedSourceKind.ConstructorIndex)
			return;

		var (kind, actualName) = Match(source, targetAttribute);
		if (kind == MatchKind.Exact)
			return;

		if (kind == MatchKind.CaseOnly)
		{
			context.ReportDiagnostic(
				Diagnostic.Create(
					MappedNameCaseMismatch,
					location,
					source.Name,
					KindText(source),
					actualName,
					targetAttribute.Name
				)
			);
			return;
		}

		if (source.Kind == MappedSourceKind.ConstructorName && !source.IsImplied)
			return;

		context.ReportDiagnostic(
			Diagnostic.Create(
				MappedMemberNotFound,
				location,
				property.PropertyName,
				KindText(source),
				source.Name,
				targetAttribute.Name
			)
		);
	}

	static void AnalyzeNestedModel(
		SymbolAnalysisContext context,
		Location location,
		INamedTypeSymbol nestedModel,
		INamedTypeSymbol outerAttribute,
		MarkerTypes markers,
		ImmutableHashSet<INamedTypeSymbol> visited
	)
	{
		if (visited.Contains(nestedModel))
			return;

		visited = visited.Add(nestedModel);

		var nestedGenerate = GetAttribute(nestedModel, markers.Generate);
		var nestedTarget = nestedGenerate is null ? null : ResolveTargetAttribute(context.Compilation, nestedGenerate);

		foreach (var property in ReadProperties(nestedModel, markers))
		{
			// The nested model's own nested models are read from the same outer attribute.
			if (property.NestedModel is not null)
			{
				AnalyzeNestedModel(context, location, property.NestedModel, outerAttribute, markers, visited);
				continue;
			}

			var resolved = false;
			var reported = false;
			foreach (var source in property.Sources)
			{
				if (source.Kind == MappedSourceKind.ConstructorIndex)
				{
					var outerNames = ParameterNamesAt(outerAttribute, source.Index);
					if (outerNames.IsEmpty)
						continue;

					var nestedNames = nestedTarget is null ? [] : ParameterNamesAt(nestedTarget, source.Index);
					if (!nestedNames.IsEmpty && !outerNames.Overlaps(nestedNames))
					{
						context.ReportDiagnostic(
							Diagnostic.Create(
								NestedModelConstructorIndexMismatch,
								location,
								nestedModel.Name,
								property.PropertyName,
								source.Index,
								nestedNames.First(),
								nestedTarget!.Name,
								outerNames.First(),
								outerAttribute.Name
							)
						);
						reported = true;
						continue;
					}

					resolved = true;
					continue;
				}

				var (kind, actualName) = Match(source, outerAttribute);
				if (kind == MatchKind.Exact)
				{
					resolved = true;
				}
				else if (kind == MatchKind.CaseOnly)
				{
					context.ReportDiagnostic(
						Diagnostic.Create(
							MappedNameCaseMismatch,
							location,
							source.Name,
							KindText(source),
							actualName,
							outerAttribute.Name
						)
					);
					reported = true;
				}
			}

			if (!resolved && !reported && !property.Sources.IsEmpty)
			{
				context.ReportDiagnostic(
					Diagnostic.Create(
						NestedModelMemberNotDeclared,
						location,
						nestedModel.Name,
						property.PropertyName,
						DescribeSources(property.Sources),
						outerAttribute.Name
					)
				);
			}
		}
	}

	static string KindText(MappedSource source) =>
		source.Kind == MappedSourceKind.NamedArgument ? NamedArgumentKind : ConstructorParameterKind;

	static string DescribeSources(ImmutableArray<MappedSource> sources)
	{
		StringBuilder builder = new();
		for (var i = 0; i < sources.Length; i++)
		{
			if (i > 0)
				builder.Append(i == sources.Length - 1 ? " or " : ", ");

			var source = sources[i];
			if (source.Kind == MappedSourceKind.ConstructorIndex)
				builder.Append("constructor index ").Append(source.Index);
			else
				builder.Append(KindText(source)).Append(" '").Append(source.Name).Append('\'');
		}

		return builder.ToString();
	}

	static (MatchKind Kind, string? ActualName) Match(MappedSource source, INamedTypeSymbol attribute)
	{
		var candidates =
			source.Kind == MappedSourceKind.ConstructorName
				? attribute.InstanceConstructors.SelectMany(static c => c.Parameters).Select(static p => p.Name)
				: SettableMemberNames(attribute);

		string? caseOnly = null;
		foreach (var candidate in candidates)
		{
			if (string.Equals(candidate, source.Name, StringComparison.Ordinal))
				return (MatchKind.Exact, candidate);

			if (caseOnly is null && string.Equals(candidate, source.Name, StringComparison.OrdinalIgnoreCase))
				caseOnly = candidate;
		}

		return caseOnly is null ? (MatchKind.None, null) : (MatchKind.CaseOnly, caseOnly);
	}

	// A named argument can only set a public, writable instance property or field, including inherited ones.
	static IEnumerable<string> SettableMemberNames(INamedTypeSymbol attribute)
	{
		for (var type = attribute; type is not null; type = type.BaseType)
		{
			foreach (var member in type.GetMembers())
			{
				if (member.IsStatic || member.DeclaredAccessibility != Accessibility.Public)
					continue;

				var settable = member switch
				{
					IPropertySymbol property => property.SetMethod is { DeclaredAccessibility: Accessibility.Public },
					IFieldSymbol field => !field.IsReadOnly && !field.IsConst,
					_ => false,
				};

				if (settable)
					yield return member.Name;
			}
		}
	}

	static ImmutableHashSet<string> ParameterNamesAt(INamedTypeSymbol attribute, int index) =>
		attribute
			.InstanceConstructors.Where(c => index < c.Parameters.Length)
			.Select(c => c.Parameters[index].Name)
			.ToImmutableHashSet(StringComparer.Ordinal);

	/// <summary>
	/// Reads a model's mapped properties the way <c>AttributeDataModelLibrary</c> does: from the primary
	/// constructor's parameters, skipping <c>[Exclude]</c> and <c>[GenericTypeArgument]</c>.
	/// </summary>
	static IEnumerable<MappedProperty> ReadProperties(INamedTypeSymbol model, MarkerTypes markers)
	{
		var constructor = model.InstanceConstructors.FirstOrDefault(static c =>
			c.DeclaringSyntaxReferences.Any(static r => r.GetSyntax() is TypeDeclarationSyntax)
		);
		if (constructor is null)
			yield break;

		foreach (var parameter in constructor.Parameters)
		{
			if (
				GetAttribute(parameter, markers.Exclude) is not null
				|| GetAttribute(parameter, markers.GenericTypeArgument) is not null
			)
				continue;

			var propertyName = ToPascalCase(parameter.Name);

			if (GetAttribute(parameter, markers.NestedModel) is not null)
			{
				var nestedModel =
					parameter.Type is INamedTypeSymbol named && GetAttribute(named, markers.Generate) is not null
						? named
						: null;

				// A [NestedModel] type without [Generate] is ADM0004; there is nothing to compare.
				if (nestedModel is not null)
					yield return new(parameter, propertyName, [], nestedModel);

				continue;
			}

			var sources = ImmutableArray.CreateBuilder<MappedSource>();

			// The named argument is read first, matching the generator's source order.
			var propertyAttribute = GetAttribute(parameter, markers.Property);
			if (propertyAttribute is not null)
			{
				var name = GetNamedArgument<string>(propertyAttribute, "Name");
				sources.Add(new(MappedSourceKind.NamedArgument, name ?? propertyName, -1, IsImplied: name is null));
			}

			var argumentAttribute = GetAttribute(parameter, markers.Argument);
			if (argumentAttribute is not null)
			{
				var first = argumentAttribute.ConstructorArguments.FirstOrDefault().Value;
				var name = first as string ?? GetNamedArgument<string>(argumentAttribute, "Name");
				var index = first is int i ? i : GetNamedArgument<int?>(argumentAttribute, "Index") ?? -1;

				if (name is not null)
					sources.Add(new(MappedSourceKind.ConstructorName, name, -1, IsImplied: false));
				else if (index >= 0)
					sources.Add(new(MappedSourceKind.ConstructorIndex, null, index, IsImplied: false));
				else
					sources.Add(new(MappedSourceKind.ConstructorName, ToCamelCase(propertyName), -1, IsImplied: true));
			}

			if (sources.Count == 0)
				sources.Add(new(MappedSourceKind.NamedArgument, propertyName, -1, IsImplied: true));

			yield return new(parameter, propertyName, sources.ToImmutable(), null);
		}
	}

	static INamedTypeSymbol? ResolveTargetAttribute(Compilation compilation, AttributeData generateAttribute)
	{
		if (generateAttribute.ConstructorArguments.Length == 0)
			return null;

		// The generator accepts either a type or a string, but the string is resolved to a type at compile time. The
		// analyzer only needs the type to read its members, so it does not report a missing type.
		return generateAttribute.ConstructorArguments[0].Value switch
		{
			INamedTypeSymbol type => type.IsUnboundGenericType ? type.OriginalDefinition : type,
			string name => compilation.GetTypeByMetadataName(name),
			_ => null,
		};
	}

	static AttributeData? GetAttribute(ISymbol symbol, INamedTypeSymbol? attributeType)
	{
		if (attributeType is null)
			return null;

		foreach (var attribute in symbol.GetAttributes())
		{
			if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeType))
				return attribute;
		}

		return null;
	}

	static T? GetNamedArgument<T>(AttributeData attributeData, string name)
	{
		foreach (var argument in attributeData.NamedArguments)
		{
			if (argument.Key == name && argument.Value.Value is T value)
				return value;
		}

		return default;
	}

	// Must match AttributeDataModelLibrary.ToPascalCase: the model property is named from the parameter.
	static string ToPascalCase(string value)
	{
		StringBuilder builder = new(value.Length);
		var newWord = true;
		foreach (var c in value)
		{
			if (c == '_')
			{
				newWord = true;
				continue;
			}

			builder.Append(newWord ? char.ToUpperInvariant(c) : c);
			newWord = false;
		}

		return builder.ToString();
	}

	static string ToCamelCase(string value) =>
		value.Length == 0 ? value : char.ToLowerInvariant(value[0]) + value.Substring(1);

	enum MappedSourceKind
	{
		NamedArgument,
		ConstructorName,
		ConstructorIndex,
	}

	enum MatchKind
	{
		None,
		CaseOnly,
		Exact,
	}

	readonly record struct MappedSource(MappedSourceKind Kind, string? Name, int Index, bool IsImplied);

	sealed record MappedProperty(
		IParameterSymbol Parameter,
		string PropertyName,
		ImmutableArray<MappedSource> Sources,
		INamedTypeSymbol? NestedModel
	);

	readonly record struct MarkerTypes(
		INamedTypeSymbol Generate,
		INamedTypeSymbol? Property,
		INamedTypeSymbol? Argument,
		INamedTypeSymbol? NestedModel,
		INamedTypeSymbol? Exclude,
		INamedTypeSymbol? GenericTypeArgument
	)
	{
		const string Namespace = "Purview.SourceGeneratorFramework.Generators.";

		public static MarkerTypes? Resolve(Compilation compilation)
		{
			var generate = compilation.GetTypeByMetadataName(Namespace + "GenerateAttribute");
			if (generate is null)
				return null;

			// The other marker attributes are optional: the generator accepts the default behavior when they are missing, and
			// the analyzer does not report a diagnostic when they are missing.
			return new(
				generate,
				compilation.GetTypeByMetadataName(Namespace + "PropertyAttribute"),
				compilation.GetTypeByMetadataName(Namespace + "ArgumentAttribute"),
				compilation.GetTypeByMetadataName(Namespace + "NestedModelAttribute"),
				compilation.GetTypeByMetadataName(Namespace + "ExcludeAttribute"),
				compilation.GetTypeByMetadataName(Namespace + "GenericTypeArgumentAttribute")
			);
		}
	}
}
