#pragma warning disable CS1591
using System.ComponentModel;

namespace System;

[Diagnostics.CodeAnalysis.SuppressMessage(
	"Design",
	"CA1034:Nested types should not be visible",
	Justification = "The extension(...) block is lowered to an unnamed nested type; the language requires it to be nested in a static class, and PSGFR34 mandates extension blocks over classic 'this'-parameter methods."
)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static class StringExtensions
{
	extension(string? value)
	{
		/// <summary>
		/// Surrounds the string with the specified string. Default is double quotes.
		/// </summary>
		/// <param name="surroundWith">The string to surround the value with.</param>
		/// <returns>The surrounded string.</returns>
		public string Surround(string surroundWith = "\"") => $"{surroundWith}{value}{surroundWith}";
	}
}
