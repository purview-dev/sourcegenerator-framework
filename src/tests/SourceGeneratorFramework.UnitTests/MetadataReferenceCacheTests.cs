using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Purview.SourceGeneratorFramework;

/// <summary>
/// Locks in the reference-sharing contract of <c>SourceGeneratorHelpers</c>.
/// </summary>
/// <remarks>
/// Each <see cref="MetadataReference"/> owns an <c>AssemblyMetadata</c>, and Roslyn keys its assembly-symbol
/// reuse off that instance. Creating references per run makes every test compilation build its own symbol
/// tables, which is what made harness-based suites peak in the gigabytes. These tests fail if a refactor
/// reintroduces a per-call <c>MetadataReference.CreateFromFile</c>.
/// </remarks>
public class MetadataReferenceCacheTests
{
	[Test]
	public async Task GetOrCreateReference_GivenTheSamePath_ReturnsTheSameInstance()
	{
		// Arrange
		var path = typeof(object).Assembly.Location;

		// Act
		var first = SourceGeneratorHelpers.GetOrCreateReference(path);
		var second = SourceGeneratorHelpers.GetOrCreateReference(path);

		// Assert
		await Assert.That(second).IsSameReferenceAs(first);
	}

	[Test]
	public async Task ResolveTrustedReferences_GivenATrustedPath_SharesTheCachedInstance()
	{
		// Arrange
		var path = typeof(object).Assembly.Location;
		var cached = SourceGeneratorHelpers.GetOrCreateReference(path);

		// Act
		var trusted = SourceGeneratorHelpers.ResolveTrustedReferences.FirstOrDefault(reference =>
			string.Equals(reference.Display, path, StringComparison.OrdinalIgnoreCase)
		);

		// Assert
		await Assert.That(trusted).IsSameReferenceAs(cached);
	}

	[Test]
	public async Task ResolveReferences_GivenAdditionalAssemblyTypes_ReusesEveryReferenceAcrossRuns()
	{
		// Arrange - the slow path, taken whenever any additional assembly is configured.
		SourceGeneratorTestOptions options = new()
		{
			AdditionalAssemblyTypes = [typeof(Compilation), typeof(SourceGeneratorTestOptions)],
		};

		// Act
		var first = SourceGeneratorHelpers.ResolveReferences(options, typeof(Compilation).Assembly);
		var second = SourceGeneratorHelpers.ResolveReferences(options, typeof(Compilation).Assembly);

		// Assert - not merely equal: the same objects, so Roslyn can reuse the bound symbols.
		await Assert.That(second.Length).IsEqualTo(first.Length);
		await Assert.That(first.Where((reference, index) => !ReferenceEquals(reference, second[index]))).IsEmpty();
	}

	[Test]
	public async Task ResolveReferences_GivenAnAssemblyAlreadyTrusted_DoesNotAddItAgain()
	{
		// Arrange
		SourceGeneratorTestOptions options = new() { AdditionalAssemblyTypes = [typeof(object)] };

		// Act
		var references = SourceGeneratorHelpers.ResolveReferences(options, typeof(Compilation).Assembly);

		// Assert
		await Assert
			.That(
				references.Count(reference =>
					string.Equals(
						reference.Display,
						typeof(object).Assembly.Location,
						StringComparison.OrdinalIgnoreCase
					)
				)
			)
			.IsEqualTo(1);
	}

	[Test]
	public async Task ResolveReferences_GivenRepeatedAssemblyTypes_ProducesNoDuplicateInstances()
	{
		// Arrange - Solution.AddMetadataReferences rejects duplicate items outright.
		SourceGeneratorTestOptions options = new()
		{
			AdditionalAssemblyTypes = [typeof(SourceGeneratorTestOptions), typeof(DriverRunResult)],
			AdditionalReferences = [SourceGeneratorHelpers.GetOrCreateReference(typeof(Compilation).Assembly.Location)],
		};

		// Act
		var references = SourceGeneratorHelpers.ResolveReferences(options, typeof(Compilation).Assembly);

		// Assert
		await Assert.That(references.Distinct().Count()).IsEqualTo(references.Length);
	}

	[Test]
	public async Task ResolveTrustedAssemblies_ContainsNoDuplicatePaths()
	{
		// Act
		var duplicates = SourceGeneratorHelpers
			.TrustedAssemblies.GroupBy(static path => path, StringComparer.OrdinalIgnoreCase)
			.Where(static group => group.Count() > 1)
			.Select(static group => group.Key);

		// Assert
		await Assert.That(duplicates).IsEmpty();
	}

	[Test]
	public async Task ResolveReferences_GivenAdditionalAssemblies_StillInvokesPreprocessReferences()
	{
		// Arrange
		ImmutableArray<MetadataReference> observed = default;
		SourceGeneratorTestOptions options = new()
		{
			AdditionalAssemblyTypes = [typeof(SourceGeneratorTestOptions)],
			PreprocessReferences = references => observed = references,
		};

		// Act
		var resolved = SourceGeneratorHelpers.ResolveReferences(options, typeof(Compilation).Assembly);

		// Assert
		await Assert.That(observed.IsDefault).IsFalse();
		await Assert.That(observed.SequenceEqual(resolved)).IsTrue();
	}
}
