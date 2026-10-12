using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Purview.SourceGeneratorFramework.Analyzers;

/// <summary>
/// Flags a disposable Testing-harness result that is produced and then never disposed, which leaks the
/// collectible <c>AssemblyLoadContext</c> and <c>MetadataLoadContext</c> created for a compiled run.
/// </summary>
/// <remarks>
/// The rule is reported as a warning when the run syntactically opts into compiling to an assembly (a
/// <c>Compile</c> options member, or <c>CompileToAssembly = true</c>), because that is when the undisposed
/// result actually holds an emitted assembly; otherwise it is informational.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UndisposedTestResultAnalyzer : DiagnosticAnalyzer
{
	public const string DiagnosticId = "PSGFR42";

	/// <summary>
	/// Set on the diagnostic when the fix can turn the declaration into a <c>using</c> declaration.
	/// </summary>
	internal const string FixableProperty = "Fixable";

	const string TestingNamespace = "Purview.SourceGeneratorFramework.Testing";

	public static readonly DiagnosticDescriptor Rule = new(
		DiagnosticId,
		"Dispose test harness results with a using declaration",
		"The {0} produced by {1} is never disposed; declare it with 'using' so the compiled assembly and its load contexts are released",
		"Purview.SourceGeneratorFramework",
		DiagnosticSeverity.Info,
		isEnabledByDefault: true,
		description: "Test-harness results are disposable because a run that compiles to an assembly creates a collectible AssemblyLoadContext and a MetadataLoadContext. A result that is neither consumed by a using declaration nor handed to a caller leaks both for the lifetime of the test process."
	);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

	public override void Initialize(AnalysisContext context)
	{
		if (context is null)
			throw new ArgumentNullException(nameof(context));

		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterSyntaxNodeAction(AnalyzeLocalDeclaration, SyntaxKind.LocalDeclarationStatement);
		context.RegisterSyntaxNodeAction(AnalyzeExpressionStatement, SyntaxKind.ExpressionStatement);
	}

	static void AnalyzeLocalDeclaration(SyntaxNodeAnalysisContext context)
	{
		if (context.Node is not LocalDeclarationStatementSyntax declaration)
			return;

		// 'using var result = ...' and 'await using var result = ...' already own the result.
		if (!declaration.UsingKeyword.IsKind(SyntaxKind.None))
			return;

		foreach (var declarator in declaration.Declaration.Variables)
		{
			if (declarator.Initializer?.Value is not { } initializer)
				continue;

			if (Unwrap(initializer) is not InvocationExpressionSyntax and not ObjectCreationExpressionSyntax)
				continue;

			if (
				context.SemanticModel.GetDeclaredSymbol(declarator, context.CancellationToken) is not ILocalSymbol local
			)
				continue;

			if (!IsDisposableTestingResult(local.Type))
				continue;

			if (IsOwnedElsewhere(declarator, local, context))
				continue;

			context.ReportDiagnostic(
				Diagnostic.Create(
					Rule,
					declarator.Identifier.GetLocation(),
					ResolveSeverity(initializer),
					additionalLocations: null,
					properties: ImmutableDictionary<string, string?>.Empty.Add(FixableProperty, bool.TrueString),
					local.Type.Name,
					DescribeProducer(initializer)
				)
			);
		}
	}

	static void AnalyzeExpressionStatement(SyntaxNodeAnalysisContext context)
	{
		if (context.Node is not ExpressionStatementSyntax statement)
			return;

		var expression = statement.Expression;

		// '_ = await RunAsync(...)' discards the result just as completely as dropping it.
		if (
			expression is AssignmentExpressionSyntax
			{
				Left: IdentifierNameSyntax { Identifier.ValueText: "_" }
			} discard
		)
			expression = discard.Right;

		var produced = Unwrap(expression);
		if (produced is not InvocationExpressionSyntax and not ObjectCreationExpressionSyntax)
			return;

		var type = context.SemanticModel.GetTypeInfo(expression, context.CancellationToken).Type;
		if (!IsDisposableTestingResult(type))
			return;

		context.ReportDiagnostic(
			Diagnostic.Create(
				Rule,
				produced.GetLocation(),
				ResolveSeverity(expression),
				additionalLocations: null,
				properties: null,
				type!.Name,
				DescribeProducer(expression)
			)
		);
	}

	static ExpressionSyntax Unwrap(ExpressionSyntax expression) =>
		expression switch
		{
			AwaitExpressionSyntax await => Unwrap(await.Expression),
			ParenthesizedExpressionSyntax parenthesized => Unwrap(parenthesized.Expression),
			_ => expression,
		};

	static bool IsDisposableTestingResult(ITypeSymbol? type)
	{
		if (type is not INamedTypeSymbol named)
			return false;

		var @namespace = named.ContainingNamespace?.ToDisplayString();
		if (
			@namespace is null
			|| (
				!string.Equals(@namespace, TestingNamespace, StringComparison.Ordinal)
				&& !@namespace.StartsWith(TestingNamespace + ".", StringComparison.Ordinal)
			)
		)
			return false;

		return named.AllInterfaces.Any(static i =>
			i.ToDisplayString() is "System.IDisposable" or "System.IAsyncDisposable"
		);
	}

	/// <summary>
	/// Determines whether the local hands the result to someone else, or disposes it explicitly.
	/// </summary>
	/// <remarks>
	/// Reading members off the result (<c>result.GetSource(...)</c>) or passing it by value to an assertion
	/// does not transfer ownership: the declaring method still has to dispose it. Returning it, yielding it,
	/// assigning it elsewhere, or passing it by reference does.
	/// </remarks>
	static bool IsOwnedElsewhere(
		VariableDeclaratorSyntax declarator,
		ILocalSymbol local,
		SyntaxNodeAnalysisContext context
	)
	{
		var body = FindEnclosingBody(declarator);
		if (body is null)
			return true;

		foreach (var identifier in body.DescendantNodes().OfType<IdentifierNameSyntax>())
		{
			if (identifier.Identifier.ValueText != local.Name)
				continue;

			if (
				!SymbolEqualityComparer.Default.Equals(
					context.SemanticModel.GetSymbolInfo(identifier, context.CancellationToken).Symbol,
					local
				)
			)
				continue;

			if (Transfers(identifier))
				return true;
		}

		return false;
	}

	static bool Transfers(IdentifierNameSyntax identifier) =>
		identifier.Parent switch
		{
			ReturnStatementSyntax
			or YieldStatementSyntax
			or ArrowExpressionClauseSyntax
			or UsingStatementSyntax
			or EqualsValueClauseSyntax => true,
			AssignmentExpressionSyntax assignment => assignment.Right == identifier,
			ArgumentSyntax argument => !argument.RefKindKeyword.IsKind(SyntaxKind.None),
			MemberAccessExpressionSyntax member => member.Expression == identifier
				&& member.Name.Identifier.ValueText is "Dispose" or "DisposeAsync",
			ConditionalAccessExpressionSyntax conditional => conditional.Expression == identifier
				&& conditional.WhenNotNull.ToString().Contains("Dispose"),
			_ => false,
		};

	static SyntaxNode? FindEnclosingBody(SyntaxNode node)
	{
		for (var current = node.Parent; current is not null; current = current.Parent)
		{
			if (current is BaseMethodDeclarationSyntax method)
				return (SyntaxNode?)method.Body ?? method.ExpressionBody;

			if (current is LocalFunctionStatementSyntax localFunction)
				return (SyntaxNode?)localFunction.Body ?? localFunction.ExpressionBody;

			if (current is AnonymousFunctionExpressionSyntax lambda)
				return lambda.Body;

			if (current is AccessorDeclarationSyntax accessor)
				return (SyntaxNode?)accessor.Body ?? accessor.ExpressionBody;
		}

		return null;
	}

	/// <summary>
	/// Escalates the rule to a warning when the run syntactically compiles to an assembly, which is when the
	/// undisposed result actually holds a loaded assembly rather than only syntax trees.
	/// </summary>
	static DiagnosticSeverity ResolveSeverity(ExpressionSyntax producer) =>
		CompilesToAssembly(producer) ? DiagnosticSeverity.Warning : DiagnosticSeverity.Info;

	static bool CompilesToAssembly(SyntaxNode producer)
	{
		foreach (var node in producer.DescendantNodesAndSelf())
		{
			// '.Compile()' and the 'Options.Compile' convenience member both opt in.
			if (
				node
				is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Compile" }
					or IdentifierNameSyntax { Identifier.ValueText: "Compile" }
			)
				return true;

			if (
				node
					is AssignmentExpressionSyntax
					{
						Left: IdentifierNameSyntax { Identifier.ValueText: "CompileToAssembly" },
						Right: LiteralExpressionSyntax right,
					}
				&& right.IsKind(SyntaxKind.TrueLiteralExpression)
			)
				return true;
		}

		return false;
	}

	static string DescribeProducer(ExpressionSyntax producer) =>
		Unwrap(producer) switch
		{
			InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax member } => member
				.Name
				.Identifier
				.ValueText,
			InvocationExpressionSyntax { Expression: IdentifierNameSyntax name } => name.Identifier.ValueText,
			ObjectCreationExpressionSyntax creation => creation.Type.ToString(),
			_ => producer.ToString(),
		};
}
