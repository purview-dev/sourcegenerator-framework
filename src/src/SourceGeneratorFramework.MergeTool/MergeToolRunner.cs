using ILRepacking;
using Mono.Cecil;

static class MergeToolRunner
{
	const string IsExternalInitName = "IsExternalInit";
	const string IsExternalInitNamespace = "System.Runtime.CompilerServices";

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

			RestoreCanonicalIsExternalInit(outputPath, searchDirectories);

			return 0;
		}
		finally
		{
			normalizedComponent?.Dispose();
		}
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

	static void RestoreCanonicalIsExternalInit(string assemblyPath, IEnumerable<string> searchDirectories)
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

		var marker = assembly
			.MainModule.Types.SelectMany(Flatten)
			.SingleOrDefault(static type =>
				type.Name == IsExternalInitName
				|| type.Name.EndsWith(IsExternalInitName, StringComparison.Ordinal)
				|| (
					type.Namespace.Contains(IsExternalInitNamespace, StringComparison.Ordinal)
					&& type.Name.Contains(IsExternalInitName, StringComparison.Ordinal)
				)
			);

		if (marker is null)
			return;

		marker.Namespace = IsExternalInitNamespace;
		marker.Name = IsExternalInitName;
		marker.Attributes = marker.IsNested
			? (marker.Attributes & ~TypeAttributes.VisibilityMask) | TypeAttributes.NestedAssembly
			: (marker.Attributes & ~TypeAttributes.VisibilityMask) | TypeAttributes.NotPublic;

		assembly.Write(assemblyPath, new WriterParameters { WriteSymbols = hasSymbols });
	}
}
