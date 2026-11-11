using System.ComponentModel;
using Microsoft.CodeAnalysis;
using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace Purview.SourceGeneratorFramework.Testing.TUnit.Assertions;

/// <summary>
/// Contains assertion methods for <see cref="DriverRunResult"/> and <see cref="AnalyzerTestResult"/>.
/// </summary>
public static partial class DiagnosticAssertions
{
	/// <summary>
	/// Asserts that the driver run contains the expected total number of generator and analyzer diagnostics.
	/// </summary>
	/// <param name="diagnostic">The result of the driver run to check.</param>
	/// <param name="count">The expected total number of diagnostics.</param>
	/// <returns>The diagnostics when the assertion passes.</returns>
	[GenerateAssertion]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static AssertionResult<IEnumerable<Diagnostic>> HasDiagnostics(this DriverRunResult diagnostic, int count)
	{
		if (diagnostic is null)
			return AssertionResult.Failed($"expected {nameof(DriverRunResult)} is null");

		// Not null... process
		return HasDiagnostics(
			diagnostic.DriverResult.Diagnostics.Concat(diagnostic.AnalyzerResult?.Diagnostics ?? []),
			count
		);
	}

	/// <summary>
	/// Asserts that the <paramref name="diagnostic"/> contains a diagnostic with the same Id as the <paramref name="expected"/> <see cref="DiagnosticDescriptor"/>.
	/// </summary>
	/// <param name="diagnostic">The result of the driver run to check for the expected diagnostic.</param>
	/// <param name="expected">The expected diagnostic descriptor.</param>
	/// <returns>An <see cref="AssertionResult"/> indicating whether the assertion passed or failed.</returns>
	[GenerateAssertion]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static AssertionResult<Diagnostic> HasDiagnostic(
		this DriverRunResult diagnostic,
		DiagnosticDescriptor expected
	)
	{
		// Don't change the name of the parameter "diagnostic" to "result" because it will break the generated assertion method name.
		if (diagnostic == null)
			return AssertionResult.Failed($"expected {nameof(DriverRunResult)} is null");

		// Not null... process
		if (expected is null)
			return AssertionResult.Failed($"expected {nameof(DiagnosticDescriptor)} is null");

		var matchingDiagnostic = diagnostic.DriverResult.Diagnostics.FirstOrDefault(d => d.Id == expected.Id)
			?? diagnostic.AnalyzerResult?.Diagnostics.FirstOrDefault(d => d.Id == expected.Id);
		return matchingDiagnostic is null
			? (AssertionResult<Diagnostic>)
				AssertionResult.Failed($"expected to contain diagnostic with Id {expected.Id}")
			: AssertionResult<Diagnostic>.Passed(matchingDiagnostic);
	}

	/// <summary>
	/// Asserts that the <paramref name="diagnostic"/> contains a diagnostic with the same Id as the <paramref name="expected"/> <see cref="DiagnosticDescriptor"/>.
	/// </summary>
	/// <param name="diagnostic">The result of the driver run to check for the expected diagnostic.</param>
	/// <param name="expected">The expected diagnostic descriptor.</param>
	/// <param name="count">The expected number of diagnostics with the same Id as the <paramref name="expected"/> <see cref="DiagnosticDescriptor"/>.</param>
	/// <returns>An <see cref="AssertionResult"/> indicating whether the assertion passed or failed.</returns>
	[GenerateAssertion]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static AssertionResult<IEnumerable<Diagnostic>> HasDiagnostics(
		this DriverRunResult diagnostic,
		DiagnosticDescriptor expected,
		int count
	)
	{
		// Don't change the name of the parameter "diagnostic" to "result" because it will break the generated assertion method name.
		if (diagnostic == null)
			return AssertionResult.Failed($"expected {nameof(DriverRunResult)} is null");
		if (count < 1)
			return AssertionResult.Failed($"expected {nameof(count)} is less than 1");

		// Not null... process
		if (expected is null)
			return AssertionResult.Failed($"expected {nameof(DiagnosticDescriptor)} is null");

		var matchingDiagnostic = diagnostic
			.DriverResult.Diagnostics.Where(d => d.Id == expected.Id)
			.Concat(diagnostic.AnalyzerResult?.Diagnostics.Where(d => d.Id == expected.Id) ?? []);
		return matchingDiagnostic is null
			? (AssertionResult<IEnumerable<Diagnostic>>)
				AssertionResult.Failed($"expected to contain {count} diagnostic(s) with Id {expected.Id}")
			: AssertionResult<IEnumerable<Diagnostic>>.Passed(matchingDiagnostic);
	}

