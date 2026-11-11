using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Purview.SourceGeneratorFramework.Testing;

/// <summary>
/// Compiles attribute applications into <see cref="AttributeData"/>, so an attribute-data model's
/// <c>FromAttributeData</c> can be tested against exactly what Roslyn produces, without running a generator.
/// </summary>
/// <remarks>
/// <para>
/// References and usings come from the <see cref="SourceGeneratorTestOptions"/>: add the assembly declaring the
/// attribute with <see cref="SourceGeneratorTestOptions.AdditionalAssemblyTypes"/> and its namespace with
/// <see cref="SourceGeneratorTestOptions.AdditionalNamespaces"/>, or declare the attribute in
/// <see cref="SourceGeneratorTestOptions.AdditionalSources"/>, which are compiled alongside.
/// </para>
/// <para>
/// A compilation error throws <see cref="InvalidOperationException"/> listing the errors, so a misspelt
/// parameter or property name fails the test instead of producing an attribute with missing arguments.
/// </para>
/// </remarks>
public static class AttributeDataFactory
{
	const string TargetName = "AttributeDataTarget";
	const string HostName = "AttributeDataHost";

	// Tried in order when the attribute does not allow a class; each places the application on one kind of
	// declaration and reads it back from the compilation.
	static readonly (
		AttributeTargets Target,
		Func<string, string> Source,
		Func<Compilation, ISymbol?> Symbol
	)[] Placements =
	[
		(AttributeTargets.Class, a => $"[{a}] public class {TargetName} {{ }}", c => Type(c, TargetName)),
		(AttributeTargets.Method, a => Host($"[{a}] public void {TargetName}() {{ }}"), c => Member(c, TargetName)),
		(
			AttributeTargets.Property,
			a => Host($"[{a}] public int {TargetName} {{ get; set; }}"),
			c => Member(c, TargetName)
		),
		(AttributeTargets.Field, a => Host($"[{a}] public int {TargetName};"), c => Member(c, TargetName)),
		(
			AttributeTargets.Parameter,
			a => Host($"public void M([{a}] int {TargetName}) {{ }}"),
			c => (Member(c, "M") as IMethodSymbol)?.Parameters.FirstOrDefault()
		),
		(AttributeTargets.Struct, a => $"[{a}] public struct {TargetName} {{ }}", c => Type(c, TargetName)),
		(AttributeTargets.Interface, a => $"[{a}] public interface {TargetName} {{ }}", c => Type(c, TargetName)),
		(AttributeTargets.Enum, a => $"[{a}] public enum {TargetName} {{ }}", c => Type(c, TargetName)),
		(AttributeTargets.Delegate, a => $"[{a}] public delegate void {TargetName}();", c => Type(c, TargetName)),
		(
			AttributeTargets.Constructor,
			a => Host($"[{a}] public {HostName}() {{ }}"),
			c => Type(c, HostName)?.InstanceConstructors.FirstOrDefault()
		),
		(
			AttributeTargets.Event,
			a => Host($"[{a}] public event global::System.Action? {TargetName};"),
			c => Member(c, TargetName)
		),
		(
			AttributeTargets.GenericParameter,
			a => $"public class {TargetName}<[{a}] T> {{ }}",
			c => c.GetTypeByMetadataName(TargetName + "`1")?.TypeParameters.FirstOrDefault()
		),
		(AttributeTargets.Assembly, a => $"[assembly: {a}]", c => c.Assembly),
		(AttributeTargets.Module, a => $"[module: {a}]", c => c.SourceModule),
	];

	/// <summary>
	/// Compiles a single attribute application and returns it.
	/// </summary>
	/// <param name="attributeUsage">The application, with or without brackets: <c>[Log("text", EventId = 7)]</c>.</param>
	/// <param name="options">Supplies references, usings and language settings; <see cref="SourceGeneratorTestOptions.Default"/> when omitted.</param>
	/// <returns>The attribute application.</returns>
	/// <remarks>
	/// The application is placed on a class when the attribute allows it, and otherwise on the first declaration
	/// kind its <see cref="AttributeUsageAttribute"/> allows, so method-only and parameter-only attributes work too.
	/// </remarks>
	public static AttributeData FromUsage(string attributeUsage, SourceGeneratorTestOptions? options = null)
	{
		if (string.IsNullOrWhiteSpace(attributeUsage))
			throw new ArgumentException("The attribute usage cannot be null or whitespace.", nameof(attributeUsage));

		options ??= SourceGeneratorTestOptions.Default;

		var usage = attributeUsage.Trim();
		if (usage.Length >= 2 && usage[0] == '[' && usage[usage.Length - 1] == ']')
			usage = usage.Substring(1, usage.Length - 2).Trim();

		var placement = Placements[0];
		var compilation = Compile(placement.Source(usage), options);
		var attributes = placement.Symbol(compilation)?.GetAttributes() ?? [];

		// The class placement binds the attribute type even when the attribute does not allow classes; its
		// AttributeUsage then says where it can go.
		var validOn = ValidOn(attributes.FirstOrDefault()?.AttributeClass);
		if (!validOn.HasFlag(AttributeTargets.Class))
		{
			placement = Placements.FirstOrDefault(p => validOn.HasFlag(p.Target));
			if (placement.Source is null)
				throw new InvalidOperationException($"No supported declaration accepts [{usage}] ({validOn}).");

			compilation = Compile(placement.Source(usage), options);
			attributes = placement.Symbol(compilation)?.GetAttributes() ?? [];
		}

		ThrowOnErrors(compilation, usage);

		return attributes.Length == 1
			? attributes[0]
			: throw new InvalidOperationException(
				$"Expected exactly one attribute application in [{usage}] but found {attributes.Length}."
			);
	}

