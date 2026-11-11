using Microsoft.CodeAnalysis.Diagnostics;

namespace Purview.SourceGeneratorFramework.Testing;

/// <summary>
/// Executes a diagnostic analyzer against a test compilation.
/// </summary>
public sealed class DiagnosticAnalyzerTestRunner<TAnalyzer> : RoslynTestRunner
	where TAnalyzer : DiagnosticAnalyzer, new()
{
	/// <summary>
	/// Runs the analyzer against one source file.
	/// </summary>
	public Task<AnalyzerTestResult> RunAsync(
		string source,
		AnalyzerTestOptions? options = null,
		CancellationToken cancellationToken = default
	) => RunAsync([source], options, cancellationToken);

	/// <summary>
	/// Runs the analyzer against the supplied source files.
	/// </summary>
	public async Task<AnalyzerTestResult> RunAsync(
		IEnumerable<string> sources,
		AnalyzerTestOptions? options = null,
		CancellationToken cancellationToken = default
	)
	{
		options ??= new();
		using var testProject = CreateProject(sources, options, typeof(TAnalyzer).Assembly);
		var compilation = await testProject.Project.GetCompilationAsync(cancellationToken)
			?? throw new InvalidOperationException("Unable to create the test compilation.");
		var diagnostics = await WithAnalyzers(compilation, [new TAnalyzer()], options)
			.GetAnalyzerDiagnosticsAsync(cancellationToken);

		return new(diagnostics, compilation);
	}
}