	/// <summary>
	/// Asserts that the <paramref name="diagnostic"/> contains a diagnostic with the same Id as the <paramref name="expected"/> <see cref="DiagnosticDescriptor.Id"/>.
	/// </summary>
	/// <param name="diagnostic">The result of the driver run to check for the expected diagnostic.</param>
	/// <param name="expected">The expected diagnostic Id.</param>
	/// <returns>An <see cref="AssertionResult"/> indicating whether the assertion passed or failed.</returns>
	[GenerateAssertion]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static AssertionResult<Diagnostic> HasDiagnostic(this DriverRunResult diagnostic, string expected)
	{
		// Don't change the name of the parameter "diagnostic" to "result" because it will break the generated assertion method name.
		if (diagnostic == null)
			return AssertionResult.Failed($"expected {nameof(DriverRunResult)} is null");

		if (string.IsNullOrWhiteSpace(expected))
			return AssertionResult.Failed($"expected {nameof(DiagnosticDescriptor.Id)} is null/ empty/ whitespace");

		var matchedDiagnotics = diagnostic.DriverResult.Diagnostics.FirstOrDefault(d => d.Id == expected)
			?? diagnostic.AnalyzerResult?.Diagnostics.FirstOrDefault(d => d.Id == expected);
		;
		return matchedDiagnotics is null
			? (AssertionResult<Diagnostic>)AssertionResult.Failed($"expected to contain diagnostic with Id {expected}")
			: AssertionResult<Diagnostic>.Passed(matchedDiagnotics);
	}

	/// <summary>
	/// Asserts that the <paramref name="diagnostic"/> contains a diagnostic with the same Id as the <paramref name="expected"/> <see cref="DiagnosticDescriptor.Id"/>.
	/// </summary>
	/// <param name="diagnostic">The result of the driver run to check for the expected diagnostic.</param>
	/// <param name="expected">The expected diagnostic Id.</param>
	/// <param name="count">The expected number of diagnostics with the same Id as the <paramref name="expected"/> <see cref="DiagnosticDescriptor.Id"/>.</param>
	/// <returns>An <see cref="AssertionResult"/> indicating whether the assertion passed or failed.</returns>
	[GenerateAssertion]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static AssertionResult<IEnumerable<Diagnostic>> HasDiagnostics(
		this DriverRunResult diagnostic,
		string expected,
		int count
	)
	{
		// Don't change the name of the parameter "diagnostic" to "result" because it will break the generated assertion method name.
		if (diagnostic == null)
			return AssertionResult.Failed($"expected {nameof(DriverRunResult)} is null");
		if (count < 1)
			return AssertionResult.Failed($"expected {nameof(count)} is less than 1");

		if (string.IsNullOrWhiteSpace(expected))
			return AssertionResult.Failed($"expected {nameof(DiagnosticDescriptor.Id)} is null/ empty/ whitespace");

		var matchedDiagnotics = diagnostic
			.DriverResult.Diagnostics.Where(d => d.Id == expected)
			.Concat(diagnostic.AnalyzerResult?.Diagnostics.Where(d => d.Id == expected) ?? []);
		return matchedDiagnotics is null
			? (AssertionResult<IEnumerable<Diagnostic>>)
				AssertionResult.Failed($"expected to contain {count} diagnostic(s) with Id {expected}")
			: AssertionResult<IEnumerable<Diagnostic>>.Passed(matchedDiagnotics);
	}

	/// <summary>
	/// Asserts that the <paramref name="result"/> does not contain a diagnostic with the same Id as the <paramref name="expected"/> <see cref="DiagnosticDescriptor"/>.
	/// </summary>
	/// <param name="result">The result of the driver run to check for the expected diagnostic.</param>
	/// <param name="expected">The expected diagnostic descriptor.</param>
	/// <returns>An <see cref="AssertionResult"/> indicating whether the assertion passed or failed.</returns>
	[GenerateAssertion]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static AssertionResult DoesNotHaveDiagnostic(this DriverRunResult result, DiagnosticDescriptor expected)
	{
		if (result == null)
			return AssertionResult.Failed($"expected {nameof(DriverRunResult)} is null");

		// Not null... process
		return expected is null
			? AssertionResult.Failed($"expected {nameof(DiagnosticDescriptor)} is null")
			: AssertionResult.FailIf(
				result.DriverResult.Diagnostics.Any(d => d.Id == expected.Id)
					|| result.AnalyzerResult?.Diagnostics.Any(d => d.Id == expected.Id) == true,
				$"expected not to contain diagnostic with Id {expected.Id}"
			);
	}

