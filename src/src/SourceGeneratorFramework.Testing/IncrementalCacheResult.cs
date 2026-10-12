using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Purview.SourceGeneratorFramework.Testing.Models;

namespace Purview.SourceGeneratorFramework.Testing;

/// <summary>
/// The sources and optional analyzer-config overrides for a single run of an incremental cache test.
/// </summary>
/// <param name="Sources">The source files for this run.</param>
/// <param name="AnalyzerConfig">
/// Optional analyzer-config (MSBuild property) overrides applied only for this run. Keys are used verbatim.
/// </param>
public sealed record IncrementalRunInput(
	IEnumerable<string> Sources,
	IEnumerable<(string Key, string Value)>? AnalyzerConfig = null
);

/// <summary>
/// A single run of an incremental cache test: the generator run result and its tracked pipeline steps.
/// </summary>
/// <param name="RunResult">The generator run result for this run.</param>
/// <param name="Steps">
/// The tracked incremental steps keyed by their tracking name (for example
/// <c>GetGenerationConfiguration</c>, <c>ForAttribute_MyAttribute</c>). Each step's
/// <c>IncrementalGeneratorRunStep.Outputs</c> carry an <c>IncrementalStepRunReason</c>
/// (<c>New</c>, <c>Modified</c>, <c>Cached</c>, <c>Unchanged</c>) proving whether that pipeline stage was
/// recomputed or reused.
/// </param>
public sealed record IncrementalCacheRun(
	GeneratorRunResult RunResult,
	ImmutableDictionary<string, ImmutableArray<IncrementalGeneratorRunStep>> Steps
)
{
	/// <summary>
	/// The framework log entries written by the generator during this run, in the order they were written.
	/// Empty when <see cref="SourceGeneratorTestOptions.EnableLogging"/> is disabled.
	/// </summary>
	/// <remarks>
	/// Scoped to the single run rather than the whole sequence, so a rerun whose stages all report
	/// <see cref="IncrementalStepRunReason.Cached"/> can be asserted to have logged nothing.
	/// </remarks>
	public ImmutableArray<LogEntry> LogEntries { get; init; } = [];
}

/// <summary>
/// The result of an incremental cache test run: one <see cref="IncrementalCacheRun"/> per input, produced by a
/// single shared <see cref="GeneratorDriver"/> so each run's step reasons reflect what changed since the
/// previous run.
/// </summary>
/// <param name="Runs">The per-run results, in input order.</param>
public sealed record IncrementalCacheResult(ImmutableArray<IncrementalCacheRun> Runs);
