using BenchmarkDotNet.Attributes;
using Purview.SourceGeneratorFramework.Generators;
using Purview.SourceGeneratorFramework.Testing;

namespace Purview.SourceGeneratorFramework.Benchmarks;

[MemoryDiagnoser]
public class AttributeDataModelGeneratorBenchmarks
{
	readonly SourceGeneratorTestRunner<AttributeDataModelGenerator> _runner = new();
	string _source;

	[GlobalSetup]
	public void Setup()
	{
		_source = """
			using Purview.SourceGeneratorFramework;
			using Purview.SourceGeneratorFramework.Generators;

			namespace Benchmarks
			{
				[Generate(typeof(MyAttribute))]
				public class MyAttribute : System.Attribute
				{
					public string Name { get; set; } = default!;
				}
			}
			""";
	}

	[Benchmark]
	public async Task RunAsync()
	{
		var options = new SourceGeneratorTestOptions
		{
			CompileToAssembly = false,
			EnableLogging = false,
			ValidateCodeWriterScopes = false,
		};

		await _runner.RunAsync(_source, options);
	}
}
