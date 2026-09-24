using System.Collections.Immutable;
using Mono.Cecil;

namespace Purview.SourceGeneratorFramework.MergeTool;

public sealed class FrameworkTypeInternalizerTests
{
	/// <summary>
	/// Mirrors the real framework shape: framework types live in the framework namespace, and the
	/// framework ships the Roslyn component interface used to identify component entry points.
	/// </summary>
	const string FixtureFrameworkSource = """
		namespace System.Runtime.CompilerServices
		{
			public static class IsExternalInit;
		}

		namespace Microsoft.CodeAnalysis
		{
			public interface IIncrementalGenerator;
		}

		namespace Purview.SourceGeneratorFramework
		{
			public sealed record TypeReference
			{
				public string Name { get; }

				public TypeReference(string name) => Name = name;
			}

			public sealed class CodeWriter
			{
				public void Write(string value) { }
			}

			public readonly record struct TypeIdentity(string Name, string Namespace);
		}
		""";

	/// <summary>
	/// Mirrors a component that leaks framework types publicly: a generated type library declared in
	/// the framework namespace (so it is never part of the merged framework assembly and ILRepack
	/// cannot internalize it), a component entry point in the same namespace that must stay public,
	/// and a public component member exposing a framework type.
	/// </summary>
	const string LeakingComponentSource = """
		namespace Purview.SourceGeneratorFramework
		{
			using Microsoft.CodeAnalysis;

			public static class TypeLibrary
			{
				public static readonly TypeIdentity TypeReference =
					new("TypeReference", "Purview.SourceGeneratorFramework");
			}

			public sealed class ComponentEntryPoint : IIncrementalGenerator
			{
			}
		}

		namespace Fixture.Component
		{
			public sealed class PublicSurface
			{
				public Purview.SourceGeneratorFramework.TypeReference Reference { get; } = new("Exposed");
			}
		}
		""";

	/// <summary>
	/// A component that references the <em>real</em> framework assembly: it publishes a framework-typed
	/// member and declares a Roslyn component entry point inside the framework namespace.
	/// </summary>
	const string RealFrameworkComponentSource = """
		namespace Microsoft.CodeAnalysis
		{
			public interface IIncrementalGenerator;
		}

		namespace Purview.SourceGeneratorFramework
		{
			public static class TypeLibrary
			{
				public static global::Purview.SourceGeneratorFramework.TypeIdentity Identity => default;
			}

			public sealed class ComponentEntryPoint : global::Microsoft.CodeAnalysis.IIncrementalGenerator
			{
			}
		}
		""";

	[Test]
	public async Task Apply_GivenOwnedPublicTypes_InternalizesThemAndKeepsComponentsPublic(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		cancellationToken.ThrowIfCancellationRequested();
		using TestWorkspace workspace = new();
		var frameworkPath = workspace.Compile("Fixture.Framework", FixtureFrameworkSource);
		var componentPath = workspace.Compile("Fixture.Component", LeakingComponentSource, frameworkPath);
		List<string> warnings = [];

		// Act
		var report = FrameworkTypeInternalizer.Apply(
			componentPath,
			[workspace.GetPath("Fixture.Component")],
			warnings.Add,
			ownedNamespaces: ["Purview.SourceGeneratorFramework"]
		);

		// Assert
		await Assert.That(report.PublicFrameworkTypesRemaining).IsEmpty();
		await Assert.That(report.InternalizedTypeCount).IsGreaterThan(0);

		using var component = AssemblyDefinition.ReadAssembly(componentPath);
		var typeLibrary = component.MainModule.GetType("Purview.SourceGeneratorFramework.TypeLibrary");
		await Assert.That(typeLibrary).IsNotNull();
		await Assert.That(typeLibrary!.IsNotPublic).IsTrue();

		var entryPoint = component.MainModule.GetType("Purview.SourceGeneratorFramework.ComponentEntryPoint");
		await Assert.That(entryPoint).IsNotNull();
		await Assert.That(entryPoint!.IsPublic).IsTrue();

		await Assert
			.That(warnings)
			.Contains(static warning =>
				warning.Contains("Fixture.Component.PublicSurface", StringComparison.Ordinal)
				&& warning.Contains("Reference", StringComparison.Ordinal)
			);
	}