	/// <summary>
	/// Compiles <paramref name="source"/> and returns the attribute applications on a type or one of its members.
	/// </summary>
	/// <param name="source">The source declaring the attributed symbol.</param>
	/// <param name="typeMetadataName">The metadata name of the type, for example <c>Sample.IEvents</c> or <c>Sample.Outer+Inner</c>.</param>
	/// <param name="memberName">A member of the type to read instead of the type itself. It must name exactly one member.</param>
	/// <param name="options">Supplies references, usings and language settings; <see cref="SourceGeneratorTestOptions.Default"/> when omitted.</param>
	/// <returns>The attribute applications, in declaration order.</returns>
	public static ImmutableArray<AttributeData> FromSource(
		string source,
		string typeMetadataName,
		string? memberName = null,
		SourceGeneratorTestOptions? options = null
	)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));
		if (string.IsNullOrWhiteSpace(typeMetadataName))
			throw new ArgumentException(
				"The type metadata name cannot be null or whitespace.",
				nameof(typeMetadataName)
			);

		var compilation = Compile(source, options ?? SourceGeneratorTestOptions.Default);
		ThrowOnErrors(compilation, null);

		var type = compilation.GetTypeByMetadataName(typeMetadataName)
			?? throw new InvalidOperationException($"Type '{typeMetadataName}' was not found in the compilation.");

		if (memberName is null)
			return type.GetAttributes();

		var members = type.GetMembers(memberName);
		return members.Length == 1
			? members[0].GetAttributes()
			: throw new InvalidOperationException(
				$"Expected exactly one member named '{memberName}' on '{typeMetadataName}' but found {members.Length}."
			);
	}

	static CSharpCompilation Compile(string source, SourceGeneratorTestOptions options)
	{
		CSharpParseOptions parseOptions = new(options.LanguageVersion);
		SyntaxTree[] trees =
		[
			CSharpSyntaxTree.ParseText(SourceGeneratorHelpers.PrepareSource(source, options), parseOptions),
			.. options.AdditionalSources.Select(s => CSharpSyntaxTree.ParseText(s, parseOptions)),
		];

		return SourceGeneratorHelpers.CreateCompilation(
			trees,
			SourceGeneratorHelpers.ResolveReferences(options, typeof(AttributeDataFactory).Assembly),
			options
		);
	}

	static void ThrowOnErrors(Compilation compilation, string? usage)
	{
		var errors = compilation.GetDiagnostics().Where(static d => d.Severity == DiagnosticSeverity.Error).ToList();
		if (errors.Count == 0)
			return;

		var subject = usage is null ? "the source" : $"[{usage}]";
		throw new InvalidOperationException(
			$"Compiling {subject} failed:\n"
				+ string.Join("\n", errors.Select(static e => e.ToString()))
				+ "\n\nSource:\n"
				+ compilation.SyntaxTrees.First().GetText()
		);
	}

	static AttributeTargets ValidOn(INamedTypeSymbol? attributeClass)
	{
		for (var type = attributeClass; type is not null; type = type.BaseType)
		{
			foreach (var attribute in type.GetAttributes())
			{
				if (
					attribute.AttributeClass is { Name: nameof(AttributeUsageAttribute) } usageClass
					&& usageClass.ContainingNamespace.ToDisplayString() == "System"
					&& attribute.ConstructorArguments.FirstOrDefault().Value is int validOn
				)
				{
					return (AttributeTargets)validOn;
				}
			}
		}

		return AttributeTargets.All;
	}

	static string Host(string member) => $"public class {HostName} {{ {member} }}";

	static INamedTypeSymbol? Type(Compilation compilation, string name) => compilation.GetTypeByMetadataName(name);

	static ISymbol? Member(Compilation compilation, string name) =>
		Type(compilation, HostName)?.GetMembers(name).FirstOrDefault();
}
