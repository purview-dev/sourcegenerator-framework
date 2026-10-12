using System.Text;
using BenchmarkDotNet.Attributes;
using Purview.SourceGeneratorFramework.Testing;

namespace Purview.SourceGeneratorFramework.Benchmarks;

[MemoryDiagnoser]
public class SourceGeneratorTestRunnerBenchmarks
{
	[Params(1, 10, 100)]
	public int ClassCount { get; set; }

	[Params(false, true)]
	public bool CompileToAssembly { get; set; }

	string _source;
	readonly SourceGeneratorTestRunner<SimpleGenerator> _runner = new();

	[GlobalSetup]
	public void Setup()
	{
		var builder = new StringBuilder();
		for (var i = 0; i < ClassCount; i++)
		{
			builder.Append("public sealed class Class");
			builder.Append(i);
			builder.AppendLine(" { }");
		}

		_source = builder.ToString();
	}

	[Benchmark]
	public async Task RunAsync()
	{
		var options = new SourceGeneratorTestOptions
		{
			CompileToAssembly = CompileToAssembly,
			EnableLogging = false,
			ValidateCodeWriterScopes = false,
		};

		// Disposed because a CompileToAssembly run creates a collectible AssemblyLoadContext: leaving it to
		// the finalizer would accumulate one per iteration and measure the leak rather than the run.
		using var result = await _runner.RunAsync(_source, options);
	}
}
