namespace Purview.SourceGeneratorFramework.Examples;

/// <summary>
/// Attribute data model for <see cref="SeverityAttribute"/>, demonstrating a <c>[Generate]</c> target that
/// references the generated type library's <c>SeverityAttributeFullName</c> constant instead of a
/// <c>typeof(...)</c> value. The <c>IsEnum</c> defaults are supplied as bare member names and are expanded by the
/// generator to the fully-qualified enum member name derived from the target attribute's constructor parameter
/// and property.
/// </summary>
[Generate(SampleTypeLibrary.Purview.SourceGeneratorFramework.Examples.SeverityAttributeFullName)]
public readonly partial record struct SeverityAttributeData(
	[Argument(IsEnum = true, Name = "severity", DefaultValue = "Inherit")] string Severity,
	[Property(IsEnum = true, DefaultValue = "Inherit")] string Level
);
