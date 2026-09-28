using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using ILRepacking;
using Mono.Cecil;

static class MergeToolRunner
{
	const string IsExternalInitName = "IsExternalInit";
	const string IsExternalInitNamespace = "System.Runtime.CompilerServices";
	const string TagCommand = "--tag";
	const string InputValueSwitch = "--input-value";
	const string OwnedNamespaceSwitch = "--owned-namespace";
	const string OwnedTypeSwitch = "--owned-type";
	const string PublicSurfaceSeveritySwitch = "--public-surface-severity";
	const string OriginSwitch = "--origin";

	/// <summary>
	/// Reported when a component's public surface exposes framework types that the merge internalizes;
	/// advisory, because the merge still produces a self-contained analyzer.
	/// </summary>
	const string PublicSurfaceFindingCode = "PSGFR41";

	/// <summary>
	/// Reported when the merged artifact still exposes framework types; the merge fails.
	/// </summary>
	const string PublicSurfaceBlockedCode = "PSGFR42";

	const string DefaultDiagnosticOrigin = "Purview.SourceGeneratorFramework";

	public static int Run(string[] args, TextWriter error, ILogger? logger = null, TextWriter? output = null)
	{
		if (args.Length > 0 && string.Equals(args[0], TagCommand, StringComparison.Ordinal))
			return WriteContentTag(args, error, output);

		if (!TryParseMergeArguments(args, error, out var options))
			return 2;

		var componentPath = Path.GetFullPath(options.ComponentPath);
		var frameworkPath = Path.GetFullPath(options.FrameworkPath);
		var outputPath = Path.GetFullPath(options.OutputPath);

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

		foreach (var searchPath in options.SearchPaths)
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

			RepackOptions repackOptions = new()
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
				new ILRepack(repackOptions).Repack();
			}
			else
			{
				new ILRepack(repackOptions, logger).Repack();
			}

			RestoreCanonicalIsExternalInit(stagingPath, searchDirectories);

			// ILRepack's internalize is best-effort and cannot reach framework types the component's
			// own generators emit, so force self-containment deterministically and fail the build
			// rather than shipping an analyzer that leaks framework types. Ownership is seeded with
			// the framework assembly's own type identities: the framework's public surface is not
			// confined to the framework namespace, so the namespace heuristic alone is not enough.
			var internalization = FrameworkTypeInternalizer.Apply(
				stagingPath,
				searchDirectories,
				logger is not null
					? logger.Warn
					: CreateFindingWriter(error, options.PublicSurfaceSeverity, options.Origin),
				ownedNamespaces: Extend(FrameworkTypeInternalizer.DefaultOwnedNamespaces, options.OwnedNamespaces),
				ownedTypeFullNames: Extend(
					FrameworkTypeInternalizer.DefaultOwnedTypeFullNames,
					[.. FrameworkTypeInternalizer.CollectTypeFullNames(frameworkPath), .. options.OwnedTypeFullNames]
				)
			);

