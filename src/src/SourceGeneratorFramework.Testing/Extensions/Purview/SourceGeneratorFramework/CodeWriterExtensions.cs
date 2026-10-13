#pragma warning disable CS1591
using System.ComponentModel;

namespace Purview.SourceGeneratorFramework;

[System.Diagnostics.CodeAnalysis.SuppressMessage(
	"Design",
	"CA1034:Nested types should not be visible",
	Justification = "The extension(...) block is lowered to an unnamed nested type; the language requires it to be nested in a static class, and PSGFR34 mandates extension blocks over classic 'this'-parameter methods."
)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static class CodeWriterExtensions
{
	extension(CodeWriter)
	{
		public static CodeWriter CreateTestWriter(
			GenerationSettings? settings = null,
			bool includeGeneratedAttributes = false,
			bool throwOnUnclosedScopes = true
		)
		{
			return new(settings ?? new("TestGenerator", "1"), throwOnUnclosedScopes: throwOnUnclosedScopes)
			{
				DefaultIncludeGeneratedAttributes = includeGeneratedAttributes,
			};
		}
	}
}
