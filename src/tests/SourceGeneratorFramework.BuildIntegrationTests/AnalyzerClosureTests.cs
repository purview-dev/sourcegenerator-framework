using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text.Json;

namespace Purview.SourceGeneratorFramework;

/// <summary>
/// Builds the checked-in <c>Purview.ComponentClosureFixture</c> with the real MSBuild toolchain and
/// asserts the two invariants Defect A violated:
/// <list type="number">
/// <item>a merged component's bin output is self-sufficient, so a dependent code-fix component can
/// resolve its component dependency when the IDE loads it from bin; and</item>
/// <item>the analyzer closure returned by <c>GetSourceGeneratorAnalyzerFiles</c> has no unresolvable
/// assembly reference.</item>
/// </list>
/// </summary>
[NotInParallel(nameof(AnalyzerClosureTests))]
public sealed class AnalyzerClosureTests
{
	const string Configuration = "Debug";
	const string FrameworkAssemblyFileName = "Purview.SourceGeneratorFramework.dll";

	static readonly string RepoRoot = FindRepositoryRoot();
	static readonly string FixtureRoot = Path.Combine(
		RepoRoot,
		"src",
		"tests",
		"Fixtures",
		"Purview.ComponentClosureFixture"
	);

	[Test]
	public async Task MergedComponentBinOutputs_AreSelfSufficient(CancellationToken cancellationToken)
	{
		await BuildAsync(ProjectPath("Fixture.Consumer"), cancellationToken);

		List<string> problems = new();
		foreach (var component in new[] { "Fixture.Generator", "Fixture.Generator.CodeFixers" })
		{
			var binDirectory = BinDirectory(component);
			if (!File.Exists(Path.Combine(binDirectory, FrameworkAssemblyFileName)))
			{
				problems.Add(
					$"The bin output of '{component}' ('{binDirectory}') does not contain "
						+ $"'{FrameworkAssemblyFileName}'. A code-fix component loaded from its own bin "
						+ "cannot resolve its framework dependency at runtime."
				);
			}
		}

		await Assert.That(string.Join(Environment.NewLine, problems)).IsEmpty();
	}

	[Test]
	public async Task ComponentClosure_ContainsNoUnresolvableAssemblyReferences(CancellationToken cancellationToken)
	{
		List<string> problems = new();

		foreach (var component in new[] { "Fixture.Generator", "Fixture.Generator.CodeFixers" })
		{
			var closure = await ResolveAnalyzerClosureAsync(ProjectPath(component), cancellationToken);
			if (closure.Count == 0)
			{
				problems.Add($"'{component}' returned an empty analyzer closure.");
				continue;
			}

			var availableAssemblies = closure
				.Select(static path => Path.GetFileNameWithoutExtension(path))
				.ToHashSet(StringComparer.OrdinalIgnoreCase);

			if (availableAssemblies.Contains(Path.GetFileNameWithoutExtension(FrameworkAssemblyFileName)))
			{
				problems.Add(
					$"'{component}' returned the loose '{FrameworkAssemblyFileName}' in its analyzer "
						+ "closure. Merged components must be self-contained."
				);
			}

			foreach (var file in closure)
			{
				foreach (var reference in ReadAssemblyReferenceNames(file))
				{
					if (IsPermittedReference(reference) || availableAssemblies.Contains(reference))
						continue;

					problems.Add(
						$"'{Path.GetFileName(file)}' references '{reference}', which is neither in the "
							+ $"returned analyzer closure ({string.Join(", ", availableAssemblies.Order(StringComparer.OrdinalIgnoreCase))}) "
							+ "nor a host-provided assembly."
					);
				}
			}
		}

		await Assert.That(string.Join(Environment.NewLine, problems)).IsEmpty();
	}

	[Test]
	public async Task ProjectReferenceConsumer_ReceivesBuildTransitiveCompilerVisibleProperties(
		CancellationToken cancellationToken
	)
	{
		await BuildAsync(ProjectPath("Fixture.ProjectReferenceConsumer"), cancellationToken);

		var editorConfig = Path.Combine(
			FixtureRoot,
			"Fixture.ProjectReferenceConsumer",
			"obj",
			Configuration,
			"netstandard2.0",
			"Fixture.ProjectReferenceConsumer.GeneratedMSBuildEditorConfig.editorconfig"
		);

		await Assert.That(File.Exists(editorConfig)).IsTrue();
		await Assert
			.That(await File.ReadAllTextAsync(editorConfig, cancellationToken))
			.Contains("build_property.Fixture_TransitiveProperty");
	}