	/// <summary>
	/// Asserts that the <paramref name="result"/> does not contain a diagnostic with the same Id as the <paramref name="expected"/> <see cref="DiagnosticDescriptor.Id"/>.
	/// </summary>
	/// <param name="result">The result of the driver run to check for the expected diagnostic.</param>
	/// <param name="expected">The expected diagnostic Id.</param>
	/// <returns>An <see cref="AssertionResult"/> indicating whether the assertion passed or failed.</returns>
	[GenerateAssertion]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static AssertionResult DoesNotHaveDiagnostic(this DriverRunResult result, string expected)
	{
		if (result == null)
			return AssertionResult.Failed($"expected {nameof(DriverRunResult)} is null");

		// Not null... process
		return expected is null
			? AssertionResult.Failed($"expected {nameof(DiagnosticDescriptor)} is null")
			: AssertionResult.FailIf(
				result.DriverResult.Diagnostics.Any(d => d.Id == expected)
					|| result.AnalyzerResult?.Diagnostics.Any(d => d.Id == expected) == true,
				$"expected not to contain diagnostic with Id {expected}"
			);
	}

	/// <summary>
	/// Asserts that the <paramref name="result"/> does not contain a diagnostic where the Id starts with the <paramref name="startsWithValue"/> <see cref="DiagnosticDescriptor.Id"/>.
	/// </summary>
	/// <param name="result">The result of the driver run to check for the expected diagnostic.</param>
	/// <param name="startsWithValue">The expected diagnostic Id start value.</param>
	/// <returns>An <see cref="AssertionResult"/> indicating whether the assertion passed or failed.</returns>
	[GenerateAssertion]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static AssertionResult DoesNotHaveDiagnosticThatStartsWith(
		this DriverRunResult result,
		string startsWithValue
	)
	{
		if (result == null)
			return AssertionResult.Failed($"expected {nameof(DriverRunResult)} is null");

		// Not null... process
		return startsWithValue is null
			? AssertionResult.Failed($"expected {nameof(DiagnosticDescriptor)} is null")
			: AssertionResult.FailIf(
				result.DriverResult.Diagnostics.Any(d => d.Id.StartsWith(startsWithValue, StringComparison.Ordinal))
					|| result.AnalyzerResult?.Diagnostics.Any(d =>
						d.Id.StartsWith(startsWithValue, StringComparison.Ordinal)
					) == true,
				$"expected not to contain Diagnostic with Id starting with {startsWithValue}"
			);
	}

	/// <summary>
	/// Asserts that the <paramref name="result"/> does not contain any diagnostics with severity <see cref="DiagnosticSeverity.Error"/>.
	/// </summary>
	/// <param name="result">The result of the driver run to check for error diagnostics.</param>
	/// <returns>An <see cref="AssertionResult"/> indicating whether the assertion passed or failed.</returns>
	[GenerateAssertion]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static AssertionResult HasNoErrorDiagnostics(this DriverRunResult result)
	{
		if (result == null)
			return AssertionResult.Failed($"expected {nameof(DriverRunResult)} is null");

		// Not null... process
		return AssertionResult.FailIf(
			result.DriverResult.Diagnostics.Any(static d => d.Severity == DiagnosticSeverity.Error)
				|| result.AnalyzerResult?.Diagnostics.Any(static d => d.Severity == DiagnosticSeverity.Error) == true,
			"expected no error diagnostics to be reported by the generator"
		);
	}

	/// <summary>
	/// Asserts that the <paramref name="result"/> does not contain any diagnostics.
	/// </summary>
	/// <param name="result">The result of the driver run to check for diagnostics.</param>
	/// <returns>An <see cref="AssertionResult"/> indicating whether the assertion passed or failed.</returns>
	[GenerateAssertion]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static AssertionResult HasNoDiagnostics(this DriverRunResult result)
	{
		if (result == null)
			return AssertionResult.Failed($"expected {nameof(DriverRunResult)} is null");

		// Not null... process
		return AssertionResult.FailIf(
			result.DriverResult.Diagnostics.Any() || result.AnalyzerResult?.Diagnostics.Any() == true,
			"expected no diagnostics to be reported by the generator"
		);
	}

