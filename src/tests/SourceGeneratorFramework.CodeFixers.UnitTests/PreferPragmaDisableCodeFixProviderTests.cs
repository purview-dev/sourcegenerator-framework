using Purview.SourceGeneratorFramework.Analyzers;
using Purview.SourceGeneratorFramework.Testing;
using Purview.SourceGeneratorFramework.Testing.TUnit;

namespace Purview.SourceGeneratorFramework.CodeFixers;

public sealed class PreferPragmaDisableCodeFixProviderTests
	: TUnitCodeFixTestBase<PreferPragmaDisableAnalyzer, PreferPragmaDisableCodeFixProvider>
{
	static readonly CodeFixTestOptions Options = new()
	{
		EquivalenceKey = PreferPragmaDisableCodeFixProvider.EquivalenceKey,
		AdditionalAssemblyTypes = [typeof(CodeWriter), typeof(GenerationSettings)],
	};

	[Test]
	public async Task Line_WithPragmaWarningDisable_UsesPragmaDisable(CancellationToken cancellationToken)
	{
		const string source = """
			using Purview.SourceGeneratorFramework;

			class Emitter
			{
				public void Emit()
				{
					var writer = new CodeWriter(new GenerationSettings("G"));
					writer.Line("#pragma warning disable CS1591 CS0618");
				}
			}
			""";

		var result = await ApplyCodeFixAsync(source, Options, cancellationToken);

		await Assert.That(result).HasDiagnostic(PreferPragmaDisableAnalyzer.Rule.Id);
		await Assert.That(result.FixedSource).Contains("writer.PragmaDisable(\"CS1591\", \"CS0618\")");
	}

	[Test]
	public async Task Line_WithPragmaWarningRestore_DoesNotRegisterFix(CancellationToken cancellationToken)
	{
		const string source = """
			using Purview.SourceGeneratorFramework;

			class Emitter
			{
				public void Emit()
				{
					var writer = new CodeWriter(new GenerationSettings("G"));
					writer.Line("#pragma warning restore CS1591");
				}
			}
			""";

		await Assert
			.That(async () => await ApplyCodeFixAsync(source, Options, cancellationToken))
			.Throws<InvalidOperationException>();
	}
}
