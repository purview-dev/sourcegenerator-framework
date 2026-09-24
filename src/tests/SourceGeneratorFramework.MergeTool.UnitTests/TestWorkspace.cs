using ILRepacking;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Purview.SourceGeneratorFramework.MergeTool;

/// <summary>
/// Captures merge tool log output for assertions.
/// </summary>
sealed class TestLogger : ILogger
{
	public bool ShouldLogVerbose { get; set; }

	public List<string> Warnings { get; } = [];

	public void Error(string msg) { }

	public void Info(string msg) { }

	public void Verbose(string msg) { }

	public void Warn(string msg) => Warnings.Add(msg);
}

/// <summary>
/// Compiles fixture assemblies into a temporary directory so merge tests can run the tool against
/// real component/framework inputs.
/// </summary>
sealed class TestWorkspace : IDisposable
{
	readonly string _directory = Path.Combine(
		Path.GetTempPath(),
		$"Purview.SourceGeneratorFramework.MergeTool.Tests.{Guid.NewGuid():N}"
	);

	public static string NetStandardReferenceDirectory { get; } = GetNetStandardReferenceDirectory();

	public TestWorkspace()
	{
		Directory.CreateDirectory(_directory);
	}

	public string Compile(string assemblyName, string source, params string[] additionalReferences)
	{
		var outputPath = GetPath(assemblyName, $"{assemblyName}.dll");
		Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

		var references = Directory
			.EnumerateFiles(NetStandardReferenceDirectory, "*.dll")
			.Select(static path => (MetadataReference)MetadataReference.CreateFromFile(path))
			.ToList();
		references.AddRange(additionalReferences.Select(static path => MetadataReference.CreateFromFile(path)));

		var compilation = CreateCompilation(assemblyName, source, references);
		Emit(compilation, outputPath);
		return outputPath;
	}

	static CSharpCompilation CreateCompilation(
		string assemblyName,
		string source,
		IEnumerable<MetadataReference> references
	) =>
		CSharpCompilation.Create(
			assemblyName,
			[
				CSharpSyntaxTree.ParseText(
					source,
					CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest)
				),
			],
			references,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
		);

	static void Emit(CSharpCompilation compilation, string outputPath)
	{
		using var output = File.Create(outputPath);
		var result = compilation.Emit(output);
		if (!result.Success)
		{
			throw new InvalidOperationException(string.Join(Environment.NewLine, result.Diagnostics));
		}
	}

	public string GetPath(params string[] parts) => parts.Aggregate(_directory, Path.Combine);

	public void Dispose()
	{
		if (Directory.Exists(_directory))
		{
			Directory.Delete(_directory, recursive: true);
		}
	}

	static string GetNetStandardReferenceDirectory()
	{
		var candidateRoots = new[]
		{
			Environment.GetEnvironmentVariable("NUGET_PACKAGES"),
			Environment.GetEnvironmentVariable("RestorePackagesPath"),
			Environment.GetEnvironmentVariable("NuGetPackageRoot"),
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages"),
		}
			.Where(static path => !string.IsNullOrWhiteSpace(path))
			.Select(static path => path!)
			.Select(static path => path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
			.Distinct(StringComparer.OrdinalIgnoreCase);

		foreach (var packageRoot in candidateRoots)
		{
			var path = Path.Combine(packageRoot, "netstandard.library", "2.0.3", "build", "netstandard2.0", "ref");

			if (Directory.Exists(path))
				return path;
		}

		throw new DirectoryNotFoundException(
			$"The .NET Standard 2.0 reference directory was not found under any known package roots: {string.Join(", ", candidateRoots)}"
		);
	}
}
