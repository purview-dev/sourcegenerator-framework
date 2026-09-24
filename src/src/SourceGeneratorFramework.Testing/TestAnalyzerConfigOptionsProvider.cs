using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Purview.SourceGeneratorFramework.Testing;

/// <summary>
/// Provides analyzer-config options to the generator driver without a real MSBuild evaluation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="TestAnalyzerConfigOptionsProvider"/> class.
/// </remarks>
/// <param name="options">The global options to expose.</param>
sealed class TestAnalyzerConfigOptionsProvider(Dictionary<string, string> options) : AnalyzerConfigOptionsProvider
{
	readonly TestAnalyzerConfigOptions _globalOptions = new(options);

	/// <inheritdoc />
	public override AnalyzerConfigOptions GlobalOptions => _globalOptions;

	/// <inheritdoc />
	public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => _globalOptions;

	/// <inheritdoc />
	public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => TestAnalyzerConfigOptions.Empty;

	sealed class TestAnalyzerConfigOptions(Dictionary<string, string> options) : AnalyzerConfigOptions
	{
		public static readonly TestAnalyzerConfigOptions Empty = new([]);

		readonly ImmutableDictionary<string, string> _options = options.ToImmutableDictionary();

		public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
		{
			if (_options.TryGetValue(key, out var found))
			{
				value = found;
				return true;
			}

			value = null;
			return false;
		}
	}
}
