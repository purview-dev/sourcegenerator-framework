using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Purview.SourceGeneratorFramework.Generators.Helpers;

/// <summary>
/// Rewrites references to framework-owned types in documentation that is copied into generated code.
/// <para>
/// A <c>cref</c> has to resolve to a symbol in every compilation that contains the documentation, and
/// copied documentation lands in generated files whose namespace and using set differ from the
/// author's source (a generated type library, for example, imports only
/// <c>global::Purview.SourceGeneratorFramework</c>). An unresolvable cref produces CS1574 in the
/// generated file, and a cref that resolves through another assembly identity produces exactly the
/// ambiguity the <c>PSGFR40</c> analyzer warns about.
/// </para>
/// <para>
/// Rendering the type as inline code (<c>&lt;c&gt;</c>) removes that dependency entirely: the text is
/// documentation, never a symbol reference. Matching is name-based because documentation XML carries no
/// resolved symbols, so a cref whose qualifier is neither absent nor the framework namespace keeps its
/// original form.
/// </para>
/// </summary>
static class FrameworkCrefRewriter
{
	const string FrameworkNamespace = "Purview.SourceGeneratorFramework";
	const string GlobalAlias = "global::";

	/// <summary>
	/// Rewrites every framework-type <c>cref</c> found under <paramref name="element"/> into an inline
	/// code element, in place.
	/// </summary>
	/// <param name="element">A documentation element (for example the captured <c>summary</c>).</param>
	/// <param name="frameworkTypeNames">Public type names declared by the framework assembly.</param>
	public static void Rewrite(XElement element, EquatableArray<string> frameworkTypeNames)
	{
		if (frameworkTypeNames.IsEmpty)
			return;

		var crefs = element.DescendantsAndSelf().Where(IsCrefElement).ToList();
		foreach (var cref in crefs)
		{
			var crefText = (string?)cref.Attribute("cref");
			if (crefText is null || !TryGetInlineText(crefText, frameworkTypeNames, out var inlineText))
				continue;

			// Preserve author-supplied content (`<see cref="X">shown</see>`); otherwise show the type.
			cref.ReplaceWith(new XElement("c", cref.Nodes().Any() ? cref.Nodes().ToArray() : [new XText(inlineText)]));
		}
	}

	/// <summary>
	/// Rewrites framework-type crefs in a captured documentation fragment, returning the original text
	/// when it is empty, contains no framework cref, or is not well-formed XML.
	/// </summary>
	public static string? RewriteDocumentation(string? documentation, EquatableArray<string> frameworkTypeNames)
	{
		if (string.IsNullOrWhiteSpace(documentation) || frameworkTypeNames.IsEmpty)
			return documentation;

		try
		{
			var wrapper = XElement.Parse(
				"<purview-documentation>" + documentation + "</purview-documentation>",
				LoadOptions.PreserveWhitespace
			);
			Rewrite(wrapper, frameworkTypeNames);

			var result = string.Join(
					"\n",
					wrapper.Elements().Select(static element => element.ToString(SaveOptions.DisableFormatting))
				)
				.Trim();

			return result.Length == 0 ? documentation : result;
		}
		catch (XmlException)
		{
			return documentation;
		}
	}

	static bool IsCrefElement(XElement element) =>
		(element.Name.LocalName == "see" || element.Name.LocalName == "seealso")
		&& element.Attribute("cref") is not null;

	/// <summary>
	/// Returns the text to render as inline code when <paramref name="crefText"/> names a framework type.
	/// </summary>
	static bool TryGetInlineText(string crefText, EquatableArray<string> frameworkTypeNames, out string inlineText)
	{
		inlineText = string.Empty;

		var text = crefText.Trim();
		if (text.StartsWith(GlobalAlias, StringComparison.Ordinal))
			text = text.Substring(GlobalAlias.Length);

		// Roslyn expands crefs when documentation is captured (`T:Namespace.Type`,
		// `M:Namespace.Type.Member(...)`), so the declaration-id prefix is stripped before matching.
		text = StripIdPrefix(text);

		var segments = SplitSegments(text);
		if (segments.Length == 0)
			return false;

		for (var index = 0; index < segments.Length; index++)
		{
			var segment = StripCallArguments(segments[index]);
			if (segment.Length == 0)
				continue;

			var identifierLength = segment.IndexOf('{');
			var identifier = identifierLength < 0 ? segment : segment.Substring(0, identifierLength);
			if (
				identifier.Length == 0
				|| !frameworkTypeNames.AsImmutableArray().Contains(identifier, StringComparer.Ordinal)
			)
				continue;

			// Accept a bare type name, a member cref whose receiver is a framework type
			// (`CodeWriter.Write`), or a cref written through the framework namespace. Any other
			// qualifier means the cref targets something else that merely shares the name.
			if (index > 0 && !HasFrameworkQualifier(segments, index))
				return false;

			inlineText = BuildInlineText(segments, index);
			return true;
		}

		return false;
	}

	/// <summary>
	/// Renders the matched type plus any member path as written, dropping call arguments so a cref such
	/// as <c>CodeWriter.Write(System.String)</c> reads as <c>CodeWriter.Write</c>.
	/// </summary>
	static string BuildInlineText(string[] segments, int matchedIndex)
	{
		StringBuilder builder = new(segments[matchedIndex]);

		for (var index = matchedIndex + 1; index < segments.Length; index++)
		{
			var raw = segments[index];
			var argumentIndex = raw.IndexOf('(');
			var text = argumentIndex < 0 ? raw : raw.Substring(0, argumentIndex);
			if (text.Length == 0)
				break;

			builder.Append('.').Append(text);
			if (argumentIndex >= 0)
				break;
		}

		return builder.ToString();
	}

	/// <summary>
	/// Strips the documentation-comment declaration id prefix (<c>T:</c>, <c>M:</c>, <c>P:</c>, ...) that
	/// Roslyn adds when it expands a cref.
	/// </summary>
	static string StripIdPrefix(string text)
	{
		if (text.Length < 2 || text[1] != ':')
			return text;

		return text[0] switch
		{
			'N' or 'T' or 'F' or 'P' or 'M' or 'E' or 'O' or 'C' or '!' => text.Substring(2).TrimStart(),
			_ => text,
		};
	}

	static string StripCallArguments(string segment)
	{
		var parenthesis = segment.IndexOf('(');
		return parenthesis < 0 ? segment : segment.Substring(0, parenthesis);
	}

	/// <summary>
	/// Splits a cref on method/member separators only, keeping generic argument lists intact so a name
	/// such as <c>ImmutableArray{System.String}</c> is not mistaken for a framework type.
	/// </summary>
	static string[] SplitSegments(string text)
	{
		List<string> segments = [];
		var start = 0;
		var depth = 0;

		for (var index = 0; index < text.Length; index++)
		{
			switch (text[index])
			{
				case '{' or '(' or '[':
					depth++;
					break;
				case '}' or ')' or ']':
					if (depth > 0)
						depth--;
					break;
				case '.' when depth == 0:
					segments.Add(text.Substring(start, index - start));
					start = index + 1;
					break;
				default:
					break;
			}
		}

		segments.Add(text.Substring(start));
		return [.. segments];
	}

	static bool HasFrameworkQualifier(string[] segments, int matchedIndex)
	{
		var prefix = string.Join(".", segments, 0, matchedIndex);
		return prefix.Equals(FrameworkNamespace, StringComparison.Ordinal)
			|| prefix.StartsWith(FrameworkNamespace + ".", StringComparison.Ordinal);
	}
}
