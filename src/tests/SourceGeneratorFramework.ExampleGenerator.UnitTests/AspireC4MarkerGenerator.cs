using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Purview.SourceGeneratorFramework.ExampleGenerator;

/// <summary>
/// Emits the marker attributes and enums for the aspirec4 attribute-data model scenario via post-initialization
/// output, mirroring the aspirec4 consumer's <c>MarkerAttributeEmitter</c>:
/// <c>LikeC4RegistryAttribute</c>, <c>KnownTypeAttribute</c>, <c>SeverityAttribute</c>, <c>LikeC4RegistryType</c>
/// and <c>LikeC4Severity</c>.
/// </summary>
public sealed class AspireC4MarkerGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		context.RegisterPostInitializationOutput(static spc =>
			spc.AddSource(
				"AspireC4Marker.g.cs",
				SourceText.From(
					"""
					namespace Aspire.Hosting.AspireC4;

					public enum LikeC4Severity
					{
						Inherit = 0,
						Off = 1,
						Suggestion = 2,
						Warning = 3,
						Error = 4,
					}

					public enum LikeC4RegistryType
					{
						Tag = 0,
						ElementKind = 1,
						RelationshipKind = 2,
						Group = 3,
						MetadataKey = 4,
					}

					[global::System.AttributeUsage(global::System.AttributeTargets.Class)]
					public sealed class LikeC4RegistryAttribute : global::System.Attribute
					{
						public LikeC4Severity Strict { get; set; } = LikeC4Severity.Inherit;
					}

					[global::System.AttributeUsage(global::System.AttributeTargets.Field)]
					public sealed class KnownTypeAttribute : global::System.Attribute
					{
						public KnownTypeAttribute(LikeC4RegistryType type)
						{
							Type = type;
						}

						public LikeC4RegistryType Type { get; set; }

						public LikeC4Severity Strict { get; set; } = LikeC4Severity.Inherit;
					}

					[global::System.AttributeUsage(global::System.AttributeTargets.Class)]
					public sealed class SeverityAttribute : global::System.Attribute
					{
						public SeverityAttribute(LikeC4Severity severity)
						{
							Severity = severity;
						}

						public LikeC4Severity Severity { get; set; }
					}
					""",
					System.Text.Encoding.UTF8
				)
			)
		);
	}
}
