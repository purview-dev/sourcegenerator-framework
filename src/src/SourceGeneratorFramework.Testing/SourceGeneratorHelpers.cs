using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Purview.SourceGeneratorFramework.Testing;

static class SourceGeneratorHelpers
{
	// Process-wide, keyed by assembly path. A MetadataReference owns an AssemblyMetadata, and Roslyn keys
	// its PEAssemblySymbol cache off that instance: handing every test compilation the *same* instance lets
	// Roslyn reuse one set of symbol tables across the whole run instead of rebuilding them per test. Test
	// processes are short lived and the referenced assemblies do not change underneath them, so the entries
	// are never invalidated.
	static readonly ConcurrentDictionary<string, MetadataReference> ReferenceCache = new(
		StringComparer.OrdinalIgnoreCase
	);

	public static readonly string[] TrustedAssemblies = ResolveTrustedAssemblies();

	public static ImmutableArray<MetadataReference> ResolveTrustedReferences { get; } =
		CreateMetadataReferences(TrustedAssemblies);

	static readonly string? GeneratorAssemblyPath = typeof(TypeIdentity).Assembly.Location;
	static readonly ImmutableArray<MetadataReference> CachedFilteredReferences = FilterGeneratorAssembly(
		ResolveTrustedReferences,
		GeneratorAssemblyPath
	);

	static readonly ImmutableHashSet<string> CachedFilteredReferencePaths = CollectDisplayPaths(
		CachedFilteredReferences
	);

	/// <summary>
	/// Gets the shared <see cref="MetadataReference"/> for an assembly path, creating it on first use.
	/// </summary>
	/// <remarks>
	/// Always go through this instead of calling <see cref="MetadataReference.CreateFromFile(string, MetadataReferenceProperties, DocumentationProvider)"/>
	/// directly: a fresh reference per test run gives Roslyn a fresh <c>AssemblyMetadata</c>, which defeats
	/// symbol reuse and makes every test compilation build its own symbol tables.
	/// </remarks>
	public static MetadataReference GetOrCreateReference(string path) =>
		ReferenceCache.GetOrAdd(path, static p => MetadataReference.CreateFromFile(p));

	public static CSharpCompilation CreateCompilation(
		IEnumerable<SyntaxTree> syntaxTrees,
		ImmutableArray<MetadataReference> references,
		SourceGeneratorTestOptions options
	)
	{
		return CSharpCompilation.Create(
			options.CompilationAssemblyName,
			syntaxTrees,
			references,
			new CSharpCompilationOptions(options.OutputKind).WithNullableContextOptions(options.NullableContextOptions)
		);
	}

	public static ImmutableArray<MetadataReference> ResolveReferences(
		SourceGeneratorTestOptions options,
		Assembly generatorAssembly
	)
	{
		if (generatorAssembly is null)
			throw new ArgumentNullException(nameof(generatorAssembly));

		if (options.AdditionalAssemblyTypes.IsDefaultOrEmpty && options.AdditionalReferences.IsDefaultOrEmpty)
			return CachedFilteredReferences;

		var builder = ImmutableArray.CreateBuilder<MetadataReference>(
			CachedFilteredReferences.Length
				+ options.AdditionalAssemblyTypes.Length
				+ options.AdditionalReferences.Length
		);

		builder.AddRange(CachedFilteredReferences);

		// References are shared instances now, so the same assembly named twice yields the same object.
		// Compilation.Create tolerates that, but Solution.AddMetadataReferences (the analyzer, code-fix and
		// refactoring path) rejects duplicate items, so the additions are deduplicated here.
		HashSet<string> addedPaths = new(StringComparer.OrdinalIgnoreCase);
		foreach (var type in options.AdditionalAssemblyTypes)
		{
			var location = type.Assembly.Location;

			// Dynamic and single-file assemblies have no location to reference.
			if (string.IsNullOrEmpty(location))
				continue;

			// Already in the trusted-platform set, so naming it again is a no-op.
			if (CachedFilteredReferencePaths.Contains(location) || !addedPaths.Add(location))
				continue;

			builder.Add(GetOrCreateReference(location));
		}

		// Deduplicated by instance, not by path: a caller may deliberately supply the same assembly twice
		// with different MetadataReferenceProperties (extern aliases), and those are distinct references.
		HashSet<MetadataReference> addedReferences = [];
		foreach (var reference in options.AdditionalReferences)
		{
			if (!addedReferences.Add(reference) || IsAlreadyTrusted(reference))
				continue;

			builder.Add(reference);
		}

		var references = builder.ToImmutable();
		options.PreprocessReferences?.Invoke(references);
		return references;
	}

