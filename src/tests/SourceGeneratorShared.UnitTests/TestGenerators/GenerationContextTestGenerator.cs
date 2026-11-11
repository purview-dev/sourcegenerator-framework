using Microsoft.CodeAnalysis;
using Purview.SourceGeneratorFramework.Helpers;

namespace Purview.SourceGeneratorFramework.TestGenerators;

public sealed class GenerationContextTestGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		var generationContext = IncrementalPipeline.DefaultGenerationContextValueProvider(
			context,
			new GenerationSettings("GenerationContextTestGenerator", "1.0.0")
		);

		context.RegisterSourceOutput(generationContext, static (spc, value) =>
			spc.AddSource("Context.g.cs", $"// GenerationContextTestGenerator")
		);
	}
}
