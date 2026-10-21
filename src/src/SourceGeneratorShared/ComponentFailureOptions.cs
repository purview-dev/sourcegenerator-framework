using Microsoft.CodeAnalysis;

namespace Purview.SourceGeneratorFramework;

/// <summary>
/// Overrides for the diagnostic a Roslyn component reports when it fails unexpectedly. Every member is
/// optional; anything left unset keeps the default derived from the component itself.
/// </summary>
/// <remarks>
/// <para>
/// These are code-level settings rather than build properties, and deliberately so. An analyzer has to
/// declare every diagnostic it can report in <c>SupportedDiagnostics</c>, which Roslyn reads before any
/// analyzer options exist — a build property could not reach it in time, and reporting an undeclared id
/// makes Roslyn throw. Declaring the descriptor in code is the only form that works for all four component
/// kinds.
/// </para>
/// </remarks>
public sealed record ComponentFailureOptions
{
	/// <summary>
	/// Gets the diagnostic id. Defaults to a prefix derived from the component's own namespace, plus
	/// <c>0001</c> — see <see cref="ComponentFailure.DeriveIdPrefix(System.Type)"/>.
	/// </summary>
	public string? Id { get; init; }

	/// <summary>
	/// Gets the diagnostic title. Defaults to "&lt;component kind&gt; failed".
	/// </summary>
	public string? Title { get; init; }

	/// <summary>
	/// Gets the message format. Receives the component name (<c>{0}</c>), the exception type name
	/// (<c>{1}</c>) and the exception message (<c>{2}</c>).
	/// </summary>
	public string? MessageFormat { get; init; }

	/// <summary>
	/// Gets the diagnostic category. Defaults to the component's root namespace, so a consumer's failures
	/// group under their own product rather than under the framework.
	/// </summary>
	public string? Category { get; init; }

	/// <summary>
	/// Gets the severity. Defaults to <see cref="DiagnosticSeverity.Warning"/>: a component that failed has
	/// produced nothing, and failing the consumer's build on top of that rarely helps them.
	/// </summary>
	public DiagnosticSeverity? Severity { get; init; }

	/// <summary>
	/// Gets the long description shown in IDE diagnostic details.
	/// </summary>
	public string? Description { get; init; }

	/// <summary>
	/// Gets the help link shown alongside the diagnostic.
	/// </summary>
	public string? HelpLinkUri { get; init; }

	/// <summary>
	/// Gets a value indicating whether the exception's stack trace is included in the reported message.
	/// Defaults to <see langword="false"/>.
	/// </summary>
	/// <remarks>
	/// A stack trace through a component's internals is noise to the consumer and makes the diagnostic
	/// unstable between builds, so it is off by default. Turn it on when asking a consumer to report a bug.
	/// </remarks>
	public bool IncludeStackTrace { get; init; }
}