			if (internalization.PublicFrameworkTypesRemaining.Length > 0)
			{
				error.WriteLine(
					FormatDiagnostic(
						options.Origin ?? outputPath,
						PublicSurfaceSeverity.Error,
						PublicSurfaceBlockedCode,
						$"The merged component '{stagingPath}' still exposes public Purview.SourceGeneratorFramework types: {string.Join(", ", internalization.PublicFrameworkTypesRemaining)}. Only the component's own entry points may stay public; make the listed types or their declaring types non-public."
					)
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
	/// overwrite a merged assembly the compiler host has loaded. Configuration values participate
	/// through <c>--input-value</c>, so changing the ownership lists or the reporting options re-runs
	/// the merge instead of reusing an artifact (and a report) produced with other options.
	/// </summary>
	static int WriteContentTag(string[] args, TextWriter error, TextWriter? output = null)
	{
		StringBuilder builder = new();
		for (var index = 1; index < args.Length; index++)
		{
			var argument = args[index];

			if (string.Equals(argument, InputValueSwitch, StringComparison.Ordinal))
			{
				if (++index >= args.Length)
				{
					error.WriteLine($"The {InputValueSwitch} option requires a value.");
					return 2;
				}

				builder.Append(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(args[index]))));
				builder.Append('\n');
				continue;
			}

			var path = Path.GetFullPath(argument);
			if (!File.Exists(path))
			{
				error.WriteLine($"Merge input was not found: {path}");
				return 3;
			}

			builder.Append(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))));
			builder.Append('\n');
		}

		(output ?? Console.Out).WriteLine(
			Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString())))
		);
		return 0;
	}

	/// <summary>
	/// Parses the merge command line. Positional arguments are the component, the framework assembly,
	/// the output path and any additional assembly search paths; options extend the internalization
	/// ownership and control how findings are reported back to the build.
	/// </summary>
	static bool TryParseMergeArguments(string[] args, TextWriter error, out MergeArguments options)
	{
		List<string> positional = [];
		List<string> ownedNamespaces = [];
		List<string> ownedTypeFullNames = [];
		var severity = PublicSurfaceSeverity.Message;
		string? origin = null;

		for (var index = 0; index < args.Length; index++)
		{
			var argument = args[index];

			if (argument.StartsWith("--", StringComparison.Ordinal))
			{
				if (
					!string.Equals(argument, OwnedNamespaceSwitch, StringComparison.Ordinal)
					&& !string.Equals(argument, OwnedTypeSwitch, StringComparison.Ordinal)
					&& !string.Equals(argument, PublicSurfaceSeveritySwitch, StringComparison.Ordinal)
					&& !string.Equals(argument, OriginSwitch, StringComparison.Ordinal)
				)
				{
					error.WriteLine(
						string.Equals(argument, InputValueSwitch, StringComparison.Ordinal)
							? $"The {InputValueSwitch} option is only valid with {TagCommand}."
							: $"Unknown option '{argument}'."
					);
					WriteUsage(error);
					options = default!;
					return false;
				}

				if (++index >= args.Length)
				{
					error.WriteLine($"The {argument} option requires a value.");
					WriteUsage(error);
					options = default!;
					return false;
				}

				var value = args[index];
				if (string.Equals(argument, OwnedNamespaceSwitch, StringComparison.Ordinal))
					ownedNamespaces.Add(value);
				else if (string.Equals(argument, OwnedTypeSwitch, StringComparison.Ordinal))
					ownedTypeFullNames.Add(value);
				else if (string.Equals(argument, OriginSwitch, StringComparison.Ordinal))
					origin = value;
				else if (!TryParseSeverity(value, out severity))
				{
					error.WriteLine(
						$"The {PublicSurfaceSeveritySwitch} option must be 'message', 'warning' or 'error', but was '{value}'."
					);
					WriteUsage(error);
					options = default!;
					return false;
				}

				continue;
			}

			positional.Add(argument);
		}

		if (positional.Count < 3)
		{
			error.WriteLine("The merge requires the component, the framework assembly and the output path.");
			WriteUsage(error);
			options = default!;
			return false;
		}

		options = new(
			positional[0],
			positional[1],
			positional[2],
			[.. positional.Skip(3)],
			[.. ownedNamespaces],
			[.. ownedTypeFullNames],
			severity,
			origin
		);
		return true;
	}

	static void WriteUsage(TextWriter error)
	{
		error.WriteLine(
			"Usage: Purview.SourceGeneratorFramework.MergeTool <component> <framework> <output> [<search path>...] [options]"
		);
		error.WriteLine(
			$"       Purview.SourceGeneratorFramework.MergeTool {TagCommand} <input> [--input-value <value>]..."
		);
		error.WriteLine(
			$"       Options: {OwnedNamespaceSwitch} <namespace>, {OwnedTypeSwitch} <type full name>, {PublicSurfaceSeveritySwitch} <message|warning|error|none>, {OriginSwitch} <path>"
		);
	}

	static bool TryParseSeverity(string value, out PublicSurfaceSeverity severity)
	{
		switch (value.ToUpperInvariant())
		{
			case "MESSAGE":
				severity = PublicSurfaceSeverity.Message;
				return true;
			case "WARNING":
				severity = PublicSurfaceSeverity.Warning;
				return true;
			case "ERROR":
				severity = PublicSurfaceSeverity.Error;
				return true;
			case "NONE":
				severity = PublicSurfaceSeverity.None;
				return true;
			default:
				severity = PublicSurfaceSeverity.Message;
				return false;
		}
	}

	/// <summary>
	/// Builds the sink the internalizer reports findings through. With a logger (in-repo tests) the
	/// findings stay structured; from MSBuild they are written in the canonical
	/// <c>origin : warning CODE: text</c> form so the build surfaces them in the Error List and in CI
	/// annotations, or as plain text when the caller keeps the default message severity.
	/// </summary>
	static Action<string> CreateFindingWriter(TextWriter error, PublicSurfaceSeverity severity, string? origin)
	{
		if (severity == PublicSurfaceSeverity.None)
			return static _ => { };

		if (severity == PublicSurfaceSeverity.Message && origin is null)
			return message => error.WriteLine(message);

		// The build's error stream is the only channel the merge has to report findings, so it must
		return message => error.WriteLine(FormatDiagnostic(origin, severity, PublicSurfaceFindingCode, message));
	}

	static string FormatDiagnostic(string? origin, PublicSurfaceSeverity severity, string code, string message)
	{
#pragma warning disable IDE0072 // Add missing cases
		var category = severity switch
		{
			PublicSurfaceSeverity.Error => "error",
			PublicSurfaceSeverity.Warning => "warning",
			_ => "message",
		};
#pragma warning restore IDE0072 // Add missing cases

		return $"{origin ?? DefaultDiagnosticOrigin} : {category} {code}: {message}";
	}

	static ImmutableArray<string> Extend(ImmutableArray<string> defaults, ImmutableArray<string> extras) =>
		extras.IsDefaultOrEmpty ? defaults : [.. defaults, .. extras];

	/// <summary>
	/// Parsed merge command line.
	/// </summary>
	/// <param name="ComponentPath">The component assembly to merge.</param>
	/// <param name="FrameworkPath">The framework assembly to merge in.</param>
	/// <param name="OutputPath">The merged artifact to produce.</param>
	/// <param name="SearchPaths">Additional assembly search paths.</param>
	/// <param name="OwnedNamespaces">Namespace prefixes the build adds to the framework-owned set.</param>
	/// <param name="OwnedTypeFullNames">Type full names the build adds to the framework-owned set.</param>
	/// <param name="PublicSurfaceSeverity">How public-surface findings are reported to the build.</param>
	/// <param name="Origin">The build origin reported with a finding, so the IDE and CI can attribute it.</param>
	sealed record MergeArguments(
		string ComponentPath,
		string FrameworkPath,
		string OutputPath,
		ImmutableArray<string> SearchPaths,
		ImmutableArray<string> OwnedNamespaces,
		ImmutableArray<string> OwnedTypeFullNames,
		PublicSurfaceSeverity PublicSurfaceSeverity,
		string? Origin
	);

	/// <summary>
	/// How the merge reports a component whose public surface exposes framework types.
	/// </summary>
	enum PublicSurfaceSeverity
	{
		/// <summary>Plain text on the error stream; the historical behaviour.</summary>
		Message,

		/// <summary>An MSBuild warning with a diagnostic code.</summary>
		Warning,

		/// <summary>An MSBuild error; used by the merge for a leaked public framework type.</summary>
		Error,

		/// <summary>
		/// Findings are dropped. A leaked public framework type still fails the merge with an error,
		/// because that artifact is not self-contained.
		/// </summary>
		None,
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
