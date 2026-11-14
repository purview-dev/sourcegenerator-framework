using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Purview.SourceGeneratorFramework.Analyzers;

namespace Purview.SourceGeneratorFramework.CodeFixers;

/// <summary>
/// Rewrites a raw <c>CodeWriter</c> text call that writes a single statement to the matching structured
/// statement API, such as <c>writer.Line("return value;")</c> to <c>writer.Return("value")</c>
/// (<c>PSGFR19</c>). Statements whose structured API needs a body, such as <c>if</c>, are left for the
/// block-oriented fixers because a single raw line does not describe the body.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(PreferStructuredCodeWriterStatementCodeFixProvider))]
public sealed class PreferStructuredCodeWriterStatementCodeFixProvider : CodeFixProvider
{
	internal const string EquivalenceKey = "UseStructuredStatement";

	public override ImmutableArray<string> FixableDiagnosticIds => ["PSGFR19"];

	public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

	public override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
		var semanticModel = await context
			.Document.GetSemanticModelAsync(context.CancellationToken)
			.ConfigureAwait(false);
		if (root is null || semanticModel is null)
			return;

		foreach (var diagnostic in context.Diagnostics)
		{
			var node = root.FindNode(diagnostic.Location.SourceSpan);
			if (node is not InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax } invocation)
				continue;

			if (invocation.ArgumentList.Arguments.FirstOrDefault()?.Expression is not ExpressionSyntax expression)
				continue;

			// The classifier concatenates only the literal parts of an interpolated string, so rewriting one
			// with runtime holes would silently drop those values.
			if (
				expression is InterpolatedStringExpressionSyntax
				&& !semanticModel.GetConstantValue(expression, context.CancellationToken).HasValue
			)
				continue;

			if (
				!CodeWriterLiteralClassifier.TryGetLiteralText(
					expression,
					semanticModel,
					context.CancellationToken,
					out var text
				) || text is null
			)
				continue;

			if (!TryGetReplacement(invocation, text, out var replacement, out var actionName))
				continue;

