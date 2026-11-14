using Purview.SourceGeneratorFramework.Analyzers;
using Purview.SourceGeneratorFramework.Testing;
using Purview.SourceGeneratorFramework.Testing.TUnit;

namespace Purview.SourceGeneratorFramework.CodeFixers;

public sealed class PreferMinimalCodeWriterOverloadCodeFixProviderTests
	: TUnitCodeFixTestBase<PreferMinimalCodeWriterOverloadAnalyzer, PreferMinimalCodeWriterOverloadCodeFixProvider>
{
	static readonly CodeFixTestOptions Options = new()
	{
		EquivalenceKey = PreferMinimalCodeWriterOverloadCodeFixProvider.EquivalenceKey,
		AdditionalAssemblyTypes = [typeof(CodeWriter), typeof(GenerationSettings), typeof(PropertyDeclarationOptions)],
	};

	[Test]
	public async Task Property_WithBareOptions_UsesMinimalOverload(CancellationToken cancellationToken)
	{
		const string source = """
			using Purview.SourceGeneratorFramework;

			class Emitter
			{
				public void Emit()
				{
					var writer = new CodeWriter(new GenerationSettings("G"));
					writer.Property(new PropertyDeclarationOptions("Name", TypeReference.Create<string>(), TypeDeclarationAccessibility.Public));
				}
			}
			""";

		var result = await ApplyCodeFixAsync(source, Options, cancellationToken);

		await Assert.That(result).HasDiagnostic(PreferMinimalCodeWriterOverloadAnalyzer.Rule.Id);
		await Assert
			.That(result.FixedSource)
			.Contains("writer.Property(\"Name\", TypeReference.Create<string>(), TypeDeclarationAccessibility.Public)");
	}

	[Test]
	public async Task Property_WithTargetTypedOptions_UsesMinimalOverload(CancellationToken cancellationToken)
	{
		const string source = """
			using Purview.SourceGeneratorFramework;

			class Emitter
			{
				public void Emit()
				{
					var writer = new CodeWriter(new GenerationSettings("G"));
					writer.Property(new("Name", TypeReference.Create<string>(), TypeDeclarationAccessibility.Public));
				}
			}
			""";

		var result = await ApplyCodeFixAsync(source, Options, cancellationToken);

		await Assert
			.That(result.FixedSource)
			.Contains("writer.Property(\"Name\", TypeReference.Create<string>(), TypeDeclarationAccessibility.Public)");
	}
}
