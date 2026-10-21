using System.Globalization;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Purview.SourceGeneratorFramework;

/// <summary>
/// Covers <see cref="ComponentGuard"/> and the defaults <see cref="ComponentFailure"/> derives from the
/// component itself.
/// </summary>
public class ComponentGuardTests
{
	[Test]
	public async Task TryRun_GivenABodyThatSucceeds_ReturnsTrueAndNoFailure()
	{
		var ran = false;

		var succeeded = ComponentGuard.TryRun<SampleGenerator>(() => ran = true, out var failure);

		await Assert.That(succeeded).IsTrue();
		await Assert.That(failure).IsNull();
		await Assert.That(ran).IsTrue();
	}

	[Test]
	public async Task TryRun_GivenABodyThatThrows_ReturnsFalseAndDescribesTheFailure()
	{
		var succeeded = ComponentGuard.TryRun<SampleGenerator>(
			() => throw new InvalidOperationException("boom"),
			out var failure
		);

		await Assert.That(succeeded).IsFalse();
		await Assert.That(failure).IsNotNull();

		var message = failure!.GetMessage(CultureInfo.InvariantCulture);
		await Assert.That(message).Contains(nameof(SampleGenerator));
		await Assert.That(message).Contains(nameof(InvalidOperationException));
		await Assert.That(message).Contains("boom");
	}

	/// <summary>
	/// Roslyn cancels analysis on every keystroke. Swallowing that would turn a normal cancellation into a
	/// reported defect, and would hide from Roslyn that the work was abandoned.
	/// </summary>
	[Test]
	public async Task TryRun_GivenCancellation_LetsTheCancellationPropagate()
	{
		using CancellationTokenSource source = new();
		await source.CancelAsync();

		await Assert
			.That(() =>
				ComponentGuard.TryRun<SampleGenerator>(
					() => source.Token.ThrowIfCancellationRequested(),
					out _,
					cancellationToken: source.Token
				)
			)
			.Throws<OperationCanceledException>();
	}

	/// <summary>
	/// An <see cref="OperationCanceledException"/> raised when nothing was cancelled is an ordinary defect,
	/// not a cancellation, and must still be reported.
	/// </summary>
	[Test]
	public async Task TryRun_GivenAnOperationCanceledExceptionWithoutCancellation_ReportsIt()
	{
		var succeeded = ComponentGuard.TryRun<SampleGenerator>(
			() => throw new OperationCanceledException("not really cancelled"),
			out var failure
		);

		await Assert.That(succeeded).IsFalse();
		await Assert.That(failure).IsNotNull();
	}

	[Test]
	public async Task Run_GivenAReportDelegateAndAFailure_ReportsOnce()
	{
		List<Diagnostic> reported = [];

		ComponentGuard.Run<SampleAnalyzer>(reported.Add, () => throw new InvalidOperationException("boom"));

		await Assert.That(reported).HasSingleItem();
		await Assert.That(reported[0].Id).IsEqualTo(ComponentFailure.For<SampleAnalyzer>().Id);
	}

	[Test]
	public async Task Run_GivenAReportDelegateAndSuccess_ReportsNothing()
	{
		List<Diagnostic> reported = [];

		ComponentGuard.Run<SampleAnalyzer>(reported.Add, static () => { });

		await Assert.That(reported).IsEmpty();
	}

	// ---------------------------------------------------------------------------------------------
	// Defaults derived from the consuming codebase
	// ---------------------------------------------------------------------------------------------

	[Test]
	public async Task For_GivenAGenerator_DerivesTheIdFromItsNamespace()
	{
		var descriptor = ComponentFailure.For<SampleGenerator>();

		// Namespace is Purview.SourceGeneratorFramework -> P, S -> "PS", capped at four initials.
		await Assert.That(descriptor.Id).IsEqualTo(ComponentFailure.DeriveIdPrefix(typeof(SampleGenerator)) + "0001");
		await Assert.That(descriptor.Id).EndsWith("0001");
	}

	[Test]
	public async Task For_GivenAGenerator_NamesTheComponentKind()
	{
		await Assert
			.That(ComponentFailure.For<SampleGenerator>().Title.ToString(CultureInfo.InvariantCulture))
			.IsEqualTo("Source generator failed");
		await Assert
			.That(ComponentFailure.For<SampleAnalyzer>().Title.ToString(CultureInfo.InvariantCulture))
			.IsEqualTo("Analyzer failed");
		await Assert
			.That(ComponentFailure.For<SampleSuppressor>().Title.ToString(CultureInfo.InvariantCulture))
			.IsEqualTo("Diagnostic suppressor failed");
		await Assert
			.That(ComponentFailure.For<SampleCodeFix>().Title.ToString(CultureInfo.InvariantCulture))
			.IsEqualTo("Code fix failed");
	}

	[Test]
	public async Task For_UsesTheRootNamespaceAsCategory()
	{
		await Assert.That(ComponentFailure.For<SampleGenerator>().Category).IsEqualTo("Purview");
	}

	[Test]
	public async Task For_DefaultsToWarning()
	{
		await Assert
			.That(ComponentFailure.For<SampleGenerator>().DefaultSeverity)
			.IsEqualTo(DiagnosticSeverity.Warning);
	}

	/// <summary>
	/// An analyzer declares this descriptor in <c>SupportedDiagnostics</c>, and Roslyn throws if what is
	/// reported is not the same rule. The instance must therefore be stable.
	/// </summary>
	[Test]
	public async Task For_GivenTheSameComponentAndOptions_ReturnsTheSameInstance()
	{
		await Assert
			.That(ReferenceEquals(ComponentFailure.For<SampleGenerator>(), ComponentFailure.For<SampleGenerator>()))
			.IsTrue();
	}