	// Only skips a reference that is literally the object already in the trusted list: a caller-created
	// reference over the same file is a distinct instance and stays, preserving the previous behaviour.
	static bool IsAlreadyTrusted(MetadataReference reference) =>
		reference.Display is { Length: > 0 } display
		&& CachedFilteredReferencePaths.Contains(display)
		&& ReferenceCache.TryGetValue(display, out var cached)
		&& ReferenceEquals(cached, reference);

	public static string PrepareSource(string source, SourceGeneratorTestOptions options)
	{
		if (!options.IncludeDefaultNamespaces)
			return source;

		var namespaces = options.DefaultNamespaces.AddRange(options.AdditionalNamespaces);
		if (namespaces.IsDefaultOrEmpty)
			return source;

		StringBuilder builder = new(source.Length + (namespaces.Length * 20));
		foreach (var ns in namespaces)
		{
			builder.Append("using ").Append(ns).AppendLine(";");
		}
		builder.AppendLine();

		return builder.Append(source).ToString();
	}

	static string[] ResolveTrustedAssemblies()
	{
		var trusted = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty;
		if (!string.IsNullOrWhiteSpace(trusted))
		{
			// The host lists a path once per deps.json library that resolves to it, so an assembly
			// referenced twice by design — the generator double-reference pattern, once as an analyzer and
			// once as a normal reference — appears twice. Referencing the same file twice is never correct,
			// and Solution.AddMetadataReferences rejects it outright once the instances are shared.
			return
			[
				.. trusted
					.Split([Path.PathSeparator], StringSplitOptions.RemoveEmptyEntries)
					.Distinct(StringComparer.OrdinalIgnoreCase),
			];
		}

		// .NET Framework does not populate TRUSTED_PLATFORM_ASSEMBLIES; fall back to the loaded
		// framework assemblies so test compilations still have their core references.
		return
		[
			.. AppDomain
				.CurrentDomain.GetAssemblies()
				.Where(static assembly => !assembly.IsDynamic && !string.IsNullOrEmpty(assembly.Location))
				.Select(static assembly => assembly.Location)
				.Distinct(StringComparer.OrdinalIgnoreCase),
		];
	}

	static ImmutableArray<MetadataReference> CreateMetadataReferences(string[] paths)
	{
		if (paths.Length == 0)
			return [];

		var builder = ImmutableArray.CreateBuilder<MetadataReference>(paths.Length);
		foreach (var path in paths)
			builder.Add(GetOrCreateReference(path));

		return builder.ToImmutable();
	}

	static ImmutableHashSet<string> CollectDisplayPaths(ImmutableArray<MetadataReference> references)
	{
#pragma warning disable IDE0001 // Simplify Names
		var builder = ImmutableHashSet.CreateBuilder<string>(StringComparer.OrdinalIgnoreCase);
#pragma warning restore IDE0001 // Simplify Names
		foreach (var reference in references)
		{
			if (reference.Display is { Length: > 0 } display)
				builder.Add(display);
		}

		return builder.ToImmutable();
	}

	static ImmutableArray<MetadataReference> FilterGeneratorAssembly(
		ImmutableArray<MetadataReference> references,
		string? generatorAssemblyPath
	)
	{
		if (generatorAssemblyPath is null)
			return references;

		var builder = ImmutableArray.CreateBuilder<MetadataReference>(references.Length);
		foreach (var reference in references)
		{
			if (!string.Equals(reference.Display, generatorAssemblyPath, StringComparison.OrdinalIgnoreCase))
				builder.Add(reference);
		}

		return builder.ToImmutable();
	}
}
