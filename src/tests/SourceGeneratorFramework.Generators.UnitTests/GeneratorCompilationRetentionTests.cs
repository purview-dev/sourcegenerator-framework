using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Purview.SourceGeneratorFramework.Generators;

/// <summary>
/// Proves that a generator's pipeline nodes do not root the compilations of earlier runs.
/// </summary>
/// <remarks>
/// <para>
/// Retention is the half of the <c>PSGFR33</c> story that a cache test cannot see, and it is worth being
/// precise about which half it is. A node value holding an <see cref="ISymbol"/> compares by reference, so
/// a fresh compilation makes the node report <c>Modified</c> and the new value <em>replaces</em> the old
/// one — which releases the previous compilation rather than rooting it. The cost of a symbol in a node is
/// therefore a guaranteed cache miss and the downstream re-execution that follows, not a leak.
/// </para>
/// <para>
/// A leak needs the opposite shape: a value that compares <em>equal</em> while still referencing a symbol,
/// so Roslyn keeps the stale cached instance and the compilation behind it. These tests pin that it does
/// not happen, for the generator whose pipeline was rewritten to build its model inside the transform and
/// for the one that still carries a symbol through a combine.
/// </para>
/// </remarks>
public class GeneratorCompilationRetentionTests
{
	const string TypeLibrarySource = """
		using Purview.SourceGeneratorFramework;
		using Purview.SourceGeneratorFramework.Generators;

		namespace Test;

		[GenerateTypeLibrary]
		static partial class TypeLibraryModel
		{
			[TypeRef("Test")]
			static readonly TypeIdentity MyAttribute = default!;
		}
		""";

	[Test]
	public async Task TypeLibraryGenerator_GivenRepeatedRuns_DoesNotRetainTheFirstCompilation()
	{
		// Arrange / Act
		var (firstCompilation, driver) = RunAndDiscardFirstCompilation<TypeLibraryGenerator>(TypeLibrarySource);

		for (var attempt = 0; attempt < 3; attempt++)
		{
			GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
			GC.WaitForPendingFinalizers();
		}

		// Assert - the pipeline builds the whole target inside the ForAttributeWithMetadataName transform,
		// so the cached node value is a GeneratorResult and never a symbol. Nothing roots the compilation.
		await Assert.That(firstCompilation.IsAlive).IsFalse();

		GC.KeepAlive(driver);
	}

	const string AttributeDataSource = """
		using Purview.SourceGeneratorFramework.Generators;

		namespace Test;

		[Generate(typeof(System.Attribute))]
		public readonly partial record struct AttributeData(bool Enabled);
		""";

	[Test]
	public async Task AttributeDataModelGenerator_GivenRepeatedRuns_DoesNotRetainTheFirstCompilation()
	{
		// Arrange / Act
		var (firstCompilation, driver) = RunAndDiscardFirstCompilation<AttributeDataModelGenerator>(
			AttributeDataSource
		);

		for (var attempt = 0; attempt < 3; attempt++)
		{
			GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
			GC.WaitForPendingFinalizers();
		}

		// Assert
		await Assert.That(firstCompilation.IsAlive).IsFalse();

		GC.KeepAlive(driver);
	}

	/// <summary>
	/// Runs the generator over a sequence of fresh compilations built from identical source, discards every
	/// strong reference to the first, and returns a weak reference to it alongside the still-live driver.
	/// </summary>
	/// <remarks>
	/// The driver has to come back with it: it is the thing that holds the node tables, and letting it fall
	/// out of scope would make the compilation collectable no matter what the pipeline retains.
	/// </remarks>
	[MethodImpl(MethodImplOptions.NoInlining)]
	static (WeakReference FirstCompilation, GeneratorDriver Driver) RunAndDiscardFirstCompilation<TGenerator>(
		string source,
		int runs = 4
	)
		where TGenerator : IIncrementalGenerator, new()
	{
		GeneratorDriver driver = CSharpGeneratorDriver.Create(
			[new TGenerator().AsSourceGenerator()],
			driverOptions: new GeneratorDriverOptions(
				IncrementalGeneratorOutputKind.None,
				trackIncrementalGeneratorSteps: true
			)
		);

		WeakReference firstReference;
		(firstReference, driver) = RunFirst(driver, source);

		for (var run = 1; run < runs; run++)
		{
			// A new compilation per run, as an IDE produces on every keystroke. The source is identical, so
			// every stage reports Unchanged and Roslyn keeps the value cached from the *first* run.
			driver = driver.RunGeneratorsAndUpdateCompilation(TestCompilation.Create(source), out _, out _);
		}

		return (firstReference, driver);
	}

	/// <summary>
	/// Performs the first run in its own frame, so the only strong reference to that compilation dies with
	/// the method rather than living on as a local in the caller.
	/// </summary>
	[MethodImpl(MethodImplOptions.NoInlining)]
	static (WeakReference FirstCompilation, GeneratorDriver Driver) RunFirst(GeneratorDriver driver, string source)
	{
		var first = TestCompilation.Create(source);

		// The updated compilation is derived from the input and would root it, so it is discarded too.
		return (new WeakReference(first), driver.RunGeneratorsAndUpdateCompilation(first, out _, out _));
	}
}
