using ILRepacking;
using Mono.Cecil;

static class MergeToolRunner
{
	public static int Run(string[] args, TextWriter error, ILogger? logger = null)
	{
		if (args.Length < 3)
		{
			error.WriteLine("Usage: Purview.SourceGeneratorFramework.MergeTool <component> <framework> <output>");
			return 2;
		}

		var componentPath = Path.GetFullPath(args[0]);
		var frameworkPath = Path.GetFullPath(args[1]);
		var outputPath = Path.GetFullPath(args[2]);

		if (!File.Exists(componentPath))
		{
			error.WriteLine($"Roslyn component assembly was not found: {componentPath}");
			return 3;
		}

		if (!File.Exists(frameworkPath))
		{
			error.WriteLine($"Source Generator Framework assembly was not found: {frameworkPath}");
			return 4;
		}

		Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

		HashSet<string> searchDirectories = new(StringComparer.OrdinalIgnoreCase)
		{
			Path.GetDirectoryName(componentPath)!,
			Path.GetDirectoryName(frameworkPath)!,
		};

		foreach (var searchPath in args.Skip(3))
		{
			var fullSearchPath = Path.GetFullPath(searchPath);
			searchDirectories.Add(
				File.Exists(fullSearchPath) ? Path.GetDirectoryName(fullSearchPath)! : fullSearchPath
			);
		}

		var normalizedComponent = IsExternalInitNormalizer.Normalize(
			componentPath,
			frameworkPath,
			outputPath,
			searchDirectories
		);

		try
		{
			if (normalizedComponent is not null)
			{
				searchDirectories.Add(Path.GetDirectoryName(normalizedComponent.AssemblyPath)!);
			}

			RepackOptions options = new()
			{
				InputAssemblies = [normalizedComponent?.AssemblyPath ?? componentPath, frameworkPath],
				OutputFile = outputPath,
				SearchDirectories = searchDirectories,
				Internalize = true,
				InternalizeAssemblies = [Path.GetFileNameWithoutExtension(frameworkPath)],
				RenameInternalized = false,
				UnionMerge = true,
				Parallel = true,
				DebugInfo = File.Exists(
					Path.ChangeExtension(normalizedComponent?.AssemblyPath ?? componentPath, ".pdb")
				),
				TargetKind = ILRepack.Kind.Dll,
			};

			if (logger is null)
			{
				new ILRepack(options).Repack();
			}
			else
			{
				new ILRepack(options, logger).Repack();
			}

			var frameworkTypeNames = ReadTypeNames(frameworkPath, searchDirectories);
			InternalizeFrameworkTypes(outputPath, frameworkTypeNames, searchDirectories);
			return 0;
		}
		finally
		{
			normalizedComponent?.Dispose();
		}
	}

	static HashSet<string> ReadTypeNames(string assemblyPath, IEnumerable<string> searchDirectories)
	{
		using var resolver = CreateResolver(searchDirectories);
		using var assembly = AssemblyDefinition.ReadAssembly(
			assemblyPath,
			new ReaderParameters { AssemblyResolver = resolver }
		);
		return assembly
			.MainModule.Types.SelectMany(Flatten)
			.Select(static type => type.FullName)
			.ToHashSet(StringComparer.Ordinal);
	}

	static void InternalizeFrameworkTypes(
		string assemblyPath,
		HashSet<string> frameworkTypeNames,
		IEnumerable<string> searchDirectories
	)
	{
		var pdbPath = Path.ChangeExtension(assemblyPath, ".pdb");
		var hasSymbols = File.Exists(pdbPath);
		using var resolver = CreateResolver(searchDirectories);
		using var assembly = AssemblyDefinition.ReadAssembly(
			assemblyPath,
			new ReaderParameters
			{
				AssemblyResolver = resolver,
				ReadSymbols = hasSymbols,
				InMemory = true,
			}
		);

		foreach (var type in assembly.MainModule.Types.SelectMany(Flatten))
		{
			if (!frameworkTypeNames.Contains(type.FullName))
			{
				continue;
			}

			type.Attributes = type.IsNested
				? (type.Attributes & ~TypeAttributes.VisibilityMask) | TypeAttributes.NestedAssembly
				: (type.Attributes & ~TypeAttributes.VisibilityMask) | TypeAttributes.NotPublic;
		}

		assembly.Write(assemblyPath, new WriterParameters { WriteSymbols = hasSymbols });
	}

	internal static IEnumerable<TypeDefinition> Flatten(TypeDefinition type)
	{
		yield return type;
		foreach (var nestedType in type.NestedTypes.SelectMany(Flatten))
		{
			yield return nestedType;
		}
	}

	internal static DefaultAssemblyResolver CreateResolver(IEnumerable<string> searchDirectories)
	{
		DefaultAssemblyResolver resolver = new();
		foreach (var searchDirectory in searchDirectories)
		{
			resolver.AddSearchDirectory(searchDirectory);
		}

		return resolver;
	}
}
