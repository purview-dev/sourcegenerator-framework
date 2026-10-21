using Purview.SourceGeneratorFramework.Testing.TUnit;

namespace Purview.SourceGeneratorFramework.Analyzers;

public sealed class AvoidSymbolsInPipelineModelAnalyzerTests
	: TUnitDiagnosticAnalyzerTestBase<AvoidSymbolsInPipelineModelAnalyzer>
{
	[Test]
	public async Task ModelRetainingSymbol_ReportsDiagnostic(CancellationToken cancellationToken)
	{
		const string source = """
			using Microsoft.CodeAnalysis;

			public class MyGenerator : IIncrementalGenerator
			{
				public void Initialize(IncrementalGeneratorInitializationContext context)
				{
					var pipeline = GetPipeline(context);
				}

				IncrementalValuesProvider<MyModel> GetPipeline(IncrementalGeneratorInitializationContext context) =>
					context.SyntaxProvider.ForAttributeWithMetadataName(
						"System.ObsoleteAttribute",
						static (node, _) => true,
						static (ctx, _) => new MyModel(ctx.TargetSymbol)
					);
			}

			public sealed record MyModel(ISymbol Symbol);
			""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasDiagnostic(AvoidSymbolsInPipelineModelAnalyzer.Rule.Id);
	}

	[Test]
	public async Task ModelWithValueMembers_DoesNotReportDiagnostic(CancellationToken cancellationToken)
	{
		const string source = """
			using Microsoft.CodeAnalysis;

			public class MyGenerator : IIncrementalGenerator
			{
				public void Initialize(IncrementalGeneratorInitializationContext context)
				{
					var pipeline = GetPipeline(context);
				}

				IncrementalValuesProvider<MyModel> GetPipeline(IncrementalGeneratorInitializationContext context) =>
					context.SyntaxProvider.ForAttributeWithMetadataName(
						"System.ObsoleteAttribute",
						static (node, _) => true,
						static (ctx, _) => new MyModel(ctx.TargetSymbol.Name)
					);
			}

			public sealed record MyModel(string Name);
			""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasNoDiagnostics();
	}

	[Test]
	public async Task NonPipelineTypeRetainingSymbol_DoesNotReportDiagnostic(CancellationToken cancellationToken)
	{
		const string source = """
			using Microsoft.CodeAnalysis;

			public sealed record NotAModel(ISymbol Symbol);
			""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasNoDiagnostics();
	}

	/// <summary>
	/// The shape the framework's own generators used: the transform returns the symbol directly, so there
	/// is no named model type for <c>AnalyzeModelType</c> to walk and the rule used to stay silent.
	/// </summary>
	[Test]
	public async Task TransformReturningSymbolDirectly_ReportsDiagnostic(CancellationToken cancellationToken)
	{
		const string source = """
			using Microsoft.CodeAnalysis;

			public class MyGenerator : IIncrementalGenerator
			{
				public void Initialize(IncrementalGeneratorInitializationContext context)
				{
					var pipeline = GetPipeline(context);
				}

				IncrementalValuesProvider<INamedTypeSymbol> GetPipeline(IncrementalGeneratorInitializationContext context) =>
					context.SyntaxProvider.ForAttributeWithMetadataName(
						"System.ObsoleteAttribute",
						static (node, _) => true,
						static (ctx, _) => (INamedTypeSymbol)ctx.TargetSymbol
					);
			}
			""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasDiagnostic(AvoidSymbolsInPipelineModelAnalyzer.Rule.Id);
	}

	/// <summary>
	/// A tuple carrying the symbol through a combine. The tuple is not a named model type either.
	/// </summary>
	[Test]
	public async Task TupleCarryingSymbolThroughCombine_ReportsDiagnostic(CancellationToken cancellationToken)
	{
		const string source = """
			using Microsoft.CodeAnalysis;

			public class MyGenerator : IIncrementalGenerator
			{
				public void Initialize(IncrementalGeneratorInitializationContext context)
				{
					var pipeline = GetPipeline(context);
				}

				IncrementalValuesProvider<(INamedTypeSymbol Symbol, string Name)> GetPipeline(
					IncrementalGeneratorInitializationContext context
				) =>
					context.SyntaxProvider.ForAttributeWithMetadataName(
						"System.ObsoleteAttribute",
						static (node, _) => true,
						static (ctx, _) => ((INamedTypeSymbol)ctx.TargetSymbol, ctx.TargetSymbol.Name)
					);
			}
			""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasDiagnostic(AvoidSymbolsInPipelineModelAnalyzer.Rule.Id);
	}

	/// <summary>
	/// A <c>Select</c> that projects the compilation into the pipeline value.
	/// </summary>
	[Test]
	public async Task SelectProjectingCompilation_ReportsDiagnostic(CancellationToken cancellationToken)
	{
		const string source = """
			using Microsoft.CodeAnalysis;

			public class MyGenerator : IIncrementalGenerator
			{
				public void Initialize(IncrementalGeneratorInitializationContext context)
				{
					var pipeline = context.CompilationProvider.Select(static (compilation, _) => compilation);
				}
			}
			""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasDiagnostic(AvoidSymbolsInPipelineModelAnalyzer.Rule.Id);
	}

	/// <summary>
	/// A transform that extracts an equatable value is the fix, and must stay silent.
	/// </summary>
	[Test]
	public async Task TransformReturningEquatableValue_DoesNotReportDiagnostic(CancellationToken cancellationToken)
	{
		const string source = """
			using Microsoft.CodeAnalysis;

			public class MyGenerator : IIncrementalGenerator
			{
				public void Initialize(IncrementalGeneratorInitializationContext context)
				{
					var pipeline = GetPipeline(context);
				}

				IncrementalValuesProvider<string> GetPipeline(IncrementalGeneratorInitializationContext context) =>
					context.SyntaxProvider.ForAttributeWithMetadataName(
						"System.ObsoleteAttribute",
						static (node, _) => true,
						static (ctx, _) => ctx.TargetSymbol.ToDisplayString()
					);
			}
			""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasNoDiagnostics();
	}
}
