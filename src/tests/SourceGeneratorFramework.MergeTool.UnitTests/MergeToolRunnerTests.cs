using System.Reflection;
using System.Runtime.Loader;
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
			public sealed class TypeReference
			{
				public string Name { get; }

				public TypeReference(string name) => Name = name;
			}

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

				public static string Describe() => new TypeReference("Merged").Name;
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

				public static string Describe() => new TypeReference("Merged").Name;
			}
		}
		""";

	/// <summary>
	/// A component that uses framework types only inside method bodies, so its public surface stays
	/// free of framework types and the merge reports no findings for it.
	/// </summary>
	const string InternalUsageComponentSource = """
		namespace Fixture.Component
		{
			using Fixture.Framework;

			public static class Consumer
			{
				public static string Describe()
				{
					var options = new Options { Kind = OptionKind.Enabled };
					return new TypeReference(options.Kind.ToString()).Name;
				}
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
		await Assert.That(File.Exists(outputPath)).IsTrue();

		// ILRepack must stay quiet (this test covers the no-duplicate normalizer path)...
		await Assert
			.That(logger.Warnings)
			.DoesNotContain(static warning =>
				warning.Contains(
					"Method reference is used with definition return type / parameter",
					StringComparison.Ordinal
				)
			);

		// ...while the framework identity seeding now makes the fixture framework assembly
		// framework-owned, so the component's public member returning a framework type is reported even
		// though the fixture framework does not live in the framework namespace. The report is grouped
		// by the real declaring type.
		await Assert
			.That(logger.Warnings)
			.Contains(static warning =>
				warning.Contains("'Fixture.Component.Consumer' is public and exposes", StringComparison.Ordinal)
			);
	}

	[Test]
	public async Task Run_GivenComponentUsingFrameworkTypesInternally_ReportsNoFindings(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		cancellationToken.ThrowIfCancellationRequested();
		using TestWorkspace workspace = new();
		var frameworkPath = workspace.Compile("Fixture.Framework", FrameworkSource);
		var componentPath = workspace.Compile("Fixture.Component", InternalUsageComponentSource, frameworkPath);
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
	}

	[Test]
	public async Task Run_GivenWarningSeverityAndOrigin_WritesMsBuildWarning(CancellationToken cancellationToken)
	{
		// Arrange
		cancellationToken.ThrowIfCancellationRequested();
		using TestWorkspace workspace = new();
		var frameworkPath = workspace.Compile("Fixture.Framework", FrameworkSource);
		var componentPath = workspace.Compile("Fixture.Component", ComponentWithoutMarkerSource, frameworkPath);
		var outputPath = workspace.GetPath("merged", "Fixture.Component.dll");
		var origin = workspace.GetPath("Fixture.Component", "Component.cs");
		using StringWriter error = new();

		// Act
		var exitCode = MergeToolRunner.Run(
			[
				componentPath,
				frameworkPath,
				outputPath,
				TestWorkspace.NetStandardReferenceDirectory,
				"--public-surface-severity",
				"warning",
				"--origin",
				origin,
			],
			error
		);

		// Assert
		await Assert.That(exitCode).IsEqualTo(0);
		await Assert.That(error.ToString()).Contains($"{origin} : warning PSGFR41:");
	}

	[Test]
	public async Task Run_GivenDefaultSeverity_WritesPlainTextWithoutCode(CancellationToken cancellationToken)
	{
		// Arrange
		cancellationToken.ThrowIfCancellationRequested();
		using TestWorkspace workspace = new();
		var frameworkPath = workspace.Compile("Fixture.Framework", FrameworkSource);
		var componentPath = workspace.Compile("Fixture.Component", ComponentWithoutMarkerSource, frameworkPath);
		var outputPath = workspace.GetPath("merged", "Fixture.Component.dll");
		using StringWriter error = new();

		// Act
		var exitCode = MergeToolRunner.Run(
			[componentPath, frameworkPath, outputPath, TestWorkspace.NetStandardReferenceDirectory],
			error
		);

		// Assert
		await Assert.That(exitCode).IsEqualTo(0);
		await Assert.That(error.ToString()).Contains("is public and exposes");
		await Assert.That(error.ToString()).DoesNotContain("PSGFR41");
	}

	[Test]
	public async Task Run_GivenUnknownOption_ReturnsUsageError(CancellationToken cancellationToken)
	{
		// Arrange
		cancellationToken.ThrowIfCancellationRequested();
		using StringWriter error = new();

		// Act
		var exitCode = MergeToolRunner.Run(["--unknown"], error);

		// Assert
		await Assert.That(exitCode).IsEqualTo(2);
		await Assert.That(error.ToString()).Contains("Unknown option '--unknown'.");
	}

	[Test]
	public async Task Tag_GivenConfigurationValues_TracksThemInTheContentTag(CancellationToken cancellationToken)
	{
		// Arrange
		cancellationToken.ThrowIfCancellationRequested();
		using TestWorkspace workspace = new();
		var frameworkPath = workspace.Compile("Fixture.Framework", FrameworkSource);
		using StringWriter first = new();
		using StringWriter repeated = new();
		using StringWriter changed = new();

		// Act
		var firstExitCode = MergeToolRunner.Run(
			["--tag", frameworkPath, "--input-value", "namespaces=a"],
			TextWriter.Null,
			output: first
		);
		var repeatedExitCode = MergeToolRunner.Run(
			["--tag", frameworkPath, "--input-value", "namespaces=a"],
			TextWriter.Null,
			output: repeated
		);
		var changedExitCode = MergeToolRunner.Run(
			["--tag", frameworkPath, "--input-value", "namespaces=b"],
			TextWriter.Null,
			output: changed
		);

		// Assert
		await Assert.That(firstExitCode).IsEqualTo(0);
		await Assert.That(repeatedExitCode).IsEqualTo(0);
		await Assert.That(changedExitCode).IsEqualTo(0);
		await Assert.That(first.ToString()).IsEqualTo(repeated.ToString());
		await Assert.That(first.ToString()).IsNotEqualTo(changed.ToString());
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

		var mergedTypeReference = merged.MainModule.GetType("Fixture.Framework.TypeReference");
		await Assert.That(mergedTypeReference).IsNotNull();
		await Assert.That(mergedTypeReference.IsNotPublic).IsTrue();

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
			var described = loaded
				.GetType("Fixture.Component.Consumer", throwOnError: true)!
				.GetMethod("Describe", BindingFlags.Public | BindingFlags.Static)!
				.Invoke(null, null);
			await Assert.That(described).IsEqualTo("Merged");
		}
		finally
		{
			loadContext.Unload();
		}
	}
}
