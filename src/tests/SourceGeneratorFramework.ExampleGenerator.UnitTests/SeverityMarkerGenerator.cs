using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Purview.SourceGeneratorFramework.ExampleGenerator;

/// <summary>
/// Emits the target attribute and enum for the attribute-data model TypeLibrary scenario via post-initialization
/// output. This mirrors a consumer generator that declares its marker attribute and enum as generated content:
/// post-init output is shared across all generators in a pass, while the <c>TypeLibrary</c> class emitted by
/// <c>TypeLibraryGenerator</c>'s main pipeline is not.
/// </summary>
public sealed class SeverityMarkerGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		context.RegisterPostInitializationOutput(static spc =>
		{
			spc.AddSource(
				"SeverityMarker.g.cs",
				SourceText.From(
					"""
					namespace Aspire.Hosting.AspireC4;

					public enum LikeC4Severity
					{
						Inherit = 0,
						Info = 1,
						Warning = 2,
						Error = 3,
					}

					[global::System.AttributeUsage(global::System.AttributeTargets.All, AllowMultiple = false)]
					public sealed class SeverityAttribute : global::System.Attribute
					{
						public SeverityAttribute(LikeC4Severity severity)
						{
							Severity = severity;
						}

						public LikeC4Severity Severity { get; set; }

						public LikeC4Severity Level { get; set; }
					}
					""",
					System.Text.Encoding.UTF8
				)
			);
		});
	}
}
