using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Purview.SourceGeneratorFramework.Generators.Helpers;

static class AttributeDataModelLibrary
{
	public static IncrementalValuesProvider<GeneratorResult<AttributeDataModelTarget>> GetAttributeTargetPipeline(
		IncrementalGeneratorInitializationContext context
	)
	{
		// A [Generate] target may be a reference to a generated type-library full-name constant
		// (TypeLibrary.{Namespace}.{Member}FullName). The TypeLibrary class itself is emitted by
		// TypeLibraryGenerator through RegisterSourceOutput (the main pipeline), so it is not part of the
		// compilation this generator's ForAttributeWithMetadataName pipeline sees. The [GenerateTypeLibrary]
		// specs are source (and resolvable), so their ClassName and [TypeRef] members are discovered here and
		// used to guard and validate the reconstruction of the target from the argument's member-access
		// expression (see BuildTarget).
		var typeLibrarySpecData = context
			.SyntaxProvider.ForAttributeWithMetadataName(
				GeneratorTypeLibrary.Attirbutes.GenerateTypeLibraryAttribute.MetadataFullName,
				transform: static (ctx, _) => (INamedTypeSymbol)ctx.TargetSymbol,
				predicate: static (ctx, _) => ctx is ClassDeclarationSyntax
			)
			.Select(static (specSymbol, _) => ReadTypeLibrarySpecData(specSymbol))
			.Collect()
			.Select(static (specs, _) => new EquatableArray<TypeLibrarySpecData>(specs))
			.WithTrackingName("GetTypeLibrarySpecClassNames");

		var attributeCandidates = IncrementalPipeline.ForAttributeWithMetadataName(
			context,
			GeneratorTypeLibrary.Attirbutes.GenerateAttribute,
			// ForAttributeWithMetadataName already resolves TargetSymbol; calling
			// SemanticModel.GetDeclaredSymbol again would re-run the same symbol resolution on every
			// pipeline rerun, so the pre-resolved symbol is carried through as the intermediate value.
			static (ctx, _) => (INamedTypeSymbol)ctx.TargetSymbol,
			predicate: static (ctx, _) => ctx is StructDeclarationSyntax or RecordDeclarationSyntax
		);

		return attributeCandidates
			.CombineWith(
				typeLibrarySpecData,
				static (structSymbol, specData, _) => (Symbol: structSymbol, Specs: specData)
			)
			.CombineWith(
				context.CompilationProvider,
				static (pair, compilation, ct) => BuildTarget(pair.Symbol, pair.Specs, compilation, ct)
			)
			.WithTrackingName("GetAttributeDataTargets");
	}