	/// <summary>
	/// Asserts that an analyzer result contains the expected diagnostic.
	/// </summary>
	[GenerateAssertion]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static AssertionResult<Diagnostic> HasDiagnostic(
		this AnalyzerTestResult diagnostic,
		DiagnosticDescriptor expected
	) => HasDiagnostic(diagnostic?.Diagnostics, expected);

	/// <summary>
	/// Asserts that an analyzer result contains the expected total number of diagnostics.
	/// </summary>
	[GenerateAssertion]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static AssertionResult<IEnumerable<Diagnostic>> HasDiagnostics(
		this AnalyzerTestResult diagnostic,
		int count
	) => HasDiagnostics(diagnostic?.Diagnostics, count);

	/// <summary>
	/// Asserts that an analyzer result contains the expected number of diagnostics.
	/// </summary>
	[GenerateAssertion]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static AssertionResult<IEnumerable<Diagnostic>> HasDiagnostics(
		this AnalyzerTestResult diagnostic,
		DiagnosticDescriptor expected,
		int count
	) => HasDiagnostics(diagnostic?.Diagnostics, expected, count);

	/// <summary>
	/// Asserts that an analyzer result contains the expected diagnostic.
	/// </summary>
	[GenerateAssertion]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static AssertionResult<Diagnostic> HasDiagnostic(this AnalyzerTestResult diagnostic, string expected) =>
		HasDiagnostic(diagnostic?.Diagnostics, expected);

	/// <summary>
	/// Asserts that an analyzer result contains the expected number of diagnostics.
	/// </summary>
	[GenerateAssertion]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static AssertionResult<IEnumerable<Diagnostic>> HasDiagnostics(
		this AnalyzerTestResult diagnostic,
		string expected,
		int count
	) => HasDiagnostics(diagnostic?.Diagnostics, expected, count);

	/// <summary>
	/// Asserts that an analyzer result does not contain the expected diagnostic.
	/// </summary>
	[GenerateAssertion]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static AssertionResult DoesNotHaveDiagnostic(
		this AnalyzerTestResult result,
		DiagnosticDescriptor expected
	) => DoesNotHaveDiagnostic(result?.Diagnostics, expected);

	/// <summary>
	/// Asserts that an analyzer result does not contain the expected diagnostic.
	/// </summary>
	[GenerateAssertion]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static AssertionResult DoesNotHaveDiagnostic(this AnalyzerTestResult result, string expected) =>
		DoesNotHaveDiagnostic(result?.Diagnostics, expected);

	/// <summary>
	/// Asserts that an analyzer result does not contain a diagnostic with the specified prefix.
	/// </summary>
	[GenerateAssertion]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static AssertionResult DoesNotHaveDiagnosticThatStartsWith(
		this AnalyzerTestResult result,
		string startsWithValue
	) => DoesNotHaveDiagnosticThatStartsWith(result?.Diagnostics, startsWithValue);

	/// <summary>
	/// Asserts that an analyzer result does not contain error diagnostics.
	/// </summary>
	[GenerateAssertion]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static AssertionResult HasNoErrorDiagnostics(this AnalyzerTestResult result) =>
		HasNoErrorDiagnostics(result?.Diagnostics, "analyzer");

	/// <summary>
	/// Asserts that an analyzer result does not contain diagnostics.
	/// </summary>
	[GenerateAssertion]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static AssertionResult HasNoDiagnostics(this AnalyzerTestResult result) =>
		HasNoDiagnostics(result?.Diagnostics, "analyzer");

	/// <summary>
	/// Asserts that a code-fix result contains the expected diagnostic.
	/// </summary>
	[GenerateAssertion]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static AssertionResult<Diagnostic> HasDiagnostic(
		this CodeFixTestResult diagnostic,
		DiagnosticDescriptor expected
	) => HasDiagnostic(diagnostic?.Diagnostics, expected);

	/// <summary>
	/// Asserts that a code-fix result contains the expected total number of diagnostics.
	/// </summary>
	[GenerateAssertion]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static AssertionResult<IEnumerable<Diagnostic>> HasDiagnostics(
		this CodeFixTestResult diagnostic,
		int count
	) => HasDiagnostics(diagnostic?.Diagnostics, count);

