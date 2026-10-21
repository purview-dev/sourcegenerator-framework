using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Purview.SourceGeneratorFramework.Analyzers;

/// <summary>
/// Flags a <c>throw</c> in code that runs during source generation.
/// </summary>
/// <remarks>
/// <para>
/// An exception escaping a source generator is not a private failure. Roslyn catches it and reports
/// <c>CS8785</c> — "Generator 'X' failed to generate source" — in every build and in every IDE that loads the
/// project, and the generator produces <em>no</em> output for that compilation, so the consumer sees a wall
/// of unrelated "type or namespace not found" errors alongside it. The consumer cannot act on any of it: the
/// stack trace names the generator's internals, not the code that triggered it.
/// </para>
/// <para>
/// A generator always has a reporting channel that does not have those properties. Carry the failure on a
/// <c>GeneratorResult&lt;T&gt;</c> as a <c>ReportableDiagnostic</c> and let the source-output stage report it,
/// or call <c>SourceProductionContext.ReportDiagnostic</c> directly. Either way the consumer gets a
/// diagnostic pointing at their own code, with an id they can suppress or configure, and the rest of the
/// generator's output still arrives.
/// </para>
/// <para>
/// Two scopes are checked, because generation code is not all written in one place: anything lexically inside
/// a type implementing <see cref="IIncrementalGenerator"/> (the <c>Initialize</c> body, the pipeline lambdas
/// and the emit helpers that hang off the generator), and any lambda handed to a pipeline method, wherever it
/// is written.
/// </para>
/// <para>
/// Cancellation is unaffected: it is signalled by <c>CancellationToken.ThrowIfCancellationRequested()</c>, a
/// method call rather than a <c>throw</c>, and Roslyn expects that exception to propagate.
/// </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DoNotThrowFromGeneratorAnalyzer : DiagnosticAnalyzer
{
	public const string DiagnosticId = "PSGFR44";

	public static readonly DiagnosticDescriptor Rule = new(
		DiagnosticId,
		"Source generators must not throw",
		"{0} throws {1}; an exception escaping a generator is reported as CS8785 in every consuming build and IDE, and the generator produces no output at all for that compilation. Report a diagnostic instead - carry it on GeneratorResult<T> as a ReportableDiagnostic, call SourceProductionContext.ReportDiagnostic, or wrap the work in ComponentGuard.Run so the failure is reported for you.",
		"Purview.SourceGeneratorFramework",
		DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		description: "Code that runs during source generation must report failures as diagnostics rather than throwing. An escaping exception surfaces as CS8785, loses every generated file for that compilation, and gives the consumer a stack trace through the generator's internals instead of a diagnostic on their own code."
	);

	/// <summary>
	/// The pipeline methods whose lambda arguments run during generation. Mirrors
	/// <see cref="PreferStaticLambdaInPipelineAnalyzer"/>, plus the registration methods that take a callback.
	/// </summary>
	static readonly HashSet<string> PipelineMethodNames =
	[
		"Select",
		"SelectMany",
		"Where",
		"Combine",
		"Collect",
		"ForAttributeWithMetadataName",
		"CreateSyntaxProvider",
		"CombineWith",
		"CollectWith",
		"CombineWithContext",
		"RegisterSourceOutput",
		"RegisterImplementationSourceOutput",
		"RegisterPostInitializationOutput",
	];

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

	public override void Initialize(AnalysisContext context)
	{
		if (context is null)
			throw new ArgumentNullException(nameof(context));

		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();

		context.RegisterCompilationStartAction(static startContext =>
		{
			var generatorInterface = startContext.Compilation.GetTypeByMetadataName(
				"Microsoft.CodeAnalysis.IIncrementalGenerator"
			);

			// Nothing in this compilation can be a generator, so there is nothing to check.
			if (generatorInterface is null)
				return;

			var cancellation = startContext.Compilation.GetTypeByMetadataName("System.OperationCanceledException");

			startContext.RegisterSyntaxNodeAction(
				nodeContext => AnalyzeThrow(nodeContext, generatorInterface, cancellation),
				SyntaxKind.ThrowStatement,
				SyntaxKind.ThrowExpression
			);
		});
	}

	static void AnalyzeThrow(
		SyntaxNodeAnalysisContext context,
		INamedTypeSymbol generatorInterface,
		INamedTypeSymbol? cancellation
	)
	{
		if (IsCancellation(context, cancellation))
			return;

		if (!TryDescribeScope(context, generatorInterface, out var scope))
			return;

		context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation(), scope, DescribeThrown(context)));
	}

	/// <summary>
	/// Determines whether the <c>throw</c> is propagating cancellation, which must never be reported.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Roslyn cancels analysis on every keystroke and expects an <see cref="OperationCanceledException"/>
	/// to reach it. Two shapes count: throwing one directly, and the far more common
	/// <c>catch (OperationCanceledException) { throw; }</c> that sits in front of a broad
	/// <c>catch (Exception)</c> precisely so cancellation is not swallowed by it.
	/// </para>
	/// <para>
	/// A bare <c>throw;</c> is only exempt because of the clause it sits in, so the enclosing
	/// <c>catch</c> has to be resolved rather than the throw read on its own — the rule would otherwise
	/// push an author to delete the one piece of cancellation handling that is correct.
	/// </para>
	/// </remarks>
	static bool IsCancellation(SyntaxNodeAnalysisContext context, INamedTypeSymbol? cancellation)
	{
		if (cancellation is null)
			return false;

		var thrown = context.Node switch
		{
			ThrowStatementSyntax statement => statement.Expression,
			ThrowExpressionSyntax expression => expression.Expression,
			_ => null,
		};

		if (thrown is not null)
		{
			return IsCancellationType(
				context.SemanticModel.GetTypeInfo(thrown, context.CancellationToken).Type,
				cancellation
			);
		}

		// A rethrow. Its type is whatever the enclosing catch clause filters on.
		foreach (var ancestor in context.Node.Ancestors())
		{
			// Stop at the first lambda or method body: a rethrow cannot cross one.
			if (ancestor is AnonymousFunctionExpressionSyntax or MemberDeclarationSyntax)
				return false;

			if (ancestor is not CatchClauseSyntax { Declaration.Type: { } caughtType })
				continue;

			return IsCancellationType(
				context.SemanticModel.GetTypeInfo(caughtType, context.CancellationToken).Type,
				cancellation
			);
		}

		return false;
	}

	/// <summary>
	/// Whether <paramref name="type"/> is <see cref="OperationCanceledException"/> or derives from it,
	/// which covers <c>TaskCanceledException</c>.
	/// </summary>
	static bool IsCancellationType(ITypeSymbol? type, INamedTypeSymbol cancellation)
	{
		for (var current = type; current is not null; current = current.BaseType)
		{
			if (SymbolEqualityComparer.Default.Equals(current, cancellation))
				return true;
		}

		return false;
	}

	/// <summary>
	/// Describes the generation scope the <c>throw</c> sits in, or returns <see langword="false"/> when it
	/// does not run during generation.
	/// </summary>
	static bool TryDescribeScope(
		SyntaxNodeAnalysisContext context,
		INamedTypeSymbol generatorInterface,
		out string scope
	)
	{
		foreach (var current in context.Node.AncestorsAndSelf())
		{
			if (
				current is AnonymousFunctionExpressionSyntax lambda
				&& TryGetPipelineMethodName(context, lambda, out var pipelineMethod)
			)
			{
				scope = $"The lambda passed to '{pipelineMethod}'";
				return true;
			}

			if (current is not TypeDeclarationSyntax declaration)
				continue;

			if (
				context.SemanticModel.GetDeclaredSymbol(declaration, context.CancellationToken) is INamedTypeSymbol type
				&& type.AllInterfaces.Any(implemented =>
					SymbolEqualityComparer.Default.Equals(implemented, generatorInterface)
				)
			)
			{
				scope = $"Generator '{type.Name}'";
				return true;
			}
		}

		scope = string.Empty;
		return false;
	}

	/// <summary>
	/// Names the thrown exception for the message, falling back to a description of a rethrow.
	/// </summary>
	static string DescribeThrown(SyntaxNodeAnalysisContext context)
	{
		var thrown = context.Node switch
		{
			ThrowStatementSyntax statement => statement.Expression,
			ThrowExpressionSyntax expression => expression.Expression,
			_ => null,
		};

		// A bare 'throw;' rethrows whatever was caught, which escapes the generator just the same.
		if (thrown is null)
			return "the caught exception";

		return context.SemanticModel.GetTypeInfo(thrown, context.CancellationToken).Type is { } type
			? $"'{type.Name}'"
			: "an exception";
	}

	static bool TryGetPipelineMethodName(
		SyntaxNodeAnalysisContext context,
		AnonymousFunctionExpressionSyntax lambda,
		out string methodName
	)
	{
		methodName = string.Empty;

		if (lambda.Parent is not ArgumentSyntax)
			return false;

		if (lambda.Parent?.Parent?.Parent is not InvocationExpressionSyntax invocation)
			return false;

		if (
			context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol
			is not IMethodSymbol method
		)
			return false;

		if (!PipelineMethodNames.Contains(method.Name))
			return false;

		// Pipeline methods come from Roslyn or from the framework's own helpers; this keeps LINQ's
		// Select/Where and anything else sharing a name out.
		var containingType = method.ContainingType;
		if (containingType is null)
			return false;

		var containingNamespace = containingType.ContainingNamespace?.ToDisplayString();
		if (
			containingNamespace != "Microsoft.CodeAnalysis"
			&& containingType.Name != "IncrementalPipeline"
			&& containingType.Name != "IncrementalValueProviderExtensions"
			&& containingType.Name != "IncrementalGeneratorInitializationContextExtensions"
		)
		{
			return false;
		}

		methodName = method.Name;
		return true;
	}
}