	[Test]
	public async Task GeneratorVisibleProperty_NotCompilerVisible_ReportsPsgf0003(CancellationToken cancellationToken)
	{
		var output = await RunAllowFailureAsync(
			ProjectPath("Fixture.Generator"),
			"-p:FixtureGeneratorVisiblePropertyName=Fixture_MissingInBuildAssets",
			cancellationToken
		);

		await Assert.That(output).Contains("PSGF0003");
	}

	[Test]
	public async Task GeneratorVisibleProperty_DeclaredInBuildAssets_Succeeds(CancellationToken cancellationToken)
	{
		var output = await RunAllowFailureAsync(
			ProjectPath("Fixture.Generator"),
			"-p:FixtureGeneratorVisiblePropertyName=Fixture_Disable",
			cancellationToken
		);

		await Assert.That(output).DoesNotContain("PSGF0003");
	}

	[Test]
	public async Task ConsumerAnalyzerSet_ExcludesUnmergedComponentAnalyzer(CancellationToken cancellationToken)
	{
		var probe = Path.Combine(FixtureRoot, "VsAnalyzerSimulation.targets");

		var output = await RunAsync(
			"dotnet",
			[
				"msbuild",
				ProjectPath("Fixture.Consumer"),
				"-t:ResolveProjectReferences;ResolveSourceGeneratorProjectReferenceFiles;GetFixtureAnalyzerItems",
				"-getTargetResult:GetFixtureAnalyzerItems",
				$"-p:Configuration={Configuration}",
				$"-p:CustomAfterMicrosoftCommonTargets={probe}",
				"-nologo",
			],
			Path.Combine(FixtureRoot, "Fixture.Consumer"),
			cancellationToken
		);

		var start = output.IndexOf('{', StringComparison.Ordinal);
		var end = output.LastIndexOf('}');
		if (start < 0 || end <= start)
		{
			throw new InvalidOperationException(
				$"Unable to locate the target-result JSON in the msbuild output:{Environment.NewLine}{output}"
			);
		}

		using var document = JsonDocument.Parse(output[start..(end + 1)]);
		var items = document
			.RootElement.GetProperty("TargetResults")
			.GetProperty("GetFixtureAnalyzerItems")
			.GetProperty("Items");

		List<string> analyzerPaths = new();
		foreach (var item in items.EnumerateArray())
			analyzerPaths.Add(item.GetProperty("Identity").GetString()!);

		List<string> problems = new();
		if (!analyzerPaths.Any(static path => path.Contains("purview-merged", StringComparison.OrdinalIgnoreCase)))
			problems.Add("The analyzer set does not contain the merged artifact.");

		var unmerged = analyzerPaths
			.Where(static path =>
				path.Contains(@"Fixture.Generator\bin", StringComparison.OrdinalIgnoreCase)
				&& path.EndsWith("Fixture.Generator.dll", StringComparison.OrdinalIgnoreCase)
			)
			.ToArray();
		if (unmerged.Length > 0)
		{
			problems.Add(
				"The analyzer set still contains the unmerged component assembly, which the Visual "
					+ "Studio project system adds: "
					+ string.Join(", ", unmerged)
			);
		}

		await Assert.That(string.Join(Environment.NewLine, problems)).IsEmpty();
	}

	static bool IsPermittedReference(string name) =>
		name.StartsWith("Microsoft.CodeAnalysis", StringComparison.Ordinal)
		|| name.StartsWith("System.Composition", StringComparison.Ordinal)
		|| name.StartsWith("System.", StringComparison.Ordinal)
		|| name.Equals("netstandard", StringComparison.Ordinal)
		|| name.Equals("mscorlib", StringComparison.Ordinal)
		|| name.Equals("System.Collections.Immutable", StringComparison.Ordinal)
		|| name.Equals("System.Memory", StringComparison.Ordinal)
		|| name.Equals("System.Threading.Tasks.Extensions", StringComparison.Ordinal);

	static List<string> ReadAssemblyReferenceNames(string path)
	{
		using var stream = File.OpenRead(path);
		using PEReader peReader = new(stream);
		var metadata = peReader.GetMetadataReader();

		List<string> names = new();
		foreach (var handle in metadata.AssemblyReferences)
			names.Add(metadata.GetString(metadata.GetAssemblyReference(handle).Name));

		return names;
	}