	/// <summary>
	/// Asserts that a code-fix result contains the expected number of diagnostics.
	/// </summary>
	[GenerateAssertion]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static AssertionResult<IEnumerable<Diagnostic>> HasDiagnostics(
		this CodeFixTestResult diagnostic,
		DiagnosticDescriptor expected,
		int count
	) => HasDiagnostics(diagnostic?.Diagnostics, expected, count);

	/// <summary>
	/// Asserts that a code-fix result contains the expected diagnostic.
	/// </summary>
	[GenerateAssertion]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static AssertionResult<Diagnostic> HasDiagnostic(this CodeFixTestResult diagnostic, string expected) =>
		HasDiagnostic(diagnostic?.Diagnostics, expected);

	/// <summary>
	/// Asserts that a code-fix result contains the expected number of diagnostics.
	/// </summary>
	[GenerateAssertion]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static AssertionResult<IEnumerable<Diagnostic>> HasDiagnostics(
		this CodeFixTestResult diagnostic,
		string expected,
		int count
	) => HasDiagnostics(diagnostic?.Diagnostics, expected, count);

	/// <summary>
	/// Asserts that a code-fix result does not contain the expected diagnostic.
	/// </summary>
	[GenerateAssertion]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static AssertionResult DoesNotHaveDiagnostic(this CodeFixTestResult result, DiagnosticDescriptor expected) =>
		DoesNotHaveDiagnostic(result?.Diagnostics, expected);

	/// <summary>
	/// Asserts that a code-fix result does not contain the expected diagnostic.
	/// </summary>
	[GenerateAssertion]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static AssertionResult DoesNotHaveDiagnostic(this CodeFixTestResult result, string expected) =>
		DoesNotHaveDiagnostic(result?.Diagnostics, expected);

	/// <summary>
	/// Asserts that a code-fix result does not contain a diagnostic with the specified prefix.
	/// </summary>
	[GenerateAssertion]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static AssertionResult DoesNotHaveDiagnosticThatStartsWith(
		this CodeFixTestResult result,
		string startsWithValue
	) => DoesNotHaveDiagnosticThatStartsWith(result?.Diagnostics, startsWithValue);

	/// <summary>
	/// Asserts that a code-fix result does not contain error diagnostics.
	/// </summary>
	[GenerateAssertion]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static AssertionResult HasNoErrorDiagnostics(this CodeFixTestResult result) =>
		HasNoErrorDiagnostics(result?.Diagnostics, "code fix");

	/// <summary>
	/// Asserts that a code-fix result does not contain diagnostics.
	/// </summary>
	[GenerateAssertion]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static AssertionResult HasNoDiagnostics(this CodeFixTestResult result) =>
		HasNoDiagnostics(result?.Diagnostics, "code fix");

	static AssertionResult<Diagnostic> HasDiagnostic(
		IEnumerable<Diagnostic>? diagnostics,
		DiagnosticDescriptor expected
	)
	{
		if (diagnostics is null)
			return AssertionResult.Failed("expected analyzer result is null");
		if (expected is null)
			return AssertionResult.Failed($"expected {nameof(DiagnosticDescriptor)} is null");

		var matchingDiagnostic = diagnostics.FirstOrDefault(diagnostic => diagnostic.Id == expected.Id);
		return matchingDiagnostic is null
			? (AssertionResult<Diagnostic>)
				AssertionResult.Failed($"expected to contain diagnostic with Id {expected.Id}")
			: AssertionResult<Diagnostic>.Passed(matchingDiagnostic);
	}

	static AssertionResult<Diagnostic> HasDiagnostic(IEnumerable<Diagnostic>? diagnostics, string expected)
	{
		if (diagnostics is null)
			return AssertionResult.Failed("expected analyzer result is null");
		if (string.IsNullOrWhiteSpace(expected))
			return AssertionResult.Failed($"expected {nameof(DiagnosticDescriptor.Id)} is null/ empty/ whitespace");

		var matchingDiagnostic = diagnostics.FirstOrDefault(diagnostic => diagnostic.Id == expected);
		return matchingDiagnostic is null
			? (AssertionResult<Diagnostic>)AssertionResult.Failed($"expected to contain diagnostic with Id {expected}")
			: AssertionResult<Diagnostic>.Passed(matchingDiagnostic);
	}

