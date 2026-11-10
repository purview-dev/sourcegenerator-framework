using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using TUnit.Assertions.Core;

namespace Purview.SourceGeneratorFramework.Testing.TUnit.Assertions;

/// <summary>
/// Base class for node-chain assertions. The chain value is the matched
/// <see cref="CodeQueryResult{T}"/>, and awaiting the assertion yields that same non-null result.
/// </summary>
/// <remarks>
/// Closed on .NET 11: the assertion chain is a fixed set defined by this package, so the base exists
/// for the sealed assertion types rather than as a consumer extension point. C# 15's <c>closed</c>
/// keeps the type implicitly abstract; the other targets keep the previous abstract declaration
/// because the compiler attribute the modifier emits is only shipped by .NET 11.
/// </remarks>
#if NET11_0_OR_GREATER
public closed class CodeQueryResultAssertion<T>(AssertionContext<CodeQueryResult<T>> context)
#else
public abstract class CodeQueryResultAssertion<T>(AssertionContext<CodeQueryResult<T>> context)
#endif
	: Assertion<CodeQueryResult<T>>(context)
	where T : SyntaxNode
{
	/// <summary>
	/// Awaits the assertion and returns the matched non-null <see cref="CodeQueryResult{T}"/>.
	/// </summary>
	public new TaskAwaiter<CodeQueryResult<T>> GetAwaiter() => ExecuteAndReturnResultAsync().GetAwaiter();

	async Task<CodeQueryResult<T>> ExecuteAndReturnResultAsync()
	{
		var value = await AssertAsync();
		return value!;
	}
}
