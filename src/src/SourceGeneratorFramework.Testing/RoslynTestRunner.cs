using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Purview.SourceGeneratorFramework.Testing;

/// <summary>
/// Provides shared Roslyn project creation for analyzer and code fix test runners.
/// </summary>
public abstract class RoslynTestRunner
{
	/// <summary>
	/// Creates a compilation-with-analyzers using the configured analyzer options.
	/// </summary>
	protected static CompilationWithAnalyzers WithAnalyzers(
		Compilation compilation,
		ImmutableArray<DiagnosticAnalyzer> analyzers,
		SourceGeneratorTestOptions options
	)
	{
		if (compilation is null)
			throw new ArgumentNullException(nameof(compilation));
		if (options is null)
			throw new ArgumentNullException(nameof(options));

		if (options.AnalyzerOptions is not null && options.CompilationWithAnalyzersOptions is not null)
		{
			throw new ArgumentException(
				$"{nameof(options.AnalyzerOptions)} and {nameof(options.CompilationWithAnalyzersOptions)} cannot be provided at the same time.",
				nameof(options)
			);
		}

		if (options.CompilationWithAnalyzersOptions is null && options.AnalyzerOptions is null)
		{
			var analyzerConfigOptions = BuildAnalyzerConfig(options);
			if (analyzerConfigOptions.Count > 0)
			{
				return compilation.WithAnalyzers(
					analyzers,
					new AnalyzerOptions(
						ImmutableArray<AdditionalText>.Empty,
						new TestAnalyzerConfigOptionsProvider(analyzerConfigOptions)
					)
				);
			}
		}

		// If the caller provided CompilationWithAnalyzersOptions, use that; otherwise, use AnalyzerOptions.
		return options.CompilationWithAnalyzersOptions is not null
			? compilation.WithAnalyzers(analyzers, options.CompilationWithAnalyzersOptions)
			: compilation.WithAnalyzers(analyzers, options.AnalyzerOptions);
	}

	static Dictionary<string, string> BuildAnalyzerConfig(SourceGeneratorTestOptions options)
	{
		Dictionary<string, string> analyzerOptions = new(options.AnalyzerConfigOptions)
		{
			[SourceGeneratorBuildProperties.ValidateCodeWriterScopes] = options.ValidateCodeWriterScopes.ToString(),
			[SourceGeneratorBuildProperties.EnableLogging] = options.EnableLogging.ToString(),
		};

		foreach (var pair in options.AnalyzerConfigOptions)
		{
			if (!pair.Key.StartsWith(SourceGeneratorBuildProperties.BuildProperty, StringComparison.Ordinal))
				analyzerOptions[SourceGeneratorBuildProperties.BuildProperty + pair.Key] = pair.Value;
		}

		if (options.DisableSourceGeneratorPropertyName is not null && options.DisableSourceGeneratorValue is not null)
		{
			var disablePropertyName = options.DisableSourceGeneratorPropertyName;
			if (!disablePropertyName.StartsWith(SourceGeneratorBuildProperties.BuildProperty, StringComparison.Ordinal))
				disablePropertyName = SourceGeneratorBuildProperties.BuildProperty + disablePropertyName;

			analyzerOptions[disablePropertyName] = options.DisableSourceGeneratorValue.Value.ToString();
		}

		return analyzerOptions;
	}

	/// <summary>
	/// Creates a project containing the supplied sources.
	/// </summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage(
		"Maintainability",
		"CA1506:Avoid excessive class coupling",
		Justification = "Constructing a Roslyn workspace necessarily coordinates Roslyn project model types."
	)]
	protected static TestProject CreateProject(
		IEnumerable<string> sources,
		SourceGeneratorTestOptions options,
		Assembly componentAssembly
	)
	{
		if (sources is null)
			throw new ArgumentNullException(nameof(sources));
		if (options is null)
			throw new ArgumentNullException(nameof(options));
		if (componentAssembly is null)
			throw new ArgumentNullException(nameof(componentAssembly));

		if (!options.AdditionalSources.IsDefaultOrEmpty)
			sources = sources.Concat(options.AdditionalSources);

		AdhocWorkspace workspace = new();
		var projectId = ProjectId.CreateNewId();
		var solution = workspace
			.CurrentSolution.AddProject(
				projectId,
				options.CompilationAssemblyName,
				options.CompilationAssemblyName,
				LanguageNames.CSharp
			)
			.WithProjectParseOptions(projectId, new CSharpParseOptions(options.LanguageVersion))
			.WithProjectCompilationOptions(
				projectId,
				new CSharpCompilationOptions(options.OutputKind).WithNullableContextOptions(
					options.NullableContextOptions
				)
			)
			.AddMetadataReferences(projectId, SourceGeneratorHelpers.ResolveReferences(options, componentAssembly));

		var documentIds = ImmutableArray.CreateBuilder<DocumentId>();
		var index = 0;
		foreach (var source in sources)
		{
			var documentId = DocumentId.CreateNewId(projectId);
			documentIds.Add(documentId);
			solution = solution.AddDocument(
				documentId,
				$"Test{++index}.cs",
				SourceText.From(PrepareSource(source, options), System.Text.Encoding.UTF8)
			);
		}

		if (documentIds.Count == 0)
			throw new ArgumentException("At least one source is required.", nameof(sources));

		// Return a disposable wrapper that owns the workspace and project.
		return new(workspace, solution.GetProject(projectId)!, documentIds.ToImmutable());
	}

	static string PrepareSource(string source, SourceGeneratorTestOptions options) =>
		SourceGeneratorHelpers.PrepareSource(source, options);

	/// <summary>
	/// Owns the workspace and project created for a test run.
	/// </summary>
	protected sealed class TestProject(
		AdhocWorkspace workspace,
		Project project,
		ImmutableArray<DocumentId> documentIds
	) : IDisposable
	{
		/// <summary>
		/// Gets the test project.
		/// </summary>
		public Project Project { get; } = project;

		/// <summary>
		/// Gets the source document identifiers in input order.
		/// </summary>
		public ImmutableArray<DocumentId> DocumentIds { get; } = documentIds;

		/// <inheritdoc />
		public void Dispose() => workspace.Dispose();
	}
}
