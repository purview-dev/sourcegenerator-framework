using Microsoft.CodeAnalysis.Diagnostics;

namespace Purview.SourceGeneratorFramework.Analyzers;

/// <summary>
/// Decides whether the project's framework implementation is merged into the shipped analyzer. That is
/// what makes a public surface over framework types a problem: the merge internalizes every framework
/// type, so a public member that references one is left with an unusable signature.
/// </summary>
static class FrameworkMergeFacts
{
	public const string IsRoslynComponentProperty = "IsRoslynComponent";
	public const string EmbedProperty = "PurviewEmbedSourceGeneratorFramework";
	public const string IsPackableProperty = "IsPackable";
	public const string MergeAnalyzerFilesProperty = "PurviewMergeSourceGeneratorFrameworkForAnalyzerFiles";

	/// <summary>
	/// Determines whether the project under analysis produces a merged, self-contained analyzer. This
	/// mirrors the gating the merge targets use, so the analyzers that depend on it agree with the merge.
	/// </summary>
	public static bool WillBeMerged(AnalyzerOptions options)
	{
		var analyzerOptions = options.AnalyzerConfigOptionsProvider.GlobalOptions;

		if (!IsExplicitlyTrue(analyzerOptions, IsRoslynComponentProperty))
			return false;

		// Merge disabled: only the framework's compile-time library opts out of embedding.
		if (IsExplicitlyFalse(analyzerOptions, EmbedProperty))
			return false;

		// A non-packable component that does not return a merged analyzer file is never merged.
		return !(
			IsExplicitlyFalse(analyzerOptions, IsPackableProperty)
			&& IsExplicitlyFalse(analyzerOptions, MergeAnalyzerFilesProperty)
		);
	}

	public static bool IsExplicitlyTrue(AnalyzerConfigOptions options, string propertyName) =>
		options.TryGetValue("build_property." + propertyName, out var value)
		&& string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);

	public static bool IsExplicitlyFalse(AnalyzerConfigOptions options, string propertyName) =>
		options.TryGetValue("build_property." + propertyName, out var value)
		&& string.Equals(value, "false", StringComparison.OrdinalIgnoreCase);
}
