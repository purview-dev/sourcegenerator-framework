using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Purview.SourceGeneratorFramework.Analyzers;

public static class AnalyzerTestHelpers
{
	public static async Task<IEnumerable<Diagnostic>> GetAnalyzerDiagnosticsAsync(
		this DiagnosticAnalyzer analyzer,
		string source,
		CancellationToken cancellationToken = default
	) => await GetAnalyzerDiagnosticsAsync(analyzer, source, null, false, cancellationToken);

	/// <summary>
	/// Runs the analyzer against the supplied source, optionally exposing <c>build_property.*</c>
	/// values through the analyzer config options and adding a reference to
	/// <c>Purview.SourceGeneratorFramework.dll</c>.
	/// </summary>
	public static async Task<IEnumerable<Diagnostic>> GetAnalyzerDiagnosticsAsync(
		this DiagnosticAnalyzer analyzer,
		string source,
		IReadOnlyDictionary<string, string>? buildProperties,
		bool referenceSourceGeneratorFramework,
		CancellationToken cancellationToken = default
	)
	{
		var compilation = CreateTestCompilation(source, referenceSourceGeneratorFramework);

		var analyzerOptions = buildProperties is null
			? null
			: TestAnalyzerConfigOptions.CreateAnalyzerOptions(buildProperties);

		var compilationWithAnalyzers = analyzerOptions is null
			? compilation.WithAnalyzers([analyzer])
			: compilation.WithAnalyzers([analyzer], analyzerOptions);

		return await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync(cancellationToken);
	}

	static CSharpCompilation CreateTestCompilation(string source, bool referenceSourceGeneratorFramework)
	{
		var syntaxTree = CSharpSyntaxTree.ParseText(source);
		List<MetadataReference> references = new()
		{
			MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
			MetadataReference.CreateFromFile(typeof(Compilation).Assembly.Location),
			MetadataReference.CreateFromFile(typeof(CSharpCompilation).Assembly.Location),
		};
		if (referenceSourceGeneratorFramework)
		{
			references.Add(MetadataReference.CreateFromFile(typeof(CodeWriter).Assembly.Location));
		}

		CSharpCompilationOptions compilationOptions = new(OutputKind.DynamicallyLinkedLibrary);

		return CSharpCompilation.Create("TestAssembly", [syntaxTree], references, compilationOptions);
	}
}

/// <summary>
/// Minimal <see cref="AnalyzerConfigOptionsProvider"/> that exposes a fixed set of
/// <c>build_property.*</c> values to analyzers in an in-memory test compilation.
/// </summary>
static class TestAnalyzerConfigOptions
{
	public static AnalyzerOptions CreateAnalyzerOptions(IReadOnlyDictionary<string, string> buildProperties) =>
		new(ImmutableArray<AdditionalText>.Empty, CreateProvider(buildProperties));

	public static AnalyzerConfigOptionsProvider CreateProvider(IReadOnlyDictionary<string, string> buildProperties) =>
		new Provider(new Options(ImmutableDictionary.CreateRange(StringComparer.Ordinal, buildProperties)));

	sealed class Provider(AnalyzerConfigOptions options) : AnalyzerConfigOptionsProvider
	{
		public override AnalyzerConfigOptions GlobalOptions => options;

		public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => options;

		public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => options;
	}

	sealed class Options(ImmutableDictionary<string, string> values) : AnalyzerConfigOptions
	{
		public override bool TryGetValue(
			string key,
			[System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? value
		)
		{
			if (values.TryGetValue(key, out var resolved))
			{
				value = resolved;
				return true;
			}

			value = null;
			return false;
		}
	}
}
