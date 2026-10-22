using Microsoft.CodeAnalysis;

namespace Purview.SourceGeneratorFramework;

/// <summary>
/// Runs the body of a Roslyn component so an unexpected exception becomes a diagnostic rather than a
/// compiler-level failure.
/// </summary>
/// <remarks>
/// <para>
/// What an escaping exception costs depends on the component, and none of the outcomes are good. A source
/// generator produces <c>CS8785</c> and emits nothing at all for that compilation, so the consumer also gets
/// every "type or namespace not found" error that follows. An analyzer produces <c>AD0001</c>. A code fix or
/// refactoring takes down the light-bulb. A suppressor silently stops suppressing, and the diagnostics it
/// was hiding reappear with no explanation. In every case the consumer is shown a stack trace through code
/// they do not own.
/// </para>
/// <para>
/// Not every component can report a diagnostic, so the guard offers two shapes rather than pretending to a
/// uniform one:
/// </para>
/// <list type="bullet">
/// <item>
/// <see cref="Run(SourceProductionContext, Type, Action, ComponentFailureOptions?)"/> and
/// <see cref="Run(Action{Diagnostic}, Type, Action, ComponentFailureOptions?,CancellationToken)"/> report the failure, for
/// generators and analyzers.
/// </item>
/// <item>
/// <see cref="TryRun(Type, Action, out Diagnostic?, ComponentFailureOptions?, CancellationToken)"/> returns whether the body
/// succeeded, for suppressors, code fixes and refactorings — none of which have a channel to report on. The
/// correct behaviour there is to do nothing: suppress nothing, offer no fix.
/// </item>
/// </list>
/// <para>
/// Cancellation is never caught. Roslyn cancels analysis constantly as the user types, and swallowing an
/// <see cref="OperationCanceledException"/> would turn a normal cancellation into a reported defect.
/// </para>
/// </remarks>
public static class ComponentGuard
{
	/// <summary>
	/// Runs <paramref name="body"/>, reporting any failure to <paramref name="context"/>.
	/// </summary>
	public static void Run<TComponent>(
		SourceProductionContext context,
		Action body,
		ComponentFailureOptions? options = null
	) => Run(context, typeof(TComponent), body, options);

	/// <inheritdoc cref="Run{TComponent}(SourceProductionContext, Action, ComponentFailureOptions?)" />
	public static void Run(
		SourceProductionContext context,
		Type componentType,
		Action body,
		ComponentFailureOptions? options = null
	)
	{
		if (TryRun(componentType, body, out var failure, options, context.CancellationToken))
			return;

		if (failure is not null)
			context.ReportDiagnostic(failure);
	}

	/// <summary>
	/// Runs <paramref name="body"/>, reporting any failure through <paramref name="report"/>.
	/// </summary>
	/// <remarks>
	/// For an analyzer, pass the <c>ReportDiagnostic</c> of whichever analysis context is in hand. The
	/// descriptor must also appear in the analyzer's <c>SupportedDiagnostics</c>, or Roslyn throws when it
	/// is reported — use <see cref="ComponentFailure.For{TComponent}(ComponentFailureOptions?)"/> to declare
	/// the same instance.
	/// </remarks>
	public static void Run<TComponent>(
		Action<Diagnostic> report,
		Action body,
		ComponentFailureOptions? options = null,
		CancellationToken cancellationToken = default
	) => Run(report, typeof(TComponent), body, options, cancellationToken);

	/// <inheritdoc cref="Run{TComponent}(Action{Diagnostic}, Action, ComponentFailureOptions?, CancellationToken)" />
	public static void Run(
		Action<Diagnostic> report,
		Type componentType,
		Action body,
		ComponentFailureOptions? options = null,
		CancellationToken cancellationToken = default
	)
	{
		if (report is null)
			throw new ArgumentNullException(nameof(report));

		if (TryRun(componentType, body, out var failure, options, cancellationToken))
			return;

		if (failure is not null)
			report(failure);
	}

	/// <summary>
	/// Runs <paramref name="body"/> and reports whether it succeeded, for a component with no channel to
	/// report a diagnostic on.
	/// </summary>
	/// <returns>
	/// <see langword="true"/> when the body completed; <see langword="false"/> when it threw, with
	/// <paramref name="failure"/> describing it.
	/// </returns>
	/// <remarks>
	/// A suppressor, code fix or refactoring cannot report a diagnostic. The failure is handed back so the
	/// caller can log it if it has a logger, but the behaviour that matters is simply to stop: a suppressor
	/// suppresses nothing, a fix is not offered.
	/// </remarks>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1021:Avoid out parameters")]
	public static bool TryRun<TComponent>(
		Action body,
		out Diagnostic? failure,
		ComponentFailureOptions? options = null,
		CancellationToken cancellationToken = default
	) => TryRun(typeof(TComponent), body, out failure, options, cancellationToken);

	/// <inheritdoc cref="TryRun{TComponent}(Action, out Diagnostic?, ComponentFailureOptions?, CancellationToken)" />
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1021:Avoid out parameters")]
	public static bool TryRun(
		Type componentType,
		Action body,
		out Diagnostic? failure,
		ComponentFailureOptions? options = null,
		CancellationToken cancellationToken = default
	)
	{
		if (componentType is null)
			throw new ArgumentNullException(nameof(componentType));
		if (body is null)
			throw new ArgumentNullException(nameof(body));

		try
		{
			body();
			failure = null;

			return true;
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
			// Roslyn cancels analysis on every keystroke. This is not a defect and must propagate.
			throw;
		}
#pragma warning disable CA1031 // Catching a general exception is the entire point of a guard.
		catch (Exception exception)
#pragma warning restore CA1031
		{
			failure = Describe(componentType, exception, options);

			return false;
		}
	}

	/// <summary>
	/// Resolves the component type behind a callback, skipping the compiler-generated closure types a lambda
	/// is compiled into.
	/// </summary>
	/// <remarks>
	/// A lambda's declaring type is a closure (<c>&lt;&gt;c</c>, <c>&lt;&gt;c__DisplayClass0_0</c>) nested in
	/// the type that wrote it. Walking out to the first type with a usable name gives the generator, which is
	/// what names the diagnostic and supplies the namespace its id is derived from.
	/// </remarks>
	public static Type ResolveComponentType(Delegate callback, Type fallback)
	{
		if (callback is null)
			throw new ArgumentNullException(nameof(callback));
		if (fallback is null)
			throw new ArgumentNullException(nameof(fallback));

		var declaring = callback.Method.DeclaringType;
		while (declaring is not null && declaring.Name.IndexOf('<') >= 0)
			declaring = declaring.DeclaringType;

		return declaring ?? fallback;
	}

	/// <summary>
	/// Builds the diagnostic for a caught exception.
	/// </summary>
	public static Diagnostic Describe(Type componentType, Exception exception, ComponentFailureOptions? options = null)
	{
		if (exception is null)
			throw new ArgumentNullException(nameof(exception));

		var message =
			options?.IncludeStackTrace == true
				? exception.Message + Environment.NewLine + exception.StackTrace
				: exception.Message;

		return Diagnostic.Create(
			ComponentFailure.For(componentType, options),
			Location.None,
			componentType.Name,
			exception.GetType().Name,
			// A message spanning lines breaks the single-line shape every diagnostic consumer expects.
			message?.Replace("\r", " ").Replace("\n", " ")
				?? "no message"
		);
	}
}