	[Test]
	public async Task Run_GivenComponentLeakingFrameworkTypes_ProducesSelfContainedMergedAssembly(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		cancellationToken.ThrowIfCancellationRequested();
		using TestWorkspace workspace = new();
		var frameworkPath = workspace.Compile("Fixture.Framework", FixtureFrameworkSource);
		var componentPath = workspace.Compile("Fixture.Component", LeakingComponentSource, frameworkPath);
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

		using var merged = AssemblyDefinition.ReadAssembly(outputPath);
		var publicFrameworkTypes = merged
			.MainModule.Types.SelectMany(MergeToolRunner.Flatten)
			.Where(static type =>
				type.Namespace is not null
				&& (
					type.Namespace.Equals("Purview.SourceGeneratorFramework", StringComparison.Ordinal)
					|| type.Namespace.StartsWith("Purview.SourceGeneratorFramework.", StringComparison.Ordinal)
				)
				&& (type.IsNested ? type.IsNestedPublic : type.IsPublic)
			)
			.Select(static type => type.FullName)
			.ToImmutableArray();

		// The only public framework-namespace type left must be the Roslyn component entry point:
		// Roslyn cannot instantiate non-public components, so entry points are never internalized.
		await Assert.That(publicFrameworkTypes).HasSingleItem();
		await Assert.That(publicFrameworkTypes[0]).IsEqualTo("Purview.SourceGeneratorFramework.ComponentEntryPoint");
		await Assert
			.That(logger.Warnings)
			.Contains(static warning =>
				warning.Contains("Fixture.Component.PublicSurface", StringComparison.Ordinal)
				&& warning.Contains("Reference", StringComparison.Ordinal)
			);

		var entryPoint = merged.MainModule.GetType("Purview.SourceGeneratorFramework.ComponentEntryPoint");
		await Assert.That(entryPoint).IsNotNull();
		await Assert.That(entryPoint!.IsPublic).IsTrue();
	}

	/// <summary>
	/// Merges the real framework assembly rather than a fixture stand-in, so the self-contained
	/// contract is asserted against the shipped metadata (and against the types the framework's own
	/// generators emit into the component).
	/// </summary>
	[Test]
	public async Task Run_GivenRealFrameworkAssembly_ProducesSelfContainedMergedAssembly(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		cancellationToken.ThrowIfCancellationRequested();
		using TestWorkspace workspace = new();
		var frameworkPath = typeof(Purview.SourceGeneratorFramework.TypeIdentity).Assembly.Location;
		var componentPath = workspace.Compile("Fixture.RealComponent", RealFrameworkComponentSource, frameworkPath);
		var outputPath = workspace.GetPath("merged", "Fixture.RealComponent.dll");
		TestLogger logger = new();

		// Act
		var exitCode = MergeToolRunner.Run(
			[
				componentPath,
				frameworkPath,
				outputPath,
				TestWorkspace.NetStandardReferenceDirectory,
				AppContext.BaseDirectory,
			],
			TextWriter.Null,
			logger
		);

		// Assert
		await Assert.That(exitCode).IsEqualTo(0);

		using var merged = AssemblyDefinition.ReadAssembly(outputPath);
		var publicFrameworkTypes = merged
			.MainModule.Types.SelectMany(MergeToolRunner.Flatten)
			.Where(static type => IsFrameworkOwnedNamespace(type.Namespace))
			.Where(static type => type.IsNested ? type.IsNestedPublic : type.IsPublic)
			.Select(static type => type.FullName)
			.ToImmutableArray();

		// Only the component's own Roslyn entry point stays public in the merged artifact.
		await Assert.That(publicFrameworkTypes).HasSingleItem();
		await Assert.That(publicFrameworkTypes[0]).IsEqualTo("Purview.SourceGeneratorFramework.ComponentEntryPoint");
	}

	static bool IsFrameworkOwnedNamespace(string? @namespace) =>
		@namespace is not null
		&& (
			@namespace.Equals("Purview.SourceGeneratorFramework", StringComparison.Ordinal)
			|| @namespace.StartsWith("Purview.SourceGeneratorFramework.", StringComparison.Ordinal)
		);
}
