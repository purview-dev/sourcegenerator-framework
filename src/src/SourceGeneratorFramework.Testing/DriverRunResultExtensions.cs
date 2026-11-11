using System.ComponentModel;
using Microsoft.CodeAnalysis;
using Purview.SourceGeneratorFramework.Logging;

namespace Purview.SourceGeneratorFramework.Testing;

/// <summary>
/// Provides assertion and inspection helpers for <see cref="DriverRunResult"/>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class DriverRunResultExtensions
{
	/// <summary>
	/// Throws <see cref="InvalidOperationException"/> if any generation exceptions occurred.
	/// </summary>
	public static DriverRunResult AssertNoGenerationExceptions(this DriverRunResult result)
	{
		if (result == null)
			throw new ArgumentNullException(nameof(result));

		var exceptions = result.DriverResult.Results.Select(r => r.Exception).Where(e => e != null).ToList();

		if (exceptions.Count > 0)
		{
			throw new InvalidOperationException(
				"Generator threw exceptions:\n" + string.Join("\n", exceptions.Select(e => e!.ToString()))
			);
		}

		// All valid...
		return result;
	}

	/// <summary>
	/// Throws <see cref="InvalidOperationException"/> if the output compilation has any errors.
	/// </summary>
	public static DriverRunResult AssertNoCompilationErrors(this DriverRunResult result)
	{
		if (result == null)
			throw new ArgumentNullException(nameof(result));

		var errors = result
			.CompilationResult.Compilation.GetDiagnostics()
			.Where(d => d.Severity == DiagnosticSeverity.Error)
			.ToList();

		if (errors.Count > 0)
		{
			throw new InvalidOperationException(
				"Compilation errors:\n" + string.Join("\n", errors.Select(d => d.ToString()))
			);
		}

		//All valid...
		return result;
	}

	/// <summary>
	/// Throws <see cref="InvalidOperationException"/> if the generator logged any errors.
	/// </summary>
	public static DriverRunResult AssertNoLogErrors(this DriverRunResult result)
	{
		if (result == null)
			throw new ArgumentNullException(nameof(result));

		var errors = result.LogEntries.Where(e => e.Type == SourceGenLogLevel.Fatal).Select(e => e.Message).ToList();

		if (errors.Count > 0)
		{
			throw new InvalidOperationException("Generator logged errors:\n" + string.Join("\n", errors));
		}

		// All valid...
		return result;
	}

	/// <summary>
	/// Throws <see cref="InvalidOperationException"/> if the number of generated non-attribute sources does not match the expected count.
	/// </summary>
	public static DriverRunResult AssertGeneratedSourceCount(this DriverRunResult result, int count)
	{
		if (result == null)
			throw new ArgumentNullException(nameof(result));

		var actual = result.PrimarySyntaxTrees.Length;
		if (actual != count)
		{
			throw new InvalidOperationException($"Expected {count} generated sources but found {actual}.");
		}

		// All valid...
		return result;
	}

	/// <summary>
	/// Returns the single generated non-attribute source, throwing <see cref="InvalidOperationException"/> if there is not exactly one.
	/// </summary>
	public static string AssertSingleGeneratedSource(this DriverRunResult result)
	{
		if (result == null)
			throw new ArgumentNullException(nameof(result));

		var tree = result.PrimarySyntaxTrees.SingleOrDefault()
			?? throw new InvalidOperationException("Expected a single generated source but found none.");

		return tree.GetText().ToString();
	}

	/// <summary>
	/// Throws <see cref="InvalidOperationException"/> if the generated source does not contain the expected text.
	/// </summary>
	public static DriverRunResult AssertGeneratedSourceContains(this DriverRunResult result, string expected)
	{
		if (result == null)
			throw new ArgumentNullException(nameof(result));

		var source = result.GetSource();
		if (!source.Contains(expected, StringComparison.Ordinal))
		{
			throw new InvalidOperationException(
				$"Expected generated source to contain '{expected}'. Generated source:\n{source}"
			);
		}

		// All valid...
		return result;
	}
}