	static AssertionResult<IEnumerable<Diagnostic>> HasDiagnostics(
		IEnumerable<Diagnostic>? diagnostics,
		DiagnosticDescriptor expected,
		int count
	) =>
		expected is null
			? AssertionResult.Failed($"expected {nameof(DiagnosticDescriptor)} is null")
			: HasDiagnostics(diagnostics, expected.Id, count);

	static AssertionResult<IEnumerable<Diagnostic>> HasDiagnostics(IEnumerable<Diagnostic>? diagnostics, int count)
	{
		if (diagnostics is null)
			return AssertionResult.Failed("expected diagnostic result is null");
		if (count < 0)
			return AssertionResult.Failed($"expected {nameof(count)} is less than 0");

		var materializedDiagnostics = diagnostics.ToList();
		return materializedDiagnostics.Count != count
			? (AssertionResult<IEnumerable<Diagnostic>>)
				AssertionResult.Failed(
					$"expected to contain {count} diagnostic(s), but found {materializedDiagnostics.Count}"
				)
			: AssertionResult<IEnumerable<Diagnostic>>.Passed(materializedDiagnostics);
	}

	static AssertionResult<IEnumerable<Diagnostic>> HasDiagnostics(
		IEnumerable<Diagnostic>? diagnostics,
		string expected,
		int count
	)
	{
		if (diagnostics is null)
			return AssertionResult.Failed("expected analyzer result is null");
		if (count < 1)
			return AssertionResult.Failed($"expected {nameof(count)} is less than 1");
		if (string.IsNullOrWhiteSpace(expected))
			return AssertionResult.Failed($"expected {nameof(DiagnosticDescriptor.Id)} is null/ empty/ whitespace");

		var matchingDiagnostics = diagnostics.Where(diagnostic => diagnostic.Id == expected).ToList();
		return matchingDiagnostics.Count != count
			? (AssertionResult<IEnumerable<Diagnostic>>)
				AssertionResult.Failed(
					$"expected to contain {count} diagnostic(s) with Id {expected}, but found {matchingDiagnostics.Count}"
				)
			: AssertionResult<IEnumerable<Diagnostic>>.Passed(matchingDiagnostics);
	}

	static AssertionResult DoesNotHaveDiagnostic(IEnumerable<Diagnostic>? diagnostics, DiagnosticDescriptor expected) =>
		expected is null
			? AssertionResult.Failed($"expected {nameof(DiagnosticDescriptor)} is null")
			: DoesNotHaveDiagnostic(diagnostics, expected.Id);

	static AssertionResult DoesNotHaveDiagnostic(IEnumerable<Diagnostic>? diagnostics, string expected)
	{
		if (diagnostics is null)
			return AssertionResult.Failed("expected analyzer result is null");

		// Not null... process
		return string.IsNullOrWhiteSpace(expected)
			? AssertionResult.Failed($"expected {nameof(DiagnosticDescriptor.Id)} is null/ empty/ whitespace")
			: AssertionResult.FailIf(
				diagnostics.Any(diagnostic => diagnostic.Id == expected),
				$"expected not to contain diagnostic with Id {expected}"
			);
	}

	static AssertionResult DoesNotHaveDiagnosticThatStartsWith(
		IEnumerable<Diagnostic>? diagnostics,
		string startsWithValue
	)
	{
		if (diagnostics is null)
			return AssertionResult.Failed("expected analyzer result is null");

		// Not null... process
		return string.IsNullOrWhiteSpace(startsWithValue)
			? AssertionResult.Failed("expected diagnostic Id prefix is null/ empty/ whitespace")
			: AssertionResult.FailIf(
				diagnostics.Any(diagnostic => diagnostic.Id.StartsWith(startsWithValue, StringComparison.Ordinal)),
				$"expected not to contain Diagnostic with Id starting with {startsWithValue}"
			);
	}

	static AssertionResult HasNoErrorDiagnostics(IEnumerable<Diagnostic>? diagnostics, string source)
	{
		if (diagnostics is null)
			return AssertionResult.Failed($"expected {source} result is null");

		// Not null... process
		return AssertionResult.FailIf(
			diagnostics.Any(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error),
			$"expected no error diagnostics to be reported by the {source}"
		);
	}

	static AssertionResult HasNoDiagnostics(IEnumerable<Diagnostic>? diagnostics, string source) =>
		diagnostics is null
			? AssertionResult.Failed($"expected {source} result is null")
			: AssertionResult.FailIf(diagnostics.Any(), $"expected no diagnostics to be reported by the {source}");
}
