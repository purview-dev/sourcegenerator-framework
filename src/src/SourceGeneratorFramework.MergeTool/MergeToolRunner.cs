using System.Security.Cryptography;
using System.Text;
using ILRepacking;
using Mono.Cecil;

static class MergeToolRunner
{
	const string IsExternalInitName = "IsExternalInit";
	const string IsExternalInitNamespace = "System.Runtime.CompilerServices";
	const string TagCommand = "--tag";

	public static int Run(string[] args, TextWriter error, ILogger? logger = null)
	{
		if (args.Length > 0 && string.Equals(args[0], TagCommand, StringComparison.Ordinal))
			return WriteContentTag(args, error);

		if (args.Length < 3)
		{
			error.WriteLine("Usage: Purview.SourceGeneratorFramework.MergeTool <component> <framework> <output>");
			error.WriteLine("       Purview.SourceGeneratorFramework.MergeTool --tag <input> [<input>...]");
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

		// Merge into a per-process staging directory that keeps the output's file name, then publish
		// the directory with a non-overwriting move. The file name must be preserved because ILRepack
		// derives the merged assembly's simple name from it, and the build content-addresses the
		// output directory so a concurrent invocation that loses the race simply observes the
		// published artifact instead of failing with a sharing violation against a file the compiler
		// host already has loaded.
		var outputDirectory = Path.GetDirectoryName(outputPath)!;
		var stagingDirectory = outputDirectory + ".staging-" + Environment.ProcessId;
		Directory.CreateDirectory(stagingDirectory);
		var stagingPath = Path.Combine(stagingDirectory, Path.GetFileName(outputPath));

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
			stagingPath,
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
				OutputFile = stagingPath,
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

			RestoreCanonicalIsExternalInit(stagingPath, searchDirectories);

			// ILRepack's internalize is best-effort and cannot reach framework types the component's
			// own generators emit, so force self-containment deterministically and fail the build
			// rather than shipping an analyzer that leaks framework types.
			var internalization = FrameworkTypeInternalizer.Apply(
				stagingPath,
				searchDirectories,
				logger is not null ? logger.Warn : message => error.WriteLine(message)
			);

			if (internalization.PublicFrameworkTypesRemaining.Length > 0)
			{
				error.WriteLine(
					$"The merged component '{stagingPath}' still exposes public Purview.SourceGeneratorFramework types: {string.Join(", ", internalization.PublicFrameworkTypesRemaining)}."
				);
				return 5;
			}

			return Publish(stagingPath, outputPath, error) ? 0 : 6;
		}
		finally
		{
			normalizedComponent?.Dispose();
		}
	}

	/// <summary>
	/// Writes a deterministic content tag (SHA-256 over the per-input SHA-256 values) for the given
	/// inputs to standard output. The build uses it to give the merged artifact a content-addressed
	/// directory, so a rebuild with unchanged inputs skips the merge and changed inputs never
	/// overwrite a merged assembly the compiler host has loaded.
	/// </summary>
	static int WriteContentTag(string[] args, TextWriter error)
	{
		StringBuilder builder = new();
		for (var index = 1; index < args.Length; index++)
		{
			var path = Path.GetFullPath(args[index]);
			if (!File.Exists(path))
			{
				error.WriteLine($"Merge input was not found: {path}");
				return 3;
			}

			builder.Append(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))));
			builder.Append('\n');
		}

		Console.Out.WriteLine(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()))));
		return 0;
	}

	/// <summary>
	/// Publishes the staged merged assembly by renaming its staging directory onto the
	/// content-addressed destination directory. When a concurrent invocation has already produced
	/// the same artifact the staging directory is discarded and the call succeeds.
	/// </summary>
	static bool Publish(string stagingPath, string outputPath, TextWriter error)
	{
		var stagingDirectory = Path.GetDirectoryName(stagingPath)!;
		var outputDirectory = Path.GetDirectoryName(outputPath)!;

		// IsExternalInitNormalizer stages its normalized component in a .purview-merge-* directory
		// beside the output; it must not be published with the merged artifact.
		foreach (
			var temporary in Directory.EnumerateDirectories(
				stagingDirectory,
				".purview-merge-*",
				SearchOption.TopDirectoryOnly
			)
		)
		{
			TryDeleteDirectory(temporary);
		}

		if (File.Exists(outputPath))
		{
			// Another invocation produced the same (content-addressed) artifact first.
			TryDeleteDirectory(stagingDirectory);
			return true;
		}

		try
		{
			Directory.Move(stagingDirectory, outputDirectory);
		}
		catch (IOException) when (File.Exists(outputPath))
		{
			TryDeleteDirectory(stagingDirectory);
			return true;
		}
		catch (IOException exception)
		{
			error.WriteLine($"Failed to publish the merged component '{outputPath}': {exception.Message}");
			TryDeleteDirectory(stagingDirectory);
			return false;
		}

		return true;
	}

	static void TryDeleteDirectory(string path)
	{
		try
		{
			if (Directory.Exists(path))
				Directory.Delete(path, recursive: true);
		}
		catch (IOException)
		{
			// Leftover staging directories are harmless; the build cleans its intermediate directory.
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
