using Purview.SourceGeneratorFramework.Analyzers;
using Purview.SourceGeneratorFramework.Testing;
using Purview.SourceGeneratorFramework.Testing.TUnit;

namespace Purview.SourceGeneratorFramework.CodeFixers;

public sealed class MakeComponentSurfaceNonPublicCodeFixProviderTests
	: TUnitCodeFixTestBase<ComponentPublicSurfaceAnalyzer, MakeComponentSurfaceNonPublicCodeFixProvider>
{
	const string ExposingSurfaceSource = """
		namespace Fixture.Component
		{
			public sealed class Consumer
			{
				public Purview.SourceGeneratorFramework.TypeIdentity Identity => default;
			}
		}
		""";

	static readonly (string, string)[] MergedComponentProperties =
	[
		("build_property.IsRoslynComponent", "true"),
		("build_property.PurviewEmbedSourceGeneratorFramework", "true"),
		("build_property.IsPackable", "true"),
	];

	static CodeFixTestOptions Options(string equivalenceKey) =>
		new CodeFixTestOptions
		{
			EquivalenceKey = equivalenceKey,
			AdditionalAssemblyTypes = [typeof(TypeIdentity)],
		}.WithAnalyzerConfigOptions(MergedComponentProperties);

	[Test]
	public async Task PublicMemberExposingFrameworkType_BecomesInternal(CancellationToken cancellationToken)
	{
		// Arrange
		// Act
		var result = await ApplyCodeFixAsync(
			ExposingSurfaceSource,
			Options(MakeComponentSurfaceNonPublicCodeFixProvider.MemberEquivalenceKey),
			cancellationToken
		);

		// Assert
		await Assert.That(result).HasDiagnostic(ComponentPublicSurfaceAnalyzer.DiagnosticId);
		await Assert
			.That(result.FixedSource)
			.Contains("internal Purview.SourceGeneratorFramework.TypeIdentity Identity");
	}

	[Test]
	public async Task PublicTypeExposingFrameworkType_BecomesInternal(CancellationToken cancellationToken)
	{
		// Arrange
		// Act
		var result = await ApplyCodeFixAsync(
			ExposingSurfaceSource,
			Options(MakeComponentSurfaceNonPublicCodeFixProvider.TypeEquivalenceKey),
			cancellationToken
		);

		// Assert
		await Assert.That(result).HasDiagnostic(ComponentPublicSurfaceAnalyzer.DiagnosticId);
		await Assert.That(result.FixedSource).Contains("sealed class Consumer");
		await Assert.That(result.FixedSource).DoesNotContain("public sealed class Consumer");
	}
}
