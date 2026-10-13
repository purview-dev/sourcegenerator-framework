using Microsoft.CodeAnalysis;
using Purview.SourceGeneratorFramework.Helpers;
using Purview.SourceGeneratorFramework.Logging;

namespace Purview.SourceGeneratorFramework.TestGenerators;

/// <summary>
/// Writes a known sequence of log entries so a test can prove the runner surfaces them in the order the
/// generator wrote them.
/// </summary>
public sealed class OrderedLoggingTestGenerator : IIncrementalGenerator
{
	/// <summary>
	/// The messages written, in order, on every run.
	/// </summary>
	public static readonly string[] Messages = ["first entry", "second entry", "third entry"];

	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		var generationContext = IncrementalPipeline.DefaultGenerationContextValueProvider(
			context,
			new GenerationSettings("OrderedLoggingTestGenerator", "1.0.0")
		);

		context.RegisterSourceOutput(
			generationContext,
			static (spc, value) =>
			{
				foreach (var message in Messages)
					value.Log(SourceGenLogLevel.Info, 0, message);

				spc.AddSource("OrderedLogging.g.cs", "// OrderedLoggingTestGenerator");
			}
		);
	}
}
