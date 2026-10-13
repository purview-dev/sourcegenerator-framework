#pragma warning disable CS1591
using System.ComponentModel;

namespace Microsoft.CodeAnalysis;

[System.Diagnostics.CodeAnalysis.SuppressMessage(
	"Design",
	"CA1034:Nested types should not be visible",
	Justification = "The extension(...) block is lowered to an unnamed nested type; the language requires it to be nested in a static class, and PSGFR34 mandates extension blocks over classic 'this'-parameter methods."
)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static class SourceProductionContextExtensions
{
	extension(SourceProductionContext context)
	{
		/// <summary>
		/// Reports a single <see cref="ReportableDiagnostic"/> to the source production context.
		/// </summary>
		public void ReportDiagnostic(ReportableDiagnostic diagnostic)
		{
			if (diagnostic is null)
				throw new ArgumentNullException(nameof(diagnostic));

			context.ReportDiagnostic(diagnostic.ToDiagnostic());
		}

		/// <summary>
		/// Reports a sequence of <see cref="ReportableDiagnostic"/> diagnostics to the source production context.
		/// </summary>
		public void ReportDiagnostics(IEnumerable<ReportableDiagnostic> diagnostics)
		{
			if (diagnostics is null)
				throw new ArgumentNullException(nameof(diagnostics));

			foreach (var diagnostic in diagnostics)
				context.ReportDiagnostic(diagnostic);
		}
	}
}
