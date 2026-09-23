using Mono.Cecil;

static class IsExternalInitNormalizer
{
	const string MarkerFullName = "System.Runtime.CompilerServices.IsExternalInit";

	public static NormalizedAssembly? Normalize(
		string componentPath,
		string frameworkPath,
		string outputPath,
		IEnumerable<string> searchDirectories
	)
	{
		using var resolver = MergeToolRunner.CreateResolver(searchDirectories);
		using var framework = AssemblyDefinition.ReadAssembly(
			frameworkPath,
			new ReaderParameters { AssemblyResolver = resolver }
		);
		var frameworkMarker = framework.MainModule.GetType(MarkerFullName);
		if (frameworkMarker is null)
		{
			return null;
		}

		var componentPdbPath = Path.ChangeExtension(componentPath, ".pdb");
		var hasSymbols = File.Exists(componentPdbPath);
		using var component = AssemblyDefinition.ReadAssembly(
			componentPath,
			new ReaderParameters
			{
				AssemblyResolver = resolver,
				ReadSymbols = hasSymbols,
				InMemory = true,
			}
		);
		var componentMarker = component.MainModule.GetType(MarkerFullName);
		if (componentMarker is null)
		{
			return null;
		}

		// A component-local marker gives init setter references a different modreq identity from
		// framework definitions. Normalize a temporary copy so ILRepack can bind those methods.
		var frameworkMarkerReference = component.MainModule.ImportReference(frameworkMarker);
		RequiredModifierRewriter rewriter = new(component.MainModule, componentMarker, frameworkMarkerReference);
		rewriter.Rewrite();

		if (rewriter.RemainingComponentMarkerReferences != 0)
		{
			throw new InvalidOperationException(
				$"Failed to normalize all {MarkerFullName} references in '{componentPath}'."
			);
		}

		component.MainModule.Types.Remove(componentMarker);

		// Keep the component's bin output untouched; only the transient merge input is rewritten.
		var normalizedDirectory = Path.Combine(
			Path.GetDirectoryName(outputPath)!,
			$".purview-merge-{Guid.NewGuid():N}"
		);
		Directory.CreateDirectory(normalizedDirectory);
		var normalizedPath = Path.Combine(normalizedDirectory, Path.GetFileName(componentPath));
		component.Write(normalizedPath, new WriterParameters { WriteSymbols = hasSymbols });

		return new NormalizedAssembly(normalizedPath, normalizedDirectory);
	}

	sealed class RequiredModifierRewriter(
		ModuleDefinition componentModule,
		TypeDefinition componentMarker,
		TypeReference frameworkMarker
	)
	{
		int _remainingComponentMarkerReferences;

		public int RemainingComponentMarkerReferences => _remainingComponentMarkerReferences;

		public void Rewrite()
		{
			foreach (var type in componentModule.Types.SelectMany(MergeToolRunner.Flatten))
			{
				Rewrite(type.BaseType);
				foreach (var @interface in type.Interfaces)
				{
					Rewrite(@interface.InterfaceType);
				}

				Rewrite(type.GenericParameters);

				foreach (var field in type.Fields)
				{
					Rewrite(field.FieldType);
				}

				foreach (var property in type.Properties)
				{
					Rewrite(property.PropertyType);
					Rewrite(property.Parameters);
				}

				foreach (var @event in type.Events)
				{
					Rewrite(@event.EventType);
				}

				foreach (var method in type.Methods)
				{
					Rewrite(method.ReturnType);
					Rewrite(method.Parameters);
					Rewrite(method.GenericParameters);

					foreach (var @override in method.Overrides)
					{
						Rewrite(@override);
					}

					if (!method.HasBody)
					{
						continue;
					}

					foreach (var variable in method.Body.Variables)
					{
						Rewrite(variable.VariableType);
					}

					foreach (var handler in method.Body.ExceptionHandlers)
					{
						Rewrite(handler.CatchType);
					}

					foreach (var instruction in method.Body.Instructions)
					{
						RewriteOperand(instruction.Operand);
					}
				}
			}
		}

		void RewriteOperand(object? operand)
		{
			switch (operand)
			{
				case TypeReference type:
					Rewrite(type);
					break;
				case MethodReference method:
					Rewrite(method);
					break;
				case FieldReference field:
					Rewrite(field.DeclaringType);
					Rewrite(field.FieldType);
					break;
				case CallSite callSite:
					Rewrite(callSite.ReturnType);
					Rewrite(callSite.Parameters);
					break;
				default:
					break;
			}
		}

		void Rewrite(MethodReference method)
		{
			Rewrite(method.DeclaringType);
			Rewrite(method.ReturnType);
			Rewrite(method.Parameters);

			if (method is GenericInstanceMethod genericMethod)
			{
				foreach (var argument in genericMethod.GenericArguments)
				{
					Rewrite(argument);
				}
			}
		}

		void Rewrite(IEnumerable<ParameterDefinition> parameters)
		{
			foreach (var parameter in parameters)
			{
				Rewrite(parameter.ParameterType);
			}
		}

		void Rewrite(IEnumerable<GenericParameter> parameters)
		{
			foreach (var parameter in parameters)
			{
				foreach (var constraint in parameter.Constraints)
				{
					Rewrite(constraint.ConstraintType);
				}
			}
		}

		void Rewrite(TypeReference? type)
		{
			if (type is null)
			{
				return;
			}

			if (type is RequiredModifierType requiredModifier)
			{
				if (IsComponentMarker(requiredModifier.ModifierType))
				{
					requiredModifier.ModifierType = frameworkMarker;
				}

				Rewrite(requiredModifier.ModifierType);
				Rewrite(requiredModifier.ElementType);
				return;
			}

			if (type is OptionalModifierType optionalModifier)
			{
				Rewrite(optionalModifier.ModifierType);
				Rewrite(optionalModifier.ElementType);
				return;
			}

			if (type is GenericInstanceType genericInstance)
			{
				Rewrite(genericInstance.ElementType);
				foreach (var argument in genericInstance.GenericArguments)
				{
					Rewrite(argument);
				}

				return;
			}

			if (type is FunctionPointerType functionPointer)
			{
				Rewrite(functionPointer.ReturnType);
				Rewrite(functionPointer.Parameters);
				return;
			}

			if (type is TypeSpecification specification)
			{
				Rewrite(specification.ElementType);
			}

			if (IsComponentMarker(type))
			{
				_remainingComponentMarkerReferences++;
			}

			Rewrite(type.DeclaringType);
		}

		bool IsComponentMarker(TypeReference type) =>
			type.FullName == MarkerFullName
			&& (
				ReferenceEquals(type, componentMarker)
				|| ReferenceEquals(type.Scope, componentModule)
				|| (type.Scope is AssemblyNameReference assembly && assembly.Name == componentModule.Assembly.Name.Name)
			);
	}
}

sealed class NormalizedAssembly(string assemblyPath, string directoryPath) : IDisposable
{
	public string AssemblyPath { get; } = assemblyPath;

	public void Dispose()
	{
		if (Directory.Exists(directoryPath))
		{
			Directory.Delete(directoryPath, recursive: true);
		}
	}
}