			context.RegisterCodeFix(
				CodeAction.Create(
					actionName,
					_ => Task.FromResult(context.Document.WithSyntaxRoot(root.ReplaceNode(invocation, replacement))),
					EquivalenceKey
				),
				diagnostic
			);
		}
	}

	static bool TryGetReplacement(
		InvocationExpressionSyntax invocation,
		string text,
		out InvocationExpressionSyntax replacement,
		out string actionName
	)
	{
		replacement = invocation;
		actionName = string.Empty;

		var member = (MemberAccessExpressionSyntax)invocation.Expression;
		var receiver = member.Expression;
		var trimmed = text.Trim();
		if (trimmed.Length == 0)
			return false;

		// An XML documentation comment must keep its /// prefix; Comment() would turn it into a line comment.
		if (trimmed.StartsWith("///", StringComparison.Ordinal))
			return false;

		if (trimmed.StartsWith("//", StringComparison.Ordinal))
		{
			var comment = trimmed.Substring(2).TrimStart();
			return Build(
				receiver,
				invocation,
				member,
				"Comment",
				"Use Comment()",
				[StringLiteral(comment)],
				out replacement,
				out actionName
			);
		}

		if (
			TryUseDirective(trimmed, out var usingApi, out var usingArguments, out var usingAction)
			&& Build(
				receiver,
				invocation,
				member,
				usingApi,
				usingAction,
				usingArguments,
				out replacement,
				out actionName
			)
		)
			return true;

		if (trimmed.StartsWith("return ", StringComparison.Ordinal) && trimmed.EndsWith(";", StringComparison.Ordinal))
		{
			var expression = trimmed.Substring("return ".Length, trimmed.Length - "return ".Length - 1).Trim();
			return Build(
				receiver,
				invocation,
				member,
				"Return",
				"Use Return()",
				[StringLiteral(expression)],
				out replacement,
				out actionName
			);
		}

		if (trimmed == "return;")
			return Build(receiver, invocation, member, "Return", "Use Return()", [], out replacement, out actionName);

		if (trimmed.StartsWith("throw ", StringComparison.Ordinal) && trimmed.EndsWith(";", StringComparison.Ordinal))
		{
			var expression = trimmed.Substring("throw ".Length, trimmed.Length - "throw ".Length - 1).Trim();
			return Build(
				receiver,
				invocation,
				member,
				"Throw",
				"Use Throw()",
				[StringLiteral(expression)],
				out replacement,
				out actionName
			);
		}

		var isAwaited = trimmed.StartsWith("await ", StringComparison.Ordinal);
		var statement = isAwaited ? trimmed.Substring("await ".Length).Trim() : trimmed;
		if (!statement.EndsWith(";", StringComparison.Ordinal))
			return false;

		var expressionText = statement.Substring(0, statement.Length - 1).Trim();
		if (expressionText.Length == 0)
			return false;

		var parsed = SyntaxFactory.ParseExpression(expressionText);
		if (isAwaited)
		{
			if (parsed is not InvocationExpressionSyntax awaitedCall)
				return false;

			// The awaited call is a separate expression from the invocation, so we need to build the replacement from it instead of the original invocation.
			return BuildCall(
				receiver,
				invocation,
				member,
				awaitedCall,
				isAwaited: true,
				out replacement,
				out actionName
			);
		}

		if (parsed is InvocationExpressionSyntax call)
		{
			return BuildCall(receiver, invocation, member, call, isAwaited: false, out replacement, out actionName);
		}

		if (parsed is AssignmentExpressionSyntax { RawKind: (int)SyntaxKind.SimpleAssignmentExpression } assignment)
		{
			return Build(
				receiver,
				invocation,
				member,
				"Assignment",
				"Use Assignment()",
				[StringLiteral(assignment.Left.ToString()), StringLiteral(assignment.Right.ToString())],
				out replacement,
				out actionName
			);
		}

		// A simple expression that is not a method call or assignment can be treated as a method call with no arguments.
		return false;
	}

	static bool TryUseDirective(string trimmed, out string api, out ExpressionSyntax[] arguments, out string actionName)
	{
		api = string.Empty;
		arguments = [];
		actionName = string.Empty;

		if (!trimmed.EndsWith(";", StringComparison.Ordinal))
			return false;

		if (trimmed.StartsWith("global using ", StringComparison.Ordinal))
		{
			var globalTarget = trimmed
				.Substring("global using ".Length, trimmed.Length - "global using ".Length - 1)
				.Trim();
			if (globalTarget.Length == 0 || globalTarget.StartsWith("static ", StringComparison.Ordinal))
				return false;

			api = "Using";
			arguments = [StringLiteral(globalTarget), BooleanLiteral(true)];
			actionName = "Use Using()";
			return true;
		}

		if (!trimmed.StartsWith("using ", StringComparison.Ordinal))
			return false;

		var body = trimmed.Substring("using ".Length, trimmed.Length - "using ".Length - 1).Trim();
		if (body.Length == 0 || body.StartsWith("static ", StringComparison.Ordinal))
			return false;

		var equalsIndex = body.IndexOf(" = ", StringComparison.Ordinal);
		if (equalsIndex < 0)
		{
			api = "Using";
			arguments = [StringLiteral(body)];
			actionName = "Use Using()";
			return true;
		}

		var alias = body.Substring(0, equalsIndex).Trim();
		var target = body.Substring(equalsIndex + " = ".Length).Trim();
		if (alias.Length == 0 || target.Length == 0)
			return false;

		api = "UsingAlias";
		arguments = [StringLiteral(alias), StringLiteral(target)];
		actionName = "Use UsingAlias()";
		return true;
	}

	static bool BuildCall(
		ExpressionSyntax receiver,
		InvocationExpressionSyntax invocation,
		MemberAccessExpressionSyntax member,
		InvocationExpressionSyntax call,
		bool isAwaited,
		out InvocationExpressionSyntax replacement,
		out string actionName
	)
	{
		replacement = invocation;
		actionName = string.Empty;

		string? callReceiver;
		string methodName;
		switch (call.Expression)
		{
			case IdentifierNameSyntax identifier:
				callReceiver = null;
				methodName = identifier.Identifier.Text;
				break;
			case GenericNameSyntax generic:
				callReceiver = null;
				methodName = generic.ToString();
				break;
			case MemberAccessExpressionSyntax access:
				callReceiver = access.Expression.ToString();
				methodName = access.Name.ToString();
				break;
			default:
				return false;
		}

		var api = (isAwaited, callReceiver is null) switch
		{
			(true, true) => "AwaitedMethodCall",
			(true, false) => "AwaitedMethodCallOn",
			(false, true) => "MethodCall",
			(false, false) => "MethodCallOn",
		};

		List<ExpressionSyntax> arguments = [];
		if (callReceiver is not null)
			arguments.Add(StringLiteral(callReceiver));
		arguments.Add(StringLiteral(methodName));
		foreach (var argument in call.ArgumentList.Arguments)
			arguments.Add(StringLiteral(argument.ToString()));

		return Build(
			receiver,
			invocation,
			member,
			api,
			$"Use {api}()",
			[.. arguments],
			out replacement,
			out actionName
		);
	}

	static bool Build(
		ExpressionSyntax receiver,
		InvocationExpressionSyntax invocation,
		MemberAccessExpressionSyntax member,
		string api,
		string action,
		ExpressionSyntax[] arguments,
		out InvocationExpressionSyntax replacement,
		out string actionName
	)
	{
		var newMemberAccess = SyntaxFactory
			.MemberAccessExpression(
				SyntaxKind.SimpleMemberAccessExpression,
				receiver,
				SyntaxFactory.IdentifierName(api)
			)
			.WithTriviaFrom(member);

		var newArguments = SyntaxFactory.ArgumentList(
			SyntaxFactory.SeparatedList(arguments.Select(SyntaxFactory.Argument))
		);

		replacement = SyntaxFactory.InvocationExpression(newMemberAccess, newArguments).WithTriviaFrom(invocation);
		actionName = action;
		return true;
	}

	static LiteralExpressionSyntax StringLiteral(string value) =>
		SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(value));

	static LiteralExpressionSyntax BooleanLiteral(bool value) =>
		SyntaxFactory.LiteralExpression(value ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression);
}
