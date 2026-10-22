using Purview.SourceGeneratorFramework.Testing.TUnit;

namespace Purview.SourceGeneratorFramework.Analyzers;

public sealed class DoNotThrowFromGeneratorAnalyzerTests
	: TUnitDiagnosticAnalyzerTestBase<DoNotThrowFromGeneratorAnalyzer>
{
	[Test]
	public async Task ThrowInInitialize_ReportsDiagnostic(CancellationToken cancellationToken)
	{
		const string source = """
			using System;
			using Microsoft.CodeAnalysis;

			public class MyGenerator : IIncrementalGenerator
			{
				public void Initialize(IncrementalGeneratorInitializationContext context)
				{
					throw new InvalidOperationException("nope");
				}
			}
			""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasDiagnostic(DoNotThrowFromGeneratorAnalyzer.Rule.Id);
	}

	/// <summary>
	/// The shape the framework's own generator used: a <c>throw</c> in a switch expression inside an emit
	/// helper, several calls away from the pipeline and not inside any lambda.
	/// </summary>
	[Test]
	public async Task ThrowInAGeneratorHelperMethod_ReportsDiagnostic(CancellationToken cancellationToken)
	{
		const string source = """
			using System;
			using Microsoft.CodeAnalysis;

			public class MyGenerator : IIncrementalGenerator
			{
				public void Initialize(IncrementalGeneratorInitializationContext context) { }

				static string Describe(int kind) =>
					kind switch
					{
						0 => "zero",
						_ => throw new InvalidOperationException("unsupported"),
					};
			}
			""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasDiagnostic(DoNotThrowFromGeneratorAnalyzer.Rule.Id);
	}

	/// <summary>
	/// A lambda handed to a pipeline method runs during generation wherever it is written, so the rule does
	/// not depend on it sitting inside the generator type.
	/// </summary>
	[Test]
	public async Task ThrowInAPipelineLambdaOutsideAGeneratorType_ReportsDiagnostic(CancellationToken cancellationToken)
	{
		const string source = """
			using System;
			using Microsoft.CodeAnalysis;

			public static class PipelineHelper
			{
				public static IncrementalValuesProvider<string> Build(
					IncrementalGeneratorInitializationContext context
				) =>
					context.SyntaxProvider.ForAttributeWithMetadataName(
						"System.ObsoleteAttribute",
						static (node, _) => true,
						static (ctx, _) =>
						{
							if (ctx.TargetSymbol.Name.Length == 0)
								throw new InvalidOperationException("nope");

							return ctx.TargetSymbol.Name;
						}
					);
			}
			""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasDiagnostic(DoNotThrowFromGeneratorAnalyzer.Rule.Id);
	}

	[Test]
	public async Task RethrowInAGenerator_ReportsDiagnostic(CancellationToken cancellationToken)
	{
		const string source = """
			using System;
			using Microsoft.CodeAnalysis;

			public class MyGenerator : IIncrementalGenerator
			{
				public void Initialize(IncrementalGeneratorInitializationContext context)
				{
					try
					{
						Work();
					}
					catch (InvalidOperationException)
					{
						throw;
					}
				}

				static void Work() { }
			}
			""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasDiagnostic(DoNotThrowFromGeneratorAnalyzer.Rule.Id);
	}

	/// <summary>
	/// The cancellation guard in front of a broad catch: the single most correct thing a generator can do,
	/// and the shape this rule flagged as a defect until a consuming repository caught it.
	/// </summary>
	[Test]
	public async Task CancellationRethrowAheadOfABroadCatch_DoesNotReportDiagnostic(CancellationToken cancellationToken)
	{
		const string source = """
			using System;
			using Microsoft.CodeAnalysis;

			public class MyGenerator : IIncrementalGenerator
			{
				static readonly DiagnosticDescriptor Rule = new(
					"ABC0001",
					"t",
					"t",
					"c",
					DiagnosticSeverity.Warning,
					isEnabledByDefault: true
				);

				public void Initialize(IncrementalGeneratorInitializationContext context) { }

				static void ExecuteSafely(SourceProductionContext context)
				{
					try
					{
						Emit(context);
					}
					catch (OperationCanceledException)
					{
						throw;
					}
					catch (Exception)
					{
						context.ReportDiagnostic(Diagnostic.Create(Rule, Location.None));
					}
				}

				static void Emit(SourceProductionContext context) { }
			}
			""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasNoDiagnostics();
	}

	/// <summary>
	/// <c>TaskCanceledException</c> derives from <c>OperationCanceledException</c>, so it is cancellation too.
	/// </summary>
	[Test]
	public async Task TaskCanceledRethrow_DoesNotReportDiagnostic(CancellationToken cancellationToken)
	{
		const string source = """
			using System;
			using System.Threading.Tasks;
			using Microsoft.CodeAnalysis;

			public class MyGenerator : IIncrementalGenerator
			{
				public void Initialize(IncrementalGeneratorInitializationContext context)
				{
					try
					{
						Work();
					}
					catch (TaskCanceledException)
					{
						throw;
					}
				}

				static void Work() { }
			}
			""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasNoDiagnostics();
	}

	/// <summary>
	/// Throwing cancellation directly is also propagation, not a defect.
	/// </summary>
	[Test]
	public async Task ThrowingOperationCanceledDirectly_DoesNotReportDiagnostic(CancellationToken cancellationToken)
	{
		const string source = """
			using System;
			using Microsoft.CodeAnalysis;

			public class MyGenerator : IIncrementalGenerator
			{
				public void Initialize(IncrementalGeneratorInitializationContext context)
				{
					throw new OperationCanceledException();
				}
			}
			""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasNoDiagnostics();
	}

	/// <summary>
	/// A rethrow from a broad catch still escapes, and is still a defect.
	/// </summary>
	[Test]
	public async Task RethrowFromABroadCatch_StillReportsDiagnostic(CancellationToken cancellationToken)
	{
		const string source = """
			using System;
			using Microsoft.CodeAnalysis;

			public class MyGenerator : IIncrementalGenerator
			{
				public void Initialize(IncrementalGeneratorInitializationContext context)
				{
					try
					{
						Work();
					}
					catch (Exception)
					{
						throw;
					}
				}

				static void Work() { }
			}
			""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasDiagnostic(DoNotThrowFromGeneratorAnalyzer.Rule.Id);
	}

	/// <summary>
	/// Reporting a diagnostic is the sanctioned alternative and must stay silent.
	/// </summary>
	[Test]
	public async Task ReportingADiagnosticInsteadOfThrowing_DoesNotReportDiagnostic(CancellationToken cancellationToken)
	{
		const string source = """
			using Microsoft.CodeAnalysis;

			public class MyGenerator : IIncrementalGenerator
			{
				static readonly DiagnosticDescriptor Rule = new(
					"ABC0001",
					"t",
					"t",
					"c",
					DiagnosticSeverity.Warning,
					isEnabledByDefault: true
				);

				public void Initialize(IncrementalGeneratorInitializationContext context) =>
					context.RegisterSourceOutput(
						context.CompilationProvider,
						static (spc, compilation) => spc.ReportDiagnostic(Diagnostic.Create(Rule, Location.None))
					);
			}
			""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasNoDiagnostics();
	}

	/// <summary>
	/// An analyzer is not a generator: its exceptions become <c>AD0001</c>, which is a separate concern this
	/// rule deliberately does not cover, and its <c>Initialize</c> guard is the established pattern.
	/// </summary>
	[Test]
	public async Task ThrowInAnAnalyzer_DoesNotReportDiagnostic(CancellationToken cancellationToken)
	{
		const string source = """
			using System;
			using System.Collections.Immutable;
			using Microsoft.CodeAnalysis;
			using Microsoft.CodeAnalysis.Diagnostics;

			public class MyAnalyzer : DiagnosticAnalyzer
			{
				public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [];

				public override void Initialize(AnalysisContext context)
				{
					if (context is null)
						throw new ArgumentNullException(nameof(context));
				}
			}
			""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasNoDiagnostics();
	}

	/// <summary>
	/// An ordinary class that happens to sit in the same assembly is not generation code.
	/// </summary>
	[Test]
	public async Task ThrowInAnUnrelatedType_DoesNotReportDiagnostic(CancellationToken cancellationToken)
	{
		const string source = """
			using System;

			public static class Helper
			{
				public static void Validate(string value)
				{
					if (value is null)
						throw new ArgumentNullException(nameof(value));
				}
			}
			""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasNoDiagnostics();
	}

	/// <summary>
	/// LINQ shares method names with the pipeline, and a lambda passed to it is ordinary code.
	/// </summary>
	[Test]
	public async Task ThrowInALinqLambdaOutsideAGenerator_DoesNotReportDiagnostic(CancellationToken cancellationToken)
	{
		const string source = """
			using System;
			using System.Collections.Generic;
			using System.Linq;

			public static class Helper
			{
				public static IEnumerable<string> Run(IEnumerable<string> values) =>
					values.Select(static value => value ?? throw new InvalidOperationException("nope"));
			}
			""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasNoDiagnostics();
	}
}
