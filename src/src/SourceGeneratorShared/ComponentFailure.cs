using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Purview.SourceGeneratorFramework;

/// <summary>
/// Builds the <see cref="DiagnosticDescriptor"/> a Roslyn component reports when it fails unexpectedly,
/// defaulting every part of it from the component's own identity.
/// </summary>
/// <remarks>
/// <para>
/// The defaults are derived from the consumer's codebase rather than from the framework, because the
/// diagnostic belongs to them: it appears in their build, they decide whether to suppress it, and an id in
/// the framework's own namespace would suggest the framework is at fault. A generator in
/// <c>Acme.Widgets.Generators</c> reports <c>AWG0001</c>, in category <c>Acme</c>.
/// </para>
/// <para>
/// An analyzer must declare this descriptor in <c>SupportedDiagnostics</c>, or Roslyn throws when it is
/// reported. <see cref="For{TComponent}(ComponentFailureOptions?)"/> is cached, so the instance an analyzer
/// declares is the same one the guard reports.
/// </para>
/// </remarks>
public static class ComponentFailure
{
	const string FallbackIdPrefix = "GEN";
	const string FallbackCategory = "SourceGeneration";

	static readonly ConcurrentDictionary<
		(Type Component, ComponentFailureOptions? Options),
		DiagnosticDescriptor
	> Descriptors = new();

	/// <summary>
	/// Gets the failure descriptor for <typeparamref name="TComponent"/>, creating it on first use.
	/// </summary>
	public static DiagnosticDescriptor For<TComponent>(ComponentFailureOptions? options = null) =>
		For(typeof(TComponent), options);

	/// <summary>
	/// Gets the failure descriptor for <paramref name="componentType"/>, creating it on first use.
	/// </summary>
	public static DiagnosticDescriptor For(Type componentType, ComponentFailureOptions? options = null)
	{
		if (componentType is null)
			throw new ArgumentNullException(nameof(componentType));

		return Descriptors.GetOrAdd((componentType, options), static key => Create(key.Component, key.Options));
	}

	static DiagnosticDescriptor Create(Type componentType, ComponentFailureOptions? options)
	{
		var kind = DescribeKind(componentType);

		return new DiagnosticDescriptor(
			options?.Id ?? DeriveIdPrefix(componentType) + "0001",
			options?.Title ?? $"{kind} failed",
			options?.MessageFormat
				?? $"{kind} '{{0}}' failed with '{{1}}': {{2}}. This is a defect in the {kind.ToLowerInvariant()}, not in the code being compiled.",
			options?.Category ?? DeriveCategory(componentType),
			options?.Severity ?? DiagnosticSeverity.Warning,
			isEnabledByDefault: true,
			description: options?.Description
				?? $"The {kind.ToLowerInvariant()} threw an unhandled exception. It has been reported as a diagnostic so the rest of the compilation still succeeds, rather than being surfaced as a compiler-level failure.",
			helpLinkUri: options?.HelpLinkUri
		);
	}

	/// <summary>
	/// Names the kind of Roslyn component, for the diagnostic's title and message.
	/// </summary>
	static string DescribeKind(Type componentType)
	{
		foreach (var (typeName, label) in KnownComponentBaseTypes)
		{
			if (InheritsOrImplements(componentType, typeName))
				return label;
		}

		return "Roslyn component";
	}

	static readonly (string TypeName, string Label)[] KnownComponentBaseTypes =
	[
		("Microsoft.CodeAnalysis.IIncrementalGenerator", "Source generator"),
		("Microsoft.CodeAnalysis.ISourceGenerator", "Source generator"),
		("Microsoft.CodeAnalysis.Diagnostics.DiagnosticSuppressor", "Diagnostic suppressor"),
		("Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer", "Analyzer"),
		("Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider", "Code fix"),
		("Microsoft.CodeAnalysis.CodeRefactorings.CodeRefactoringProvider", "Code refactoring"),
	];

	static bool InheritsOrImplements(Type componentType, string typeName)
	{
		for (var current = componentType; current is not null; current = current.BaseType)
		{
			if (string.Equals(current.FullName, typeName, StringComparison.Ordinal))
				return true;
		}

		foreach (var implemented in componentType.GetInterfaces())
		{
			if (string.Equals(implemented.FullName, typeName, StringComparison.Ordinal))
				return true;
		}

		return false;
	}

	/// <summary>
	/// Derives a diagnostic id prefix from the component's namespace: the initial of each segment, upper
	/// cased, capped at four characters.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <c>Acme.Widgets.Generators</c> becomes <c>AWG</c>. A single-segment namespace uses its first three
	/// letters instead, because a one-letter prefix reads like a compiler id. A type with no namespace at
	/// all falls back to <c>GEN</c>.
	/// </para>
	/// <para>
	/// The result is stable for a given namespace, which is what matters: a consumer suppressing the id in
	/// an <c>.editorconfig</c> must not find it renamed by an unrelated change. It is not guaranteed unique
	/// across two assemblies that share a namespace shape — set <see cref="ComponentFailureOptions.Id"/>
	/// when that matters.
	/// </para>
	/// </remarks>
	public static string DeriveIdPrefix(Type componentType)
	{
		if (componentType is null)
			throw new ArgumentNullException(nameof(componentType));

		var @namespace = componentType.Namespace;
		if (string.IsNullOrWhiteSpace(@namespace))
			return FallbackIdPrefix;

		var segments = @namespace!.Split(['.'], StringSplitOptions.RemoveEmptyEntries);
		if (segments.Length == 0)
			return FallbackIdPrefix;

		if (segments.Length == 1)
		{
			var single = Letters(segments[0]);

			return single.Length >= 2 ? Upper(single.Substring(0, Math.Min(3, single.Length))) : FallbackIdPrefix;
		}

		StringBuilder prefix = new(4);
		foreach (var segment in segments)
		{
			var letters = Letters(segment);
			if (letters.Length == 0)
				continue;

			prefix.Append(char.ToUpperInvariant(letters[0]));

			if (prefix.Length == 4)
				break;
		}

		return prefix.Length >= 2 ? prefix.ToString() : FallbackIdPrefix;
	}

	/// <summary>
	/// Uses the component's root namespace as the diagnostic category, so failures group under the
	/// consumer's own product.
	/// </summary>
	static string DeriveCategory(Type componentType)
	{
		var @namespace = componentType.Namespace;
		if (string.IsNullOrWhiteSpace(@namespace))
			return FallbackCategory;

		var separator = @namespace!.IndexOf('.');

		return separator > 0 ? @namespace.Substring(0, separator) : @namespace;
	}

	static string Letters(string value)
	{
		StringBuilder letters = new(value.Length);
		foreach (var character in value)
		{
			if (char.IsLetter(character))
				letters.Append(character);
		}

		return letters.ToString();
	}

	static string Upper(string value) => value.ToUpper(CultureInfo.InvariantCulture);
}
