using System.Reflection;
using System.Runtime.Loader;
using ILRepacking;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Mono.Cecil;

namespace Purview.SourceGeneratorFramework.MergeTool;

public sealed class MergeToolRunnerTests
{
	const string MarkerFullName = "System.Runtime.CompilerServices.IsExternalInit";

	const string FrameworkSource = """
		namespace System.Runtime.CompilerServices
		{
			public static class IsExternalInit;
		}

		namespace Fixture.Framework
		{
			public enum OptionKind
			{
				None,
				Enabled,
			}

			public sealed class Options
			{
				public OptionKind Kind { get; init; }
			}
		}
		""";

	const string ComponentSource = """
		namespace System.Runtime.CompilerServices
		{
			internal static class IsExternalInit;
		}

		namespace Fixture.Component
		{
			using Fixture.Framework;

			public static class Consumer
			{
				public static Options Create() => new() { Kind = OptionKind.Enabled };
			}

			public sealed class ComponentOptions
			{
				public int Value { get; init; }
			}
		}
		""";

	const string ComponentWithoutMarkerSource = """
		namespace Fixture.Component
		{
			using Fixture.Framework;

			public static class Consumer
			{
				public static Options Create() => new() { Kind = OptionKind.Enabled };
			}
		}
		""";

	[Test]
	public async Task GivenDuplicateIsExternalInit_MergeProducesCanonicalWarningFreeAssembly(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		cancellationToken.ThrowIfCancellationRequested();
		using TestWorkspace workspace = new();
		var frameworkPath = workspace.Compile("Fixture.Framework", FrameworkSource);
		var componentPath = workspace.Compile("Fixture.Component", ComponentSource, frameworkPath);
		var outputPath = workspace.GetPath("merged", "Fixture.Component.dll");
		TestLogger logger = new();

		// Act
		var exitCode = MergeToolRunner.Run(
			[componentPath, frameworkPath, outputPath, TestWorkspace.NetStandardReferenceDirectory],
			TextWriter.Null,
			logger
		);

		// Assert
		await Assert.That(exitCode).IsEqualTo(0);
		await Assert
			.That(
				logger.Warnings.Any(static warning =>
					warning.Contains(
						"Method reference is used with definition return type / parameter",
						StringComparison.Ordinal
					)
				)
			)
			.IsFalse();

		await AssertMergedMetadataAsync(outputPath);
		await AssertMergedAssemblyExecutesAsync(outputPath);
		using var original = AssemblyDefinition.ReadAssembly(componentPath);
		await Assert.That(original.MainModule.GetType(MarkerFullName)).IsNotNull();
		await Assert
			.That(
				Directory
					.EnumerateDirectories(
						Path.GetDirectoryName(outputPath)!,
						".purview-merge-*",
						SearchOption.TopDirectoryOnly
					)
					.Any()
			)
			.IsFalse();
	}

	[Test]
	public async Task GivenNoDuplicateIsExternalInit_MergeKeepsExistingCleanPath(CancellationToken cancellationToken)
	{
		// Arrange
		cancellationToken.ThrowIfCancellationRequested();
		using TestWorkspace workspace = new();
		var frameworkPath = workspace.Compile("Fixture.Framework", FrameworkSource);
		var componentPath = workspace.Compile("Fixture.Component", ComponentWithoutMarkerSource, frameworkPath);
		var outputPath = workspace.GetPath("merged", "Fixture.Component.dll");
		TestLogger logger = new();

		// Act
		var exitCode = MergeToolRunner.Run(
			[componentPath, frameworkPath, outputPath, TestWorkspace.NetStandardReferenceDirectory],
			TextWriter.Null,
			logger
		);

		// Assert
		await Assert.That(exitCode).IsEqualTo(0);
		await Assert.That(logger.Warnings).IsEmpty();
		await Assert.That(File.Exists(outputPath)).IsTrue();
	}

	static async Task AssertMergedMetadataAsync(string outputPath)
	{
		using var merged = AssemblyDefinition.ReadAssembly(outputPath);
		var markers = merged.MainModule.Types.Where(static type => type.FullName == MarkerFullName).ToArray();
		await Assert.That(markers).HasSingleItem();
		await Assert.That(markers[0].IsNotPublic).IsTrue();
		await Assert
			.That(merged.MainModule.AssemblyReferences.Any(static reference => reference.Name == "Fixture.Framework"))
			.IsFalse();

		var consumer = merged.MainModule.GetType("Fixture.Component.Consumer");
		var create = consumer.Methods.Single(static method => method.Name == "Create");
		var setter = create
			.Body.Instructions.Select(static instruction => instruction.Operand)
			.OfType<MethodReference>()
			.Single(static method => method.Name == "set_Kind");
		var modifier = (RequiredModifierType)setter.ReturnType;
		await Assert.That(modifier.ModifierType.FullName).IsEqualTo(MarkerFullName);
		await Assert.That(modifier.ModifierType.Scope).IsSameReferenceAs(merged.MainModule);

		var componentOptions = merged.MainModule.GetType("Fixture.Component.ComponentOptions");
		var componentSetter = componentOptions.Methods.Single(static method => method.Name == "set_Value");
		var componentModifier = (RequiredModifierType)componentSetter.ReturnType;
		await Assert.That(componentModifier.ModifierType.FullName).IsEqualTo(MarkerFullName);
		await Assert.That(componentModifier.ModifierType.Scope).IsSameReferenceAs(merged.MainModule);
	}

	static async Task AssertMergedAssemblyExecutesAsync(string outputPath)
	{
		AssemblyLoadContext loadContext = new(
			name: nameof(GivenDuplicateIsExternalInit_MergeProducesCanonicalWarningFreeAssembly),
			isCollectible: true
		);
		try
		{
			await using var stream = File.OpenRead(outputPath);
			var loaded = loadContext.LoadFromStream(stream);
			var created = loaded
				.GetType("Fixture.Component.Consumer", throwOnError: true)!
				.GetMethod("Create", BindingFlags.Public | BindingFlags.Static)!
				.Invoke(null, null);
			var kind = created!.GetType().GetProperty("Kind")!.GetValue(created);
			await Assert.That(kind!.ToString()).IsEqualTo("Enabled");
		}
		finally
		{
			loadContext.Unload();
		}
	}

	sealed class TestLogger : ILogger
	{
		public bool ShouldLogVerbose { get; set; }

		public List<string> Warnings { get; } = [];

		public void Error(string msg) { }

		public void Info(string msg) { }

		public void Verbose(string msg) { }

		public void Warn(string msg) => Warnings.Add(msg);
	}

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
			var packageRoot = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
			if (string.IsNullOrWhiteSpace(packageRoot))
			{
				packageRoot = Path.Combine(
					Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
					".nuget",
					"packages"
				);
			}

			var path = Path.Combine(packageRoot, "netstandard.library", "2.0.3", "build", "netstandard2.0", "ref");

			return Directory.Exists(path)
				? path
				: throw new DirectoryNotFoundException(
					$"The .NET Standard 2.0 reference directory was not found: {path}"
				);
		}
	}
}
