using Microsoft.CodeAnalysis;
using Purview.SourceGeneratorFramework.Helpers;

namespace Purview.SourceGeneratorFramework.TestGenerators;

/// <summary>
/// A generator whose emit callback always throws, for covering the guard the framework's
/// <c>RegisterSourceOutput</c> wraps that callback in.
/// </summary>
public sealed class ThrowingTestGenerator : IIncrementalGenerator
{
	public const string FailureMessage = "the emit step fell over";

	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		var targets = IncrementalPipeline.ForAttributeWithMetadataName(
			context,
			new TypeIdentity("TestAttribute", null),
			static (ctx, _) => GeneratorResult<TargetInfo>.Create(new TargetInfo(ctx.TargetSymbol.Name))
		);

		var generationContext = IncrementalPipeline.DefaultGenerationContextValueProvider(
			context,
			new GenerationSettings("ThrowingTestGenerator", "1.0.0")
		);

		context.RegisterSourceOutput(
			targets,
			generationContext,
			static (spc, target, ctx) => throw new InvalidOperationException(FailureMessage)
		);
	}
}