	static async Task<List<string>> ResolveAnalyzerClosureAsync(string projectPath, CancellationToken cancellationToken)
	{
		var output = await RunAsync(
			"dotnet",
			[
				"msbuild",
				projectPath,
				"-t:GetSourceGeneratorAnalyzerFiles",
				"-getTargetResult:GetSourceGeneratorAnalyzerFiles",
				$"-p:Configuration={Configuration}",
				"-nologo",
			],
			Path.GetDirectoryName(projectPath)!,
			cancellationToken
		);

		var start = output.IndexOf('{', StringComparison.Ordinal);
		var end = output.LastIndexOf('}');
		if (start < 0 || end <= start)
		{
			throw new InvalidOperationException(
				$"Unable to locate the target-result JSON in the msbuild output:{Environment.NewLine}{output}"
			);
		}

		using var document = JsonDocument.Parse(output[start..(end + 1)]);
		var items = document
			.RootElement.GetProperty("TargetResults")
			.GetProperty("GetSourceGeneratorAnalyzerFiles")
			.GetProperty("Items");

		List<string> files = new();
		foreach (var item in items.EnumerateArray())
			files.Add(item.GetProperty("Identity").GetString()!);

		return files;
	}

	static Task<string> BuildAsync(string projectPath, CancellationToken cancellationToken) =>
		RunAsync(
			"dotnet",
			["build", projectPath, "-c", Configuration, "-v:minimal"],
			Path.GetDirectoryName(projectPath)!,
			cancellationToken
		);

	static async Task<(int ExitCode, string Output, string Error)> RunCoreAsync(
		string fileName,
		string[] arguments,
		string workingDirectory,
		CancellationToken cancellationToken
	)
	{
		// The test assembly runs once per target framework in separate processes, and every test
		// builds the same fixture projects. Serialize them across processes so concurrent MSBuild
		// invocations cannot corrupt the fixture's obj/bin output.
		using var buildLock = await AcquireFixtureBuildLockAsync(cancellationToken);

		ProcessStartInfo startInfo = new(fileName)
		{
			WorkingDirectory = workingDirectory,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true,
		};
		foreach (var argument in arguments)
			startInfo.ArgumentList.Add(argument);

		startInfo.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
		startInfo.Environment["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"] = "1";

		using var process = Process.Start(startInfo)!;
		var standardOutput = process.StandardOutput.ReadToEndAsync(cancellationToken);
		var standardError = process.StandardError.ReadToEndAsync(cancellationToken);

		await process.WaitForExitAsync(cancellationToken);
		var output = await standardOutput;
		var error = await standardError;

		return (process.ExitCode, output, error);
	}

	static async Task<string> RunAsync(
		string fileName,
		string[] arguments,
		string workingDirectory,
		CancellationToken cancellationToken
	)
	{
		var (exitCode, output, error) = await RunCoreAsync(fileName, arguments, workingDirectory, cancellationToken);
		if (exitCode != 0)
		{
			throw new InvalidOperationException(
				$"'{fileName} {string.Join(' ', arguments)}' failed with exit code {exitCode}."
					+ $"{Environment.NewLine}{output}{Environment.NewLine}{error}"
			);
		}

		return output;
	}

	static async Task<string> RunAllowFailureAsync(
		string projectPath,
		string extraProperty,
		CancellationToken cancellationToken
	)
	{
		var (_, output, error) = await RunCoreAsync(
			"dotnet",
			["build", projectPath, "-c", Configuration, "-v:minimal", extraProperty],
			Path.GetDirectoryName(projectPath)!,
			cancellationToken
		);
		return output + Environment.NewLine + error;
	}

	static string ProjectPath(string projectName) => Path.Combine(FixtureRoot, projectName, projectName + ".csproj");

	static string BinDirectory(string projectName) =>
		Path.Combine(FixtureRoot, projectName, "bin", Configuration, "netstandard2.0");

	static async Task<FileStream> AcquireFixtureBuildLockAsync(CancellationToken cancellationToken)
	{
		var lockPath = Path.Combine(Path.GetTempPath(), "purview-analyzer-closure-fixture.lock");
		while (true)
		{
			cancellationToken.ThrowIfCancellationRequested();
			try
			{
				return new FileStream(
					lockPath,
					FileMode.OpenOrCreate,
					FileAccess.ReadWrite,
					FileShare.None,
					1,
					FileOptions.DeleteOnClose
				);
			}
			catch (IOException)
			{
				await Task.Delay(250, cancellationToken);
			}
		}
	}

	static string FindRepositoryRoot()
	{
		DirectoryInfo? directory = new(AppContext.BaseDirectory);
		while (directory is not null)
		{
			if (File.Exists(Path.Combine(directory.FullName, "src", "SourceGeneratorFramework.slnx")))
				return directory.FullName;

			directory = directory.Parent;
		}

		throw new InvalidOperationException(
			$"Unable to locate the repository root above '{AppContext.BaseDirectory}'."
		);
	}
}