	[Test]
	[Arguments("Acme.Widgets.Generators", "AWG")]
	[Arguments("Acme.Widgets.Generators.Internal.Deep", "AWGI")]
	[Arguments("Contoso", "CON")]
	[Arguments("A", "GEN")]
	public async Task DeriveIdPrefix_ProducesAStablePrefix(string @namespace, string expected)
	{
		await Assert.That(ComponentFailure.DeriveIdPrefix(FakeType.InNamespace(@namespace))).IsEqualTo(expected);
	}

	[Test]
	public async Task DeriveIdPrefix_GivenNoNamespace_FallsBackToGen()
	{
		await Assert.That(ComponentFailure.DeriveIdPrefix(FakeType.InNamespace(null))).IsEqualTo("GEN");
	}

	// ---------------------------------------------------------------------------------------------
	// Overrides
	// ---------------------------------------------------------------------------------------------

	[Test]
	public async Task Options_OverrideEveryPartOfTheDescriptor()
	{
		ComponentFailureOptions options = new()
		{
			Id = "ACME9999",
			Title = "Widget generator fell over",
			MessageFormat = "{0} / {1} / {2}",
			Category = "Acme.Widgets",
			Severity = DiagnosticSeverity.Error,
			Description = "A description.",
			HelpLinkUri = "https://example.invalid/acme9999",
		};

		var descriptor = ComponentFailure.For<SampleGenerator>(options);

		await Assert.That(descriptor.Id).IsEqualTo("ACME9999");
		await Assert
			.That(descriptor.Title.ToString(CultureInfo.InvariantCulture))
			.IsEqualTo("Widget generator fell over");
		await Assert.That(descriptor.Category).IsEqualTo("Acme.Widgets");
		await Assert.That(descriptor.DefaultSeverity).IsEqualTo(DiagnosticSeverity.Error);
		await Assert.That(descriptor.Description.ToString(CultureInfo.InvariantCulture)).IsEqualTo("A description.");
		await Assert.That(descriptor.HelpLinkUri).IsEqualTo("https://example.invalid/acme9999");
	}

	[Test]
	public async Task Options_GivenIncludeStackTrace_PutsTheStackTraceInTheMessage()
	{
		ComponentGuard.TryRun<SampleGenerator>(
			static () => throw new InvalidOperationException("boom"),
			out var withoutTrace
		);

		ComponentGuard.TryRun<SampleGenerator>(
			static () => throw new InvalidOperationException("boom"),
			out var withTrace,
			new ComponentFailureOptions { Id = "ACME9998", IncludeStackTrace = true }
		);

		await Assert
			.That(withoutTrace!.GetMessage(CultureInfo.InvariantCulture))
			.DoesNotContain(nameof(ComponentGuardTests));
		await Assert.That(withTrace!.GetMessage(CultureInfo.InvariantCulture)).Contains(nameof(ComponentGuardTests));
	}

	/// <summary>
	/// A diagnostic message spanning lines breaks the single-line shape every consumer of the build log
	/// expects, so the newlines a stack trace carries are flattened.
	/// </summary>
	[Test]
	public async Task Describe_FlattensNewlinesInTheMessage()
	{
		var diagnostic = ComponentGuard.Describe(
			typeof(SampleGenerator),
			new InvalidOperationException("first\r\nsecond")
		);

		await Assert.That(diagnostic.GetMessage(CultureInfo.InvariantCulture)).DoesNotContain("\n");
		await Assert.That(diagnostic.GetMessage(CultureInfo.InvariantCulture)).Contains("first");
		await Assert.That(diagnostic.GetMessage(CultureInfo.InvariantCulture)).Contains("second");
	}

	[Test]
	public async Task ResolveComponentType_GivenALambda_WalksOutToTheDeclaringComponent()
	{
		Action callback = static () => { };

		await Assert
			.That(ComponentGuard.ResolveComponentType(callback, typeof(string)))
			.IsEqualTo(typeof(ComponentGuardTests));
	}

	// ---------------------------------------------------------------------------------------------
	// Fixtures
	// ---------------------------------------------------------------------------------------------

	sealed class SampleGenerator : IIncrementalGenerator
	{
		public void Initialize(IncrementalGeneratorInitializationContext context) { }
	}

	sealed class SampleAnalyzer : DiagnosticAnalyzer
	{
		public override System.Collections.Immutable.ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [];

		public override void Initialize(AnalysisContext context) { }
	}

	sealed class SampleSuppressor : DiagnosticSuppressor
	{
		public override System.Collections.Immutable.ImmutableArray<SuppressionDescriptor> SupportedSuppressions => [];

		public override void ReportSuppressions(SuppressionAnalysisContext context) { }
	}

	sealed class SampleCodeFix : CodeFixProvider
	{
		public override System.Collections.Immutable.ImmutableArray<string> FixableDiagnosticIds => [];

		public override Task RegisterCodeFixesAsync(CodeFixContext context) => Task.CompletedTask;
	}

	/// <summary>
	/// A stand-in <see cref="Type"/> for prefix derivation, so a test does not need a real type per namespace.
	/// </summary>
	sealed class FakeType : TypeDelegator
	{
		readonly string? _namespace;

		FakeType(string? @namespace)
			: base(typeof(object)) => _namespace = @namespace;

		public override string? Namespace => _namespace;

		public static Type InNamespace(string? @namespace) => new FakeType(@namespace);
	}
}
