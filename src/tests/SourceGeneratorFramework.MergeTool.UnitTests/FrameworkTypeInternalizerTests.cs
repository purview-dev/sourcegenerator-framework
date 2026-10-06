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

	/// <summary>
	/// A framework shape with nested namespace containers, mirroring the generated type library and
	/// the nested operator/enum groups the framework ships: every container below the root type is a
	/// public nested type, and Mono.Cecil reports an empty namespace for it.
	/// </summary>
	const string NestedFrameworkSource = """
		namespace Purview.SourceGeneratorFramework
		{
			public sealed class TypeIdentity
			{
				public TypeIdentity(string name, string @namespace) { }
			}

			public static class PurviewTypeLibrary
			{
				public static class System
				{
					public static readonly TypeIdentity String = new("String", "System");

					public static class Collections
					{
						public static class Generic
						{
							public static readonly TypeIdentity List =
								new("List", "System.Collections.Generic");
						}
					}
				}
			}
		}
		""";

	/// <summary>
	/// Mirrors the compiler-synthesised extension containers the C# compiler emits for extension
	/// blocks: nested public types inside an internal static class, and a public nested type inside a
	/// public component type that genuinely exposes a framework type.
	/// </summary>
	const string NestedComponentSource = """
		namespace Fixture.Component
		{
			internal static class InternalExtensionContainer
			{
				public sealed class NestedExtensionBlock
				{
					public Purview.SourceGeneratorFramework.TypeIdentity Identity => default;
				}
			}

			public class PublicSurface
			{
				public sealed class NestedExtensionBlock
				{
					public Purview.SourceGeneratorFramework.TypeIdentity Identity => default;
				}
			}
		}
		""";

	/// <summary>
	/// Mirrors a component's generated type library: a public static class in the component's own
	/// namespace, stamped by the framework's type-library generator, with nested namespace classes
	/// that expose framework type identities.
	/// </summary>
	const string GeneratedTypeLibrarySource = """
		using System.CodeDom.Compiler;

		namespace Fixture.Component
		{
			[GeneratedCode("TypeLibraryGenerator", "1.0.0-test")]
			public static partial class TypeLibrary
			{
				public static class System
				{
					[GeneratedCode("TypeLibraryGenerator", "1.0.0-test")]
					public static readonly Purview.SourceGeneratorFramework.TypeIdentity String = default;
				}
			}
		}
		""";

	/// <summary>
	/// A framework shape whose public surface is not confined to the framework namespace, mirroring the
	/// framework's own <c>Microsoft.CodeAnalysis.*Extensions</c> and <c>System.StringExtensions</c>
	/// extension classes: those types are only recognizable as framework-owned by assembly identity.
	/// </summary>
	const string ForeignNamespaceFrameworkSource = """
		namespace Fixture.Framework
		{
			public sealed class TypeReference
			{
				public string Name { get; }

				public TypeReference(string name) => Name = name;
			}

			public static class TypeReferenceExtensions
			{
				public static string Describe(this TypeReference reference) => reference.Name;
			}
		}
		""";

	/// <summary>
	/// A framework shape with a non-sealed class usable as a generic constraint.
	/// </summary>
	const string ConstraintFrameworkSource = """
		namespace Purview.SourceGeneratorFramework
		{
			public class TypeIdentity
			{
				public string Name { get; set; }
			}
		}
		""";

	/// <summary>
	/// A component whose public generic constraints reference framework types.
	/// </summary>
	const string ConstraintComponentSource = """
		namespace Fixture.Component
		{
			public sealed class Constrained<T>
				where T : Purview.SourceGeneratorFramework.TypeIdentity
			{
			}

			public static class ConstrainedMethods
			{
				public static void Use<T>()
					where T : Purview.SourceGeneratorFramework.TypeIdentity
				{
				}
			}
		}
		""";

	/// <summary>
	/// A component that grants another assembly access to its internals.
	/// </summary>
	const string ComponentWithInternalsGrantSource = """
		using System.Runtime.CompilerServices;

		[assembly: InternalsVisibleTo("Fixture.Component.Tests")]

		namespace Fixture.Component
		{
			public sealed class PublicType
			{
			}
		}
		""";

	/// <summary>
	/// A component carrying both a grant the merge copied in from the framework and the component's own
	/// grant to its companion code-fix assembly.
	/// </summary>
	const string ComponentWithMixedInternalsGrantsSource = """
		using System.Runtime.CompilerServices;

		[assembly: InternalsVisibleTo("Purview.SourceGeneratorFramework.UnitTests")]
		[assembly: InternalsVisibleTo("Fixture.Component.CodeFixers")]

		namespace Fixture.Component
		{
			public sealed class PublicType
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
		var frameworkPath = typeof(TypeIdentity).Assembly.Location;
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

	[Test]
	public async Task Apply_GivenNestedFrameworkTypes_InternalizesThemThroughTheDeclaringChain(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		cancellationToken.ThrowIfCancellationRequested();
		using TestWorkspace workspace = new();
		var frameworkPath = workspace.Compile("Fixture.NestedFramework", NestedFrameworkSource);
		List<string> warnings = [];

		// Act
		var report = FrameworkTypeInternalizer.Apply(
			frameworkPath,
			[workspace.GetPath("Fixture.NestedFramework")],
			warnings.Add
		);

		// Assert
		await Assert.That(report.PublicFrameworkTypesRemaining).IsEmpty();
		await Assert.That(report.PublicMembersExposingFrameworkTypes).IsEmpty();
		await Assert.That(warnings).IsEmpty();

		using var framework = AssemblyDefinition.ReadAssembly(frameworkPath);
		var typeLibrary = framework.MainModule.GetType("Purview.SourceGeneratorFramework.PurviewTypeLibrary");
		await Assert.That(typeLibrary).IsNotNull();
		await Assert.That(typeLibrary!.IsNotPublic).IsTrue();

		var system = framework.MainModule.GetType("Purview.SourceGeneratorFramework.PurviewTypeLibrary/System");
		await Assert.That(system).IsNotNull();
		await Assert.That(system!.IsNestedAssembly).IsTrue();

		var generic = framework.MainModule.GetType(
			"Purview.SourceGeneratorFramework.PurviewTypeLibrary/System/Collections/Generic"
		);
		await Assert.That(generic).IsNotNull();
		await Assert.That(generic!.IsNestedAssembly).IsTrue();
	}

	[Test]
	public async Task Apply_GivenNestedTypeOnlyReachableThroughInternalType_DoesNotReportIt(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		cancellationToken.ThrowIfCancellationRequested();
		using TestWorkspace workspace = new();
		var frameworkPath = workspace.Compile("Fixture.Framework", FixtureFrameworkSource);
		var componentPath = workspace.Compile("Fixture.Component", NestedComponentSource, frameworkPath);
		List<string> warnings = [];

		// Act
		FrameworkTypeInternalizer.Apply(
			componentPath,
			[workspace.GetPath("Fixture.Component")],
			warnings.Add,
			ownedNamespaces: ["Purview.SourceGeneratorFramework"]
		);

		// Assert
		await Assert
			.That(warnings)
			.DoesNotContain(static warning => warning.Contains("InternalExtensionContainer", StringComparison.Ordinal));
		await Assert
			.That(warnings)
			.Contains(static warning => warning.Contains("Fixture.Component.PublicSurface", StringComparison.Ordinal));
	}

	[Test]
	public async Task Apply_GivenFrameworkGeneratedTypeLibrary_InternalizesItWithoutReportingIt(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		cancellationToken.ThrowIfCancellationRequested();
		using TestWorkspace workspace = new();
		var frameworkPath = workspace.Compile("Fixture.Framework", FixtureFrameworkSource);
		var componentPath = workspace.Compile("Fixture.Component", GeneratedTypeLibrarySource, frameworkPath);
		List<string> warnings = [];

		// Act
		var report = FrameworkTypeInternalizer.Apply(
			componentPath,
			[workspace.GetPath("Fixture.Component")],
			warnings.Add,
			ownedNamespaces: ["Purview.SourceGeneratorFramework"]
		);

		// Assert
		await Assert.That(warnings).IsEmpty();
		await Assert.That(report.PublicMembersExposingFrameworkTypes).IsEmpty();

		using var component = AssemblyDefinition.ReadAssembly(componentPath);
		var typeLibrary = component.MainModule.GetType("Fixture.Component.TypeLibrary");
		await Assert.That(typeLibrary).IsNotNull();
		await Assert.That(typeLibrary!.IsNotPublic).IsTrue();

		var system = component.MainModule.GetType("Fixture.Component.TypeLibrary/System");
		await Assert.That(system).IsNotNull();
		await Assert.That(system!.IsNestedAssembly).IsTrue();
	}

	[Test]
	public async Task Apply_GivenFrameworkTypesOutsideTheFrameworkNamespace_InternalizesThemByName(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		cancellationToken.ThrowIfCancellationRequested();
		using TestWorkspace workspace = new();
		var frameworkPath = workspace.Compile("Fixture.ForeignFramework", ForeignNamespaceFrameworkSource);
		List<string> warnings = [];

		// Act
		var report = FrameworkTypeInternalizer.Apply(
			frameworkPath,
			[workspace.GetPath("Fixture.ForeignFramework")],
			warnings.Add,
			ownedNamespaces: [],
			ownedTypeFullNames:
			[
				.. FrameworkTypeInternalizer.DefaultOwnedTypeFullNames,
				.. FrameworkTypeInternalizer.CollectTypeFullNames(frameworkPath),
			]
		);

		// Assert
		await Assert.That(report.PublicFrameworkTypesRemaining).IsEmpty();
		await Assert.That(warnings).IsEmpty();

		using var framework = AssemblyDefinition.ReadAssembly(frameworkPath);
		await Assert.That(framework.MainModule.GetType("Fixture.Framework.TypeReference")!.IsNotPublic).IsTrue();
		await Assert
			.That(framework.MainModule.GetType("Fixture.Framework.TypeReferenceExtensions")!.IsNotPublic)
			.IsTrue();
	}

	[Test]
	public async Task Apply_GivenInternalsGrants_StripsThemFromTheMergedArtifact(CancellationToken cancellationToken)
	{
		// Arrange
		cancellationToken.ThrowIfCancellationRequested();
		using TestWorkspace workspace = new();
		var componentPath = workspace.Compile("Fixture.Component", ComponentWithInternalsGrantSource);

		// Act
		var report = FrameworkTypeInternalizer.Apply(
			componentPath,
			[workspace.GetPath("Fixture.Component")],
			ownedNamespaces: ["Purview.SourceGeneratorFramework"]
		);

		// Assert
		await Assert.That(report.StrippedInternalsGrantCount).IsEqualTo(1);

		using var component = AssemblyDefinition.ReadAssembly(componentPath);
		await Assert
			.That(
				component.CustomAttributes.Any(static attribute =>
					attribute.AttributeType.FullName == "System.Runtime.CompilerServices.InternalsVisibleToAttribute"
				)
			)
			.IsFalse();
	}

	// The merge copies the framework assembly's own grants into the artifact, and those must go: a
	// shipped analyzer is not the framework's assembly. A grant the *component* authored is different -
	// it is what lets a companion code-fix component read the generator's internal diagnostic identity.
	// Stripping it made the merged analyzer behave differently from the unmerged bin output the author
	// compiled and tested against, so the code fix threw FieldAccessException only in the compiler host.
	[Test]
	public async Task Apply_GivenFrameworkGrants_StripsOnlyThoseAndKeepsTheComponentsOwn(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		cancellationToken.ThrowIfCancellationRequested();
		using TestWorkspace workspace = new();
		var componentPath = workspace.Compile("Fixture.Component", ComponentWithMixedInternalsGrantsSource);

		// Act
		var report = FrameworkTypeInternalizer.Apply(
			componentPath,
			[workspace.GetPath("Fixture.Component")],
			ownedNamespaces: ["Purview.SourceGeneratorFramework"],
			frameworkInternalsGrants: ["Purview.SourceGeneratorFramework.UnitTests"]
		);

		// Assert
		await Assert.That(report.StrippedInternalsGrantCount).IsEqualTo(1);

		using var component = AssemblyDefinition.ReadAssembly(componentPath);
		var grants = component
			.CustomAttributes.Where(static attribute =>
				attribute.AttributeType.FullName == "System.Runtime.CompilerServices.InternalsVisibleToAttribute"
			)
			.Select(static attribute => (string)attribute.ConstructorArguments[0].Value)
			.ToArray();

		await Assert.That(grants).IsEquivalentTo(["Fixture.Component.CodeFixers"]);
	}

	[Test]
	public async Task Apply_GivenGenericConstraintsExposingFrameworkTypes_ReportsThem(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		cancellationToken.ThrowIfCancellationRequested();
		using TestWorkspace workspace = new();
		var frameworkPath = workspace.Compile("Fixture.Framework", ConstraintFrameworkSource);
		var componentPath = workspace.Compile("Fixture.Component", ConstraintComponentSource, frameworkPath);
		List<string> warnings = [];

		// Act
		var report = FrameworkTypeInternalizer.Apply(
			componentPath,
			[workspace.GetPath("Fixture.Component")],
			warnings.Add,
			ownedNamespaces: ["Purview.SourceGeneratorFramework"]
		);

		// Assert
		await Assert
			.That(report.PublicMembersExposingFrameworkTypes)
			.Contains("Fixture.Component.Constrained`1.generic parameter T");
		await Assert
			.That(report.PublicMembersExposingFrameworkTypes)
			.Contains("Fixture.Component.ConstrainedMethods.Use<T>");
		await Assert
			.That(warnings)
			.Contains(static warning =>
				warning.Contains(
					"'Fixture.Component.ConstrainedMethods' is public and exposes",
					StringComparison.Ordinal
				)
			);
	}

	static bool IsFrameworkOwnedNamespace(string? @namespace) =>
		@namespace is not null
		&& (
			@namespace.Equals("Purview.SourceGeneratorFramework", StringComparison.Ordinal)
			|| @namespace.StartsWith("Purview.SourceGeneratorFramework.", StringComparison.Ordinal)
		);
}