	static GeneratorResult<AttributeDataModelTarget> BuildTarget(
		INamedTypeSymbol structSymbol,
		EquatableArray<TypeLibrarySpecData> typeLibrarySpecData,
		Compilation compilation,
		CancellationToken cancellationToken
	)
	{
		// Validation diagnostics (ADM0001-ADM0010) are reported by AttributeDataModelValidationAnalyzer and
		// AttributeDataModelSymbolPropertyAnalyzer; the generator carries them on the result so it can gate
		// generation on ShouldProcess without emitting the diagnostics itself.
		List<ReportableDiagnostic> diagnostics = [];
		var generateAttribute = GetAttribute(structSymbol, GeneratorTypeLibrary.Attirbutes.GenerateAttribute);
		if (generateAttribute is null)
			return GeneratorResult<AttributeDataModelTarget>.Empty;

		ITypeSymbol? targetAttributeType = null;
		TypeIdentity targetAttribute = default;
		var targetFromStringForm = false;
		if (generateAttribute.ConstructorArguments.Length == 0)
		{
			// An unresolved generated-const argument (TypeLibrary.{Namespace}.{Member}FullName) can leave the
			// attribute unbound to a constructor (ConstructorArguments empty, AttributeConstructor null), so the
			// TypeLibrary-path reconstruction is attempted before reporting ADM0001.
			if (
				!TryResolveTypeLibraryTarget(
					generateAttribute,
					typeLibrarySpecData,
					compilation,
					cancellationToken,
					out var unresolvedTargetType,
					out var unresolvedTargetIdentity
				)
			)
			{
				diagnostics.Add(
					ReportableDiagnostic.Create(
						AttributeDataModelDiagnosticRules.TargetAttributeNotResolved,
						isBlocking: true,
						structSymbol,
						structSymbol.Name
					)
				);
			}
			else
			{
				targetAttributeType = unresolvedTargetType;
				targetAttribute = unresolvedTargetIdentity;
			}
		}
		else
		{
			var firstArgument = generateAttribute.ConstructorArguments[0].Value;

			if (firstArgument is ITypeSymbol typeSymbol)
			{
				targetAttributeType = typeSymbol;
				targetAttribute = new(typeSymbol);
			}
			else if (firstArgument is string targetAttributeName)
			{
				// A literal string target. Resolve it against the compilation so enum defaults can be
				// derived from the target attribute's members; ADM0007 still requires the typeof form.
				targetFromStringForm = true;
				targetAttribute = ParseTypeIdentity(targetAttributeName);
				targetAttributeType = compilation.GetTypeByMetadataName(targetAttribute.MetadataFullName);
			}
			else if (
				TryResolveTypeLibraryTarget(
					generateAttribute,
					typeLibrarySpecData,
					compilation,
					cancellationToken,
					out var resolvedType,
					out var resolvedIdentity
				)
			)
			{
				// The argument references a {Member}FullName constant generated by TypeLibraryGenerator's
				// main pipeline, so its value is an error constant in this compilation. The target full name
				// is reassembled from the argument's member-access expression and resolved here.
				targetAttributeType = resolvedType;
				targetAttribute = resolvedIdentity;
			}
			else
			{
				diagnostics.Add(
					ReportableDiagnostic.Create(
						AttributeDataModelDiagnosticRules.TargetAttributeNotResolved,
						isBlocking: true,
						structSymbol,
						structSymbol.Name
					)
				);
			}
		}

		var matchByInheritance = GetNamedArgument(
			generateAttribute,
			"MatchByInheritance",
			GetConstructorArgument(generateAttribute, 1, false)
		);
		var autoDiscover = GetNamedArgument(
			generateAttribute,
			"AutoDiscover",
			GetConstructorArgument(generateAttribute, 2, false)
		);

		if (autoDiscover && (targetFromStringForm || targetAttributeType is null))
		{
			diagnostics.Add(
				ReportableDiagnostic.Create(
					AttributeDataModelDiagnosticRules.AutoDiscoverRequiresType,
					isBlocking: true,
					structSymbol,
					structSymbol.Name
				)
			);
		}

		HashSet<string> excludedNames = new(StringComparer.Ordinal);
		var explicitProperties = ReadExplicitProperties(
			structSymbol,
			excludedNames,
			diagnostics,
			targetAttributeType as INamedTypeSymbol,
			cancellationToken
		);
		var discoveredProperties =
			autoDiscover && targetAttributeType is not null
				? DiscoverProperties(
					targetAttributeType,
					explicitProperties,
					excludedNames,
					diagnostics,
					cancellationToken
				)
				: [];

		var mergedProperties = MergeProperties(explicitProperties, discoveredProperties);

		if (diagnostics.Any(d => d.IsBlocking))
			return GeneratorResult<AttributeDataModelTarget>.Create([.. diagnostics]);

		AttributeDataModelTarget target = new(
			Namespace: structSymbol.ContainingNamespace.IsGlobalNamespace
				? null
				: structSymbol.ContainingNamespace.ToDisplayString(),
			StructName: structSymbol.Name,
			Accessibility: structSymbol.DeclaredAccessibility.ToTypeDeclarationAccessibility(),
			IsRecord: structSymbol.IsRecord,
			IsReadOnly: structSymbol.IsReadOnly,
			TargetAttribute: targetAttribute,
			MatchByInheritance: matchByInheritance,
			AutoDiscover: autoDiscover,
			PrimaryConstructorArguments: GetPrimaryConstructorArguments(
				structSymbol,
				explicitProperties,
				cancellationToken
			),
			Properties: new EquatableArray<AttributeDataModelProperty>(mergedProperties)
		);

		return GeneratorResult<AttributeDataModelTarget>.Create(target, ImmutableArray.CreateRange(diagnostics));
	}

	static EquatableArray<string> GetPrimaryConstructorArguments(
		INamedTypeSymbol structSymbol,
		ImmutableArray<AttributeDataModelProperty> explicitProperties,
		CancellationToken cancellationToken
	)
	{
		var arguments =
			structSymbol
				.DeclaringSyntaxReferences.Select(reference => reference.GetSyntax(cancellationToken))
				.OfType<TypeDeclarationSyntax>()
				.Select(static declaration => declaration.ParameterList)
				.FirstOrDefault(static parameterList => parameterList is not null)
				?.Parameters.Select(parameter =>
				{
					var propertyName = ToPascalCase(parameter.Identifier.ValueText);
					return explicitProperties.Any(property => property.PropertyName == propertyName)
						? propertyName
						: $"default({parameter.Type})";
				})
				.ToImmutableArray()
			?? [];

		return new(arguments);
	}

	static ImmutableArray<AttributeDataModelProperty> ReadExplicitProperties(
		INamedTypeSymbol structSymbol,
		HashSet<string> excludedNames,
		List<ReportableDiagnostic> diagnostics,
		INamedTypeSymbol? targetAttributeType,
		CancellationToken cancellationToken
	)
	{
		var properties = ImmutableArray.CreateBuilder<AttributeDataModelProperty>();
		var constructor = structSymbol.InstanceConstructors.FirstOrDefault();
		if (constructor is null)
			return properties.ToImmutable();

		foreach (var parameter in constructor.Parameters)
		{
			cancellationToken.ThrowIfCancellationRequested();

			var propertyName = ToPascalCase(parameter.Name);
			var propertyType = parameter.Type;
			if (!IsSupportedType(propertyType))
			{
				diagnostics.Add(
					ReportableDiagnostic.Create(
						AttributeDataModelDiagnosticRules.PropertyTypeNotSupported,
						isBlocking: true,
						parameter,
						propertyName,
						propertyType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)
					)
				);
				continue;
			}

			if (IsSymbolOrSystemType(propertyType))
			{
				diagnostics.Add(
					ReportableDiagnostic.Create(
						AttributeDataModelDiagnosticRules.SymbolPropertyNotCacheable,
						isBlocking: true,
						parameter,
						propertyName,
						propertyType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)
					)
				);
				continue;
			}

			var info = ReadParameterAttributes(parameter, propertyName, targetAttributeType);

			if (info.IsExcluded)
			{
				excludedNames.Add(propertyName);
				continue;
			}

			if (info.IsNestedModel && !IsGeneratedAttributeModel(propertyType))
			{
				diagnostics.Add(
					ReportableDiagnostic.Create(
						AttributeDataModelDiagnosticRules.NestedModelNotGenerated,
						isBlocking: true,
						parameter,
						propertyType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)
					)
				);
			}

			if (info.IsTypeArgument && !IsTypeIdentityType(propertyType))
			{
				diagnostics.Add(
					ReportableDiagnostic.Create(
						AttributeDataModelDiagnosticRules.TypeArgumentPropertyTypeInvalid,
						isBlocking: true,
						parameter,
						propertyName,
						propertyType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)
					)
				);
			}

			if (info.IsEnum && propertyType.SpecialType != SpecialType.System_String)
			{
				diagnostics.Add(
					ReportableDiagnostic.Create(
						AttributeDataModelDiagnosticRules.IsEnumRequiresStringType,
						isBlocking: true,
						parameter,
						propertyName,
						propertyType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)
					)
				);
			}

			var sources = info.Sources;
			if (sources.IsEmpty)
				sources = [new(AttributePropertySource.NamedArgument, propertyName, -1)];

			var (modelTypeName, isNonNullableReferenceType) = GetModelTypeInfo(propertyType, autoDiscover: false);
			var defaultValueExpression = GetDefaultValueExpression(
				info.DefaultValue,
				modelTypeName,
				propertyType,
				parameter,
				diagnostics
			);

			if (isNonNullableReferenceType && !info.HasDefaultValue && !parameter.HasExplicitDefaultValue)
			{
				diagnostics.Add(
					ReportableDiagnostic.Create(
						AttributeDataModelDiagnosticRules.NonNullableReferenceTypeRequiresDefault,
						isBlocking: false,
						parameter,
						propertyName
					)
				);
			}

			properties.Add(
				new(
					PropertyName: propertyName,
					FullyQualifiedTypeName: modelTypeName,
					Sources: sources,
					DefaultValueExpression: defaultValueExpression,
					HasDefaultValue: info.HasDefaultValue,
					IsExplicit: true,
					IsNonNullableReferenceType: isNonNullableReferenceType,
					IsNestedModel: info.IsNestedModel,
					IsEnum: info.IsEnum,
					IsTypeIdentity: IsTypeIdentityType(propertyType),
					IsNullableValueType: propertyType.IsValueType
						&& propertyType.NullableAnnotation == NullableAnnotation.Annotated,
					NestedModelTypeName: info.IsNestedModel ? modelTypeName : null
				)
			);
		}

		return properties.ToImmutable();
	}

	static ParameterAttributeInfo ReadParameterAttributes(
		IParameterSymbol parameter,
		string propertyName,
		INamedTypeSymbol? targetAttributeType
	)
	{
		var sources = ImmutableArray.CreateBuilder<PropertySource>();
		var isExcluded = false;
		var isTypeArgument = false;
		object? defaultValue;
		bool hasDefaultValue;

		var excludeAttribute = GetAttribute(parameter, GeneratorTypeLibrary.Attirbutes.ExcludeAttribute);
		if (excludeAttribute is not null)
			isExcluded = true;

		var nestedModelInfo = ReadNestedModelAttributeInfo(parameter, isExcluded);
		var isNestedModel = nestedModelInfo.IsNestedModel;
		if (nestedModelInfo.IsNestedModel)
		{
			defaultValue = nestedModelInfo.DefaultValue;
			hasDefaultValue = nestedModelInfo.HasDefaultValue;
			sources.AddRange(nestedModelInfo.Sources);
		}
		else
		{
			var (IsTypeArgument, Sources, DefaultValue, HasDefaultValue) = ReadTypeArgumentAttributeInfo(
				parameter,
				isExcluded
			);

			isTypeArgument = IsTypeArgument;
			defaultValue = DefaultValue;
			hasDefaultValue = HasDefaultValue;
			sources.AddRange(Sources);
		}

		var hasExclusive = isExcluded || isNestedModel || isTypeArgument;

		var ctorAttribute = GetAttribute(parameter, GeneratorTypeLibrary.Attirbutes.ArgumentAttribute);
		var isEnum = false;
		if (ctorAttribute is not null && !hasExclusive)
		{
			var ctorName = GetCtorPropertyName(ctorAttribute);
			var ctorIndex = GetCtorPropertyIndex(ctorAttribute);
			var ctorDefaultValue = GetNamedArgument(
				ctorAttribute,
				"DefaultValue",
				GetConstructorArgument(ctorAttribute, 1, (object?)null)
			);
			isEnum = GetNamedArgument(ctorAttribute, "IsEnum", false);

			if (ctorName is not null)
				sources.Add(new PropertySource(AttributePropertySource.ConstructorName, ctorName, -1));
			else if (ctorIndex >= 0)
				sources.Add(new PropertySource(AttributePropertySource.ConstructorIndex, null, ctorIndex));

			if (ctorDefaultValue is not null)
			{
				defaultValue = ctorDefaultValue;
				hasDefaultValue = true;
			}
		}

		var namedAttribute = GetAttribute(parameter, GeneratorTypeLibrary.Attirbutes.PropertyAttribute);
		if (namedAttribute is not null && !hasExclusive)
		{
			var namedName = GetNamedArgument(namedAttribute, "Name", (string?)null);
			var namedDefaultValue = GetNamedArgument(
				namedAttribute,
				"DefaultValue",
				GetConstructorArgument(namedAttribute, 0, (object?)null)
			);
			isEnum = isEnum || GetNamedArgument(namedAttribute, "IsEnum", false);

			sources.Add(new PropertySource(AttributePropertySource.NamedArgument, namedName ?? propertyName, -1));

			if (namedDefaultValue is not null)
			{
				defaultValue = namedDefaultValue;
				hasDefaultValue = true;
			}
		}

		// An IsEnum default may be supplied as a bare member name (for example DefaultValue = "Inherit")
		// when the enum type is known to the generator through the resolved target attribute. Expand it to
		// the fully-qualified "{EnumFullName}.{Member}" form so the emitted GetEnum*Argument call receives
		// the same string the runtime's ToEnumString() produces. Fully-qualified defaults and defaults whose
		// enum type cannot be resolved are left unchanged.
		if (
			isEnum
			&& defaultValue is string defaultString
			&& !defaultString.Contains('.')
			&& targetAttributeType is not null
			&& TryResolveEnumDefault(
				targetAttributeType,
				sources.ToImmutable(),
				defaultString,
				out var effectiveDefault
			)
		)
		{
			defaultValue = effectiveDefault;
			hasDefaultValue = true;
		}

		return new ParameterAttributeInfo(
			isExcluded,
			isNestedModel,
			isTypeArgument,
			sources.ToImmutable(),
			defaultValue,
			hasDefaultValue,
			isEnum
		);
	}

	sealed record ParameterAttributeInfo(
		bool IsExcluded,
		bool IsNestedModel,
		bool IsTypeArgument,
		ImmutableArray<PropertySource> Sources,
		object? DefaultValue,
		bool HasDefaultValue,
		bool IsEnum
	);

	static (
		bool IsNestedModel,
		ImmutableArray<PropertySource> Sources,
		object? DefaultValue,
		bool HasDefaultValue
	) ReadNestedModelAttributeInfo(IParameterSymbol parameter, bool isExcluded)
	{
		var nestedModelAttribute = GetAttribute(parameter, GeneratorTypeLibrary.Attirbutes.NestedModelAttribute);
		if (nestedModelAttribute is null)
			return (false, [], null, false);

		if (isExcluded)
			return (false, [], null, false);

		var defaultValue = GetNamedArgument(nestedModelAttribute, "DefaultValue", (object?)null);
		var sources = ImmutableArray.CreateBuilder<PropertySource>();
		sources.Add(new PropertySource(AttributePropertySource.NestedModel, null, -1));

		return (true, sources.ToImmutable(), defaultValue, defaultValue is not null);
	}

	static (
		bool IsTypeArgument,
		ImmutableArray<PropertySource> Sources,
		object? DefaultValue,
		bool HasDefaultValue
	) ReadTypeArgumentAttributeInfo(IParameterSymbol parameter, bool isExcluded)
	{
		var typeArgumentAttribute = GetAttribute(
			parameter,
			GeneratorTypeLibrary.Attirbutes.GenericTypeArgumentAttribute
		);
		if (typeArgumentAttribute is null)
			return (false, [], null, false);

		if (isExcluded)
			return (false, [], null, false);

		var defaultValue = GetNamedArgument(typeArgumentAttribute, "DefaultValue", (object?)null);
		var hasDefaultValue = defaultValue is not null;

		var sources = ImmutableArray.CreateBuilder<PropertySource>();
		var typeArgName = GetNamedArgument(typeArgumentAttribute, "Name", (string?)null);
		var typeArgIndex = GetNamedArgument(typeArgumentAttribute, "Index", -1);
		if (typeArgName is not null)
			sources.Add(new PropertySource(AttributePropertySource.TypeArgument, typeArgName, -1));
		else
		{
			sources.Add(
				new PropertySource(AttributePropertySource.TypeArgument, null, typeArgIndex >= 0 ? typeArgIndex : 0)
			);
		}

		return (true, sources.ToImmutable(), defaultValue, hasDefaultValue);
	}

	static string? GetCtorPropertyName(AttributeData attributeData)
	{
		return
			attributeData.ConstructorArguments.Length > 0 && attributeData.ConstructorArguments[0].Value is string name
			? name
			: GetNamedArgument(attributeData, "Name", (string?)null);
	}

	static int GetCtorPropertyIndex(AttributeData attributeData)
	{
		return attributeData.ConstructorArguments.Length > 0 && attributeData.ConstructorArguments[0].Value is int index
			? index
			: GetNamedArgument(attributeData, "Index", -1);
	}

	static ImmutableArray<AttributeDataModelProperty> DiscoverProperties(
		ITypeSymbol targetAttributeType,
		ImmutableArray<AttributeDataModelProperty> explicitProperties,
		HashSet<string> excludedNames,
		List<ReportableDiagnostic> diagnostics,
		CancellationToken cancellationToken
	)
	{
		if (targetAttributeType is not INamedTypeSymbol namedType)
			return [];

		var discovered = ImmutableArray.CreateBuilder<AttributeDataModelProperty>();
		HashSet<string> discoveredNames = new(StringComparer.Ordinal);

		foreach (var constructor in namedType.InstanceConstructors)
		{
			cancellationToken.ThrowIfCancellationRequested();

			for (var i = 0; i < constructor.Parameters.Length; i++)
			{
				var parameter = constructor.Parameters[i];
				var propertyName = ToPascalCase(parameter.Name);

				if (discoveredNames.Contains(propertyName))
					continue;

				if (excludedNames.Contains(propertyName))
					continue;

				if (explicitProperties.Any(p => p.PropertyName == propertyName))
					continue;

				if (!IsSupportedType(parameter.Type))
				{
					diagnostics.Add(
						ReportableDiagnostic.Create(
							AttributeDataModelDiagnosticRules.PropertyTypeNotSupported,
							isBlocking: true,
							parameter,
							propertyName,
							parameter.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)
						)
					);
					continue;
				}

				if (IsSymbolOrSystemType(parameter.Type))
				{
					diagnostics.Add(
						ReportableDiagnostic.Create(
							AttributeDataModelDiagnosticRules.SymbolPropertyNotCacheable,
							isBlocking: true,
							parameter,
							propertyName,
							parameter.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)
						)
					);
					continue;
				}

				discoveredNames.Add(propertyName);

				var (modelTypeName, isNonNullableReferenceType) = GetModelTypeInfo(parameter.Type, autoDiscover: true);
				var defaultValueExpression = GetInferredDefaultExpression(parameter, modelTypeName, diagnostics);

				discovered.Add(
					new AttributeDataModelProperty(
						PropertyName: propertyName,
						FullyQualifiedTypeName: modelTypeName,
						Sources: EquatableArray<PropertySource>.Create(
							new PropertySource(AttributePropertySource.ConstructorName, parameter.Name, i)
						),
						DefaultValueExpression: defaultValueExpression,
						HasDefaultValue: parameter.HasExplicitDefaultValue,
						IsExplicit: false,
						IsNonNullableReferenceType: isNonNullableReferenceType,
						IsNestedModel: false,
						IsEnum: false,
						IsTypeIdentity: IsTypeIdentityType(parameter.Type),
						IsNullableValueType: parameter.Type.IsValueType
							&& parameter.Type.NullableAnnotation == NullableAnnotation.Annotated,
						NestedModelTypeName: null
					)
				);
			}
		}

		foreach (var property in namedType.GetMembers().OfType<IPropertySymbol>())
		{
			cancellationToken.ThrowIfCancellationRequested();

			if (property.IsStatic || property.DeclaredAccessibility != Accessibility.Public)
				continue;

			var propertyName = property.Name;
			if (discoveredNames.Contains(propertyName))
				continue;

			if (excludedNames.Contains(propertyName))
				continue;

			if (explicitProperties.Any(p => p.PropertyName == propertyName))
				continue;

			if (!IsSupportedType(property.Type))
			{
				diagnostics.Add(
					ReportableDiagnostic.Create(
						AttributeDataModelDiagnosticRules.PropertyTypeNotSupported,
						isBlocking: true,
						property,
						propertyName,
						property.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)
					)
				);
				continue;
			}

			if (IsSymbolOrSystemType(property.Type))
			{
				diagnostics.Add(
					ReportableDiagnostic.Create(
						AttributeDataModelDiagnosticRules.SymbolPropertyNotCacheable,
						isBlocking: true,
						property,
						propertyName,
						property.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)
					)
				);
				continue;
			}

			discoveredNames.Add(propertyName);

			var (modelTypeName, isNonNullableReferenceType) = GetModelTypeInfo(property.Type, autoDiscover: true);
			var defaultValueExpression = GetDefaultValueExpression(
				null,
				modelTypeName,
				property.Type,
				property,
				diagnostics
			);

			discovered.Add(
				new(
					PropertyName: propertyName,
					FullyQualifiedTypeName: modelTypeName,
					Sources: EquatableArray<PropertySource>.Create(
						new PropertySource(AttributePropertySource.NamedArgument, property.Name, -1)
					),
					DefaultValueExpression: defaultValueExpression,
					HasDefaultValue: false,
					IsExplicit: false,
					IsNonNullableReferenceType: isNonNullableReferenceType,
					IsNestedModel: false,
					IsEnum: false,
					IsTypeIdentity: IsTypeIdentityType(property.Type),
					IsNullableValueType: property.Type.IsValueType
						&& property.Type.NullableAnnotation == NullableAnnotation.Annotated,
					NestedModelTypeName: null
				)
			);
		}

		return discovered.ToImmutable();
	}

	static ImmutableArray<AttributeDataModelProperty> MergeProperties(
		ImmutableArray<AttributeDataModelProperty> explicitProperties,
		ImmutableArray<AttributeDataModelProperty> discoveredProperties
	)
	{
		if (explicitProperties.IsEmpty)
			return discoveredProperties;

		if (discoveredProperties.IsEmpty)
			return explicitProperties;

		var merged = ImmutableArray.CreateBuilder<AttributeDataModelProperty>();
		HashSet<string> explicitNames = [.. explicitProperties.Select(static p => p.PropertyName)];

		merged.AddRange(explicitProperties);
		foreach (var discovered in discoveredProperties)
		{
			if (!explicitNames.Contains(discovered.PropertyName))
				merged.Add(discovered);
		}

		return merged.ToImmutable();
	}

	static string GetDefaultValueExpression(
		object? defaultValue,
		string modelTypeName,
		ITypeSymbol originalType,
		ISymbol target,
		List<ReportableDiagnostic> diagnostics
	)
	{
		if (defaultValue is not null)
		{
			if (TryFormatValue(defaultValue, originalType, out var expression))
				return expression;

			diagnostics.Add(
				ReportableDiagnostic.Create(
					AttributeDataModelDiagnosticRules.DefaultValueNotSupported,
					isBlocking: true,
					target,
					Convert.ToString(defaultValue, CultureInfo.InvariantCulture) ?? "null",
					originalType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)
				)
			);
		}

		return $"default({modelTypeName})";
	}

	static string GetInferredDefaultExpression(
		IParameterSymbol parameter,
		string modelTypeName,
		List<ReportableDiagnostic> diagnostics
	)
	{
		return parameter.HasExplicitDefaultValue
			? GetDefaultValueExpression(
				parameter.ExplicitDefaultValue,
				modelTypeName,
				parameter.Type,
				parameter,
				diagnostics
			)
			: $"default({modelTypeName})";
	}

	static bool TryFormatValue(object? value, ITypeSymbol typeSymbol, out string expression)
	{
		expression = string.Empty;

		if (value is null)
		{
			expression = "null";
			return true;
		}

		if (value is string s)
		{
			// TypedConstant instances can only be supplied by Roslyn. Its constructors are
			// internal, so a textual attribute-model default cannot be converted into one.
			if (IsTypedConstantType(typeSymbol))
				return false;

			expression = $"\"{EscapeString(s)}\"";
			return true;
		}

		if (value is bool b)
		{
			expression = b ? "true" : "false";
			return true;
		}

		if (value is ITypeSymbol typeValue)
		{
			expression = $"typeof(global::{TypeHelpers.ToFullyQualifiedDisplayString(typeValue)})";
			return true;
		}

		if (typeSymbol.TypeKind == TypeKind.Enum)
		{
			// ToFullyQualifiedDisplayString already includes the global:: prefix.
			var enumTypeName = TypeHelpers.ToFullyQualifiedDisplayString(typeSymbol);
			expression = $"({enumTypeName}){Convert.ToString(value, CultureInfo.InvariantCulture)}";
			return true;
		}

		if (value is IFormattable formattable)
		{
			expression = formattable.ToString(null, CultureInfo.InvariantCulture);
			return expression is not null;
		}

		return false;
	}

	static bool IsTypedConstantType(ITypeSymbol typeSymbol) =>
		typeSymbol.Name == "TypedConstant"
		&& typeSymbol.ContainingNamespace.ToDisplayString() == "Microsoft.CodeAnalysis";

	static string EscapeString(string value)
	{
		StringBuilder builder = new(value.Length);
		foreach (var c in value)
		{
			builder.Append(
				c switch
				{
					'"' => "\\\"",
					'\\' => "\\\\",
					'\n' => "\\n",
					'\r' => "\\r",
					'\t' => "\\t",
					_ => c.ToString(),
				}
			);
		}
		return builder.ToString();
	}

	static (string TypeName, bool IsNonNullableReferenceType) GetModelTypeInfo(
		ITypeSymbol typeSymbol,
		bool autoDiscover
	)
	{
		var knownType = KnownLangTypes.Get(typeSymbol.SpecialType);
		var typeName = knownType.IsEmpty ? TypeHelpers.ToFullyQualifiedDisplayString(typeSymbol) : knownType.Keyword;

		var isNonNullableReferenceType = false;

		if (
			typeSymbol.IsReferenceType
			&& typeSymbol.NullableAnnotation == NullableAnnotation.Annotated
			&& !typeName.EndsWith("?", StringComparison.Ordinal)
		)
		{
			typeName += "?";
		}
		else if (autoDiscover && typeSymbol.IsReferenceType && !typeName.EndsWith("?", StringComparison.Ordinal))
		{
			typeName += "?";
		}
		else if (typeSymbol.IsReferenceType && typeSymbol.NullableAnnotation != NullableAnnotation.Annotated)
		{
			isNonNullableReferenceType = true;
		}

		return (typeName, isNonNullableReferenceType);
	}

	static bool IsSystemType(ITypeSymbol typeSymbol) => PurviewTypeLibrary.System.Type.Equals(typeSymbol);

	static bool IsTypeSymbolType(ITypeSymbol typeSymbol)
	{
		if (typeSymbol is not INamedTypeSymbol namedType)
			return false;

		var namespaceName = namedType.ContainingNamespace.IsGlobalNamespace
			? null
			: namedType.ContainingNamespace.ToDisplayString();

		return namespaceName == "Microsoft.CodeAnalysis"
			&& namedType.Name is "ITypeSymbol" or "INamedTypeSymbol" or "ISymbol";
	}

	static bool IsSymbolOrSystemType(ITypeSymbol typeSymbol) =>
		IsTypeSymbolType(typeSymbol) || IsSystemType(typeSymbol);

	static bool IsTypeIdentityType(ITypeSymbol typeSymbol)
	{
		var candidate = typeSymbol;
		if (
			candidate is INamedTypeSymbol nullableType
			&& nullableType.IsValueType
			&& nullableType.ContainingNamespace?.ToDisplayString() == "System"
			&& nullableType.Name == "Nullable"
			&& nullableType.TypeArguments.Length == 1
		)
		{
			candidate = nullableType.TypeArguments[0];
		}

		if (candidate is not INamedTypeSymbol namedType)
			return false;

		var namespaceName = namedType.ContainingNamespace.IsGlobalNamespace
			? null
			: namedType.ContainingNamespace.ToDisplayString();

		return namespaceName == "Purview.SourceGeneratorFramework" && namedType.Name == "TypeIdentity";
	}

	static bool IsSupportedType(ITypeSymbol typeSymbol)
	{
		return typeSymbol.TypeKind is not TypeKind.Array and not TypeKind.Pointer and not TypeKind.FunctionPointer;
	}

	static bool IsGeneratedAttributeModel(ITypeSymbol typeSymbol) =>
		typeSymbol is not INamedTypeSymbol namedType || namedType.TypeKind != TypeKind.Struct
			? false
			: GetAttribute(namedType, GeneratorTypeLibrary.Attirbutes.GenerateAttribute) is not null;

	static AttributeData? GetAttribute(ISymbol symbol, TypeIdentity attributeType)
	{
		foreach (var attribute in symbol.GetAttributes())
		{
			if (attribute.AttributeClass is not null && attributeType.Equals(attribute.AttributeClass))
				return attribute;
		}

		return null;
	}

	static TypeIdentity ParseTypeIdentity(string fullyQualifiedName)
	{
		var lastDot = fullyQualifiedName.LastIndexOf('.');
		if (lastDot < 0)
			return new TypeIdentity(fullyQualifiedName, null);

		var typeName = fullyQualifiedName.Substring(lastDot + 1);
		var namespaceName = fullyQualifiedName.Substring(0, lastDot);
		return new TypeIdentity(typeName, namespaceName);
	}

	/// <summary>
	/// Reads the generated class name and <c>[TypeRef]</c> members of a <c>[GenerateTypeLibrary]</c> spec,
	/// mirroring <c>TypeLibraryModelLibrary.BuildTarget</c>. The members are used to validate a reconstructed
	/// <c>TypeLibrary.{Namespace}.{Member}FullName</c> reference even when the underlying type is not resolvable
	/// in this compilation.
	/// </summary>
	static TypeLibrarySpecData ReadTypeLibrarySpecData(INamedTypeSymbol specSymbol)
	{
		var generateAttribute = GetAttribute(specSymbol, GeneratorTypeLibrary.Attirbutes.GenerateTypeLibraryAttribute);
		var className = generateAttribute is null
			? "TypeLibrary"
			: GetNamedArgument(generateAttribute, "ClassName", (string?)null) ?? "TypeLibrary";

		var members = ImmutableArray.CreateBuilder<TypeLibraryMemberRef>();
		foreach (var field in specSymbol.GetMembers().OfType<IFieldSymbol>())
		{
			var typeRef = GetAttribute(field, GeneratorTypeLibrary.Attirbutes.TypeRefAttribute);
			if (typeRef is null)
				continue;

			var @namespace = ReadTypeRefNamespace(typeRef);
			if (@namespace is null)
				continue;

			members.Add(new TypeLibraryMemberRef(field.Name, @namespace));
		}

		return new TypeLibrarySpecData(className, new EquatableArray<TypeLibraryMemberRef>(members.ToImmutable()));
	}

	/// <summary>
	/// Reads the namespace of a <c>[TypeRef]</c> marker member, mirroring the namespace-only, explicit, and
	/// <c>typeof(...)</c> declaration forms.
	/// </summary>
	static string? ReadTypeRefNamespace(AttributeData typeRef)
	{
		var named = GetNamedArgument(typeRef, "Namespace", (string?)null);
		if (named is not null)
			return named;

		var constructor = typeRef.AttributeConstructor;
		var isNamespaceOnlyForm =
			constructor is { Parameters.Length: > 0 }
			&& constructor.Parameters[0].Type.SpecialType == SpecialType.System_String;
		if (isNamespaceOnlyForm)
			return GetConstructorArgument(typeRef, 0, (string?)null);

		if (typeRef.ConstructorArguments.Length > 0 && typeRef.ConstructorArguments[0].Value is ITypeSymbol typeSymbol)
		{
			return typeSymbol.ContainingNamespace.IsGlobalNamespace
				? null
				: typeSymbol.ContainingNamespace.ToDisplayString();
		}

		// The typeof(...) form is the only remaining valid form, which has a single constructor argument of type
		return GetConstructorArgument(typeRef, 1, (string?)null);
	}

	/// <summary>
	/// Reassembles a <c>[Generate]</c> target from a reference to a generated type-library full-name constant
	/// (for example <c>TypeLibrary.Aspire.Hosting.AspireC4.SeverityAttributeFullName</c>). The constant is
	/// emitted by <c>TypeLibraryGenerator</c>'s main pipeline, so its value is an error constant (null) in the
	/// compilation this generator sees; the target full name is derived from the argument's member-access
	/// expression and validated against the <c>[TypeRef]</c> members of the matching spec.
	/// </summary>
	/// <remarks>
	/// The reconstruction is guarded so arbitrary unresolved constants are never reinterpreted: the root
	/// identifier must match the <c>ClassName</c> of a source <c>[GenerateTypeLibrary]</c> spec and the
	/// reassembled namespace + member name must be declared by that spec. A valid reference resolves to a type
	/// when the underlying type is present in this compilation (for example post-init generated attributes); when
	/// it is not (the consumer's own generator emits the marker attributes, which do not run on the project being
	/// compiled), the reconstructed identity is used so the model still generates.
	/// </remarks>
	static bool TryResolveTypeLibraryTarget(
		AttributeData generateAttribute,
		EquatableArray<TypeLibrarySpecData> typeLibrarySpecData,
		Compilation compilation,
		CancellationToken cancellationToken,
		out ITypeSymbol? targetType,
		out TypeIdentity targetIdentity
	)
	{
		targetType = null;
		targetIdentity = default;

		if (
			generateAttribute.ApplicationSyntaxReference?.GetSyntax(cancellationToken)
			is not AttributeSyntax attributeSyntax
		)
			return false;

		var expression = attributeSyntax.ArgumentList?.Arguments.FirstOrDefault()?.Expression;
		if (expression is null)
			return false;

		var segments = GetMemberAccessSegments(expression);
		if (segments.Length < 2)
			return false;

		var root = segments[0];
		var spec = typeLibrarySpecData.AsImmutableArray().FirstOrDefault(spec => spec.ClassName == root);
		if (spec is null)
			return false;

		var finalSegment = segments[segments.Length - 1];
		var typeNameSegment = finalSegment.EndsWith("FullName", StringComparison.Ordinal)
			? finalSegment.Substring(0, finalSegment.Length - "FullName".Length)
			: finalSegment;
		if (typeNameSegment.Length == 0)
			return false;

		var @namespace = string.Join(".", segments.Skip(1).Take(segments.Length - 2));

		// The reference must correspond to a [TypeRef] member declared by the spec: the TypeLibrary emits
		// {Member}FullName = "{Namespace}.{TypeName}" only for declared members, so a matching member confirms
		// the constant exists even when the underlying type is not resolvable in this compilation.
		if (!spec.Members.AsImmutableArray().Contains(new TypeLibraryMemberRef(typeNameSegment, @namespace)))
			return false;

		var fullName = @namespace.Length == 0 ? typeNameSegment : @namespace + "." + typeNameSegment;
		targetIdentity = ParseTypeIdentity(fullName);
		targetType = compilation.GetTypeByMetadataName(fullName);
		return true;
	}

	/// <summary>
	/// Walks a dotted member-access expression (for example <c>TypeLibrary.Aspire.Hosting.AspireC4.SeverityAttributeFullName</c>)
	/// into its dotted name segments, outermost identifier first.
	/// </summary>
	static ImmutableArray<string> GetMemberAccessSegments(ExpressionSyntax expression)
	{
		var segments = ImmutableArray.CreateBuilder<string>();
		while (true)
		{
			switch (expression)
			{
				case MemberAccessExpressionSyntax memberAccess:
					segments.Add(memberAccess.Name.Identifier.ValueText);
					expression = memberAccess.Expression;
					break;
				case IdentifierNameSyntax identifier:
					segments.Add(identifier.Identifier.ValueText);
					segments.Reverse();
					return segments.ToImmutable();
				case AliasQualifiedNameSyntax alias:
					segments.Add(alias.Name.Identifier.ValueText);
					segments.Reverse();
					return segments.ToImmutable();
				default:
					return [];
			}
		}
	}

	/// <summary>
	/// Expands a bare enum member name (for example <c>Inherit</c>) to its fully-qualified
	/// <c>"{EnumFullName}.{Member}"</c> form using the target attribute's property or constructor parameter
	/// type mapped by the property source. Returns <see langword="false"/> when no source maps to an enum type.
	/// </summary>
	static bool TryResolveEnumDefault(
		INamedTypeSymbol targetAttributeType,
		ImmutableArray<PropertySource> sources,
		string memberName,
		out string effectiveDefault
	)
	{
		foreach (var source in sources)
		{
			ITypeSymbol? memberType = null;
			if (source.Source == AttributePropertySource.NamedArgument && source.MappedName is not null)
			{
				memberType = targetAttributeType
					.GetMembers(source.MappedName)
					.OfType<IPropertySymbol>()
					.FirstOrDefault()
					?.Type;
			}
			else if (source.Source == AttributePropertySource.ConstructorName && source.MappedName is not null)
			{
				var mappedName = source.MappedName;
				memberType = targetAttributeType
					.InstanceConstructors.SelectMany(static ctor => ctor.Parameters)
					.FirstOrDefault(parameter =>
						string.Equals(parameter.Name, mappedName, StringComparison.OrdinalIgnoreCase)
					)
					?.Type;
			}
			else if (source.Source == AttributePropertySource.ConstructorIndex)
			{
				memberType = targetAttributeType
					.InstanceConstructors.SelectMany(static ctor => ctor.Parameters)
					.FirstOrDefault(parameter => parameter.Ordinal == source.ConstructorIndex)
					?.Type;
			}

			if (memberType is { TypeKind: TypeKind.Enum })
			{
				effectiveDefault = $"{BuildEnumFullName(memberType)}.{memberName}";
				return true;
			}
		}

		effectiveDefault = string.Empty;
		return false;
	}

	static string BuildEnumFullName(ITypeSymbol enumType)
	{
		var @namespace = enumType.ContainingNamespace;
		return @namespace is null || @namespace.IsGlobalNamespace
			? enumType.Name
			: $"{@namespace.ToDisplayString()}.{enumType.Name}";
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
			return (T?)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
		}
		catch
		{
			return defaultValue;
		}
	}

	static string ToPascalCase(string value)
	{
		if (string.IsNullOrEmpty(value))
			return value;

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
}

/// <summary>
/// The generated class name and <c>[TypeRef]</c> members of a <c>[GenerateTypeLibrary]</c> spec, used to guard
/// and validate a <c>TypeLibrary.{Namespace}.{Member}FullName</c> target reconstruction.
/// </summary>
sealed record TypeLibrarySpecData(string ClassName, EquatableArray<TypeLibraryMemberRef> Members);

/// <summary>
/// A single <c>[TypeRef]</c> member declared by a <c>[GenerateTypeLibrary]</c> spec.
/// </summary>
sealed record TypeLibraryMemberRef(string MemberName, string Namespace);
