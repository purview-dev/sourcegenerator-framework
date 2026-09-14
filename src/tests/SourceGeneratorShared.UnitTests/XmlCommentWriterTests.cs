namespace Purview.SourceGeneratorFramework;

public class XmlCommentWriterTests
{
	[Test]
	public async Task XmlComment_WritesEachCommentLine()
	{
		var writer = CodeWriterFactory.ForTests();

		var result = writer.XmlComment("first", "second");

		await Assert.That(result).IsSameReferenceAs(writer);
		await Assert.That(writer.ToString()).IsEqualTo("/// first\n/// second\n");
	}

	[Test]
	public async Task Xml_SingleLine_WritesOpeningContentAndClosingTags()
	{
		var writer = CodeWriterFactory.ForTests();

		writer.Xml("remarks", "content");

		await Assert.That(writer.ToString()).IsEqualTo("/// <remarks>content</remarks>\n");
	}

	[Test]
	public async Task Xml_MultipleLines_PutsContentOnSeparateLines()
	{
		var writer = CodeWriterFactory.ForTests();

		writer.Xml("remarks", "first", "second");

		await Assert.That(writer.ToString()).IsEqualTo("/// <remarks>\n/// first\n/// second\n/// </remarks>\n");
	}

	[Test]
	[Arguments(null)]
	[Arguments("")]
	[Arguments("   ")]
	public async Task Xml_MissingTag_Throws(string? tag)
	{
		var writer = CodeWriterFactory.ForTests();

		await Assert.That(() => writer.Xml(tag!, "content")).Throws<ArgumentException>();
	}

	[Test]
	public async Task XmlSummary_WritesSummaryBlock()
	{
		var writer = CodeWriterFactory.ForTests();

		writer.XmlSummary("first", "second");

		await Assert.That(writer.ToString()).IsEqualTo("/// <summary>\n/// first\n/// second\n/// </summary>\n");
	}

	[Test]
	public async Task XmlSummary_SingleLine_IsWrittenOverMultipleLines()
	{
		var writer = CodeWriterFactory.ForTests();

		writer.XmlSummary("single line");

		await Assert.That(writer.ToString()).IsEqualTo("/// <summary>\n/// single line\n/// </summary>\n");
	}

	[Test]
	public async Task XmlReturn_WritesReturnsBlock()
	{
		var writer = CodeWriterFactory.ForTests();

		writer.XmlReturn("value");

		await Assert.That(writer.ToString()).IsEqualTo("/// <returns>value</returns>\n");
	}

	[Test]
	public async Task XmlCref_String_WritesCrefAttribute()
	{
		var writer = CodeWriterFactory.ForTests();

		writer.XmlCref("MyType", "type description");

		await Assert.That(writer.ToString()).IsEqualTo("/// <cref cref=\"MyType\">type description</cref>\n");
	}

	[Test]
	public async Task XmlCref_TypeIdentity_WritesRenderedCrefAndMultipleContentLines()
	{
		var writer = CodeWriterFactory.ForTests();

		writer.XmlCref(new TypeIdentity(typeof(string)), "first", "second");

		await Assert
			.That(writer.ToString())
			.IsEqualTo("/// <cref cref=\"string\">\n/// first\n/// second\n/// </cref>\n");
	}

	// ---------------------------------------------------------------------------------------------
	// cref-safe rendering
	// ---------------------------------------------------------------------------------------------

	[Test]
	public async Task ToXmlCref_KeywordType_RendersKeyword()
	{
		await Assert.That(XmlCommentWriter.ToXmlCref(new TypeIdentity(typeof(string)))).IsEqualTo("string");
		await Assert.That(XmlCommentWriter.ToXmlCref(new TypeIdentity(typeof(int)))).IsEqualTo("int");
	}

	[Test]
	public async Task ToXmlCref_NamedType_RendersGlobalPrefixedName()
	{
		await Assert
			.That(XmlCommentWriter.ToXmlCref(new TypeIdentity(typeof(ArgumentException))))
			.IsEqualTo("global::System.ArgumentException");
	}

	[Test]
	public async Task ToXmlCref_ConstructedGeneric_UsesBraceForm()
	{
		var dictionary = new TypeIdentity(typeof(Dictionary<,>)).MakeGeneric(
			TypeIdentity.Create<string>(),
			TypeIdentity.Create<int>()
		);

		await Assert
			.That(XmlCommentWriter.ToXmlCref(dictionary))
			.IsEqualTo("global::System.Collections.Generic.Dictionary{string,int}");
	}

	[Test]
	public async Task ToXmlCref_OpenGeneric_UsesReadablePlaceholders()
	{
		await Assert
			.That(XmlCommentWriter.ToXmlCref(new TypeIdentity(typeof(List<>))))
			.IsEqualTo("global::System.Collections.Generic.List{T0}");
		await Assert
			.That(XmlCommentWriter.ToXmlCref(new TypeIdentity(typeof(Dictionary<,>))))
			.IsEqualTo("global::System.Collections.Generic.Dictionary{T0,T1}");
	}

	[Test]
	public async Task ToXmlCref_NestedType_RendersDottedChain()
	{
		var nested = new TypeIdentity(typeof(Dictionary<,>)).Nested("Entry");

		await Assert
			.That(XmlCommentWriter.ToXmlCref(nested))
			.IsEqualTo("global::System.Collections.Generic.Dictionary{T0,T1}.Entry");
	}

	[Test]
	public async Task ToXmlCref_TypeReference_ValueTypeNullable_UsesNullableWrapper()
	{
		await Assert
			.That(XmlCommentWriter.ToXmlCref(TypeIdentity.Create<int>().MakeNullable()))
			.IsEqualTo("global::System.Nullable{int}");
	}

	[Test]
	public async Task ToXmlCref_TypeReference_ReferenceAnnotation_IsOmitted()
	{
		await Assert.That(XmlCommentWriter.ToXmlCref(TypeIdentity.Create<string>().MakeNullable())).IsEqualTo("string");
	}

	[Test]
	public async Task ToXmlCref_TypeReference_ComposesModifiers()
	{
		await Assert.That(XmlCommentWriter.ToXmlCref(TypeIdentity.Create<int>().MakeArray())).IsEqualTo("int[]");
		await Assert.That(XmlCommentWriter.ToXmlCref(TypeIdentity.Create<int>().MakeArray(2))).IsEqualTo("int[,]");
		await Assert.That(XmlCommentWriter.ToXmlCref(TypeIdentity.Create<int>().MakePointer())).IsEqualTo("int*");
		await Assert
			.That(XmlCommentWriter.ToXmlCref(TypeIdentity.Create<int>().MakeNullable().MakeArray()))
			.IsEqualTo("global::System.Nullable{int}[]");
		await Assert
			.That(XmlCommentWriter.ToXmlCref(TypeIdentity.Create<string>().MakeNullable().MakeArray().Nullable()))
			.IsEqualTo("string[]");
	}

	[Test]
	public async Task ToXmlCref_TypeReference_TypeParameter_UsesBraces()
	{
		await Assert.That(XmlCommentWriter.ToXmlCref(TypeReference.ForTypeParameter("T"))).IsEqualTo("{T}");
	}

	[Test]
	public async Task ToXmlCref_TypeReference_GenericArgumentNullability_IsOmitted()
	{
		var list = new TypeIdentity(typeof(List<>)).MakeGeneric(TypeIdentity.Create<string>().MakeNullable());

		await Assert
			.That(XmlCommentWriter.ToXmlCref(list.AsTypeReference()))
			.IsEqualTo("global::System.Collections.Generic.List{string}");
	}

	[Test]
	public async Task XmlCref_TypeReference_WritesCrefAttribute()
	{
		var writer = CodeWriterFactory.ForTests();

		writer.XmlCref(TypeIdentity.Create<int>().MakeNullable(), "value description");

		await Assert
			.That(writer.ToString())
			.IsEqualTo("/// <cref cref=\"global::System.Nullable{int}\">value description</cref>\n");
	}

	[Test]
	public async Task XmlException_TypeReference_WritesCrefAttribute()
	{
		var writer = CodeWriterFactory.ForTests();

		writer.XmlException(TypeIdentity.Create<InvalidOperationException>().AsTypeReference(), "reason");

		await Assert
			.That(writer.ToString())
			.IsEqualTo("/// <exception cref=\"global::System.InvalidOperationException\">reason</exception>\n");
	}

	[Test]
	public async Task XmlSee_TypeReference_ReturnsSelfClosingSee()
	{
		var see = XmlCommentWriter.XmlSee(TypeIdentity.Create<InvalidOperationException>().AsTypeReference());

		await Assert.That(see).IsEqualTo("<see cref=\"global::System.InvalidOperationException\" />");
	}

	[Test]
	public async Task XmlSeeAlso_TypeReference_WritesSelfClosingSeeAlso()
	{
		var writer = CodeWriterFactory.ForTests();

		writer.XmlSeeAlso(TypeIdentity.Create<string>().MakeNullable());

		await Assert.That(writer.ToString()).IsEqualTo("/// <seealso cref=\"string\" />\n");
	}

	[Test]
	public async Task XmlCref_TypeReference_Null_Throws()
	{
		var writer = CodeWriterFactory.ForTests();
		TypeReference reference = null!;

		await Assert.That(() => writer.XmlCref(reference)).Throws<ArgumentNullException>();
	}

	[Test]
	[Arguments(null)]
	[Arguments("")]
	[Arguments("   ")]
	public async Task XmlCref_String_MissingTypeName_Throws(string? typeName)
	{
		var writer = CodeWriterFactory.ForTests();

		await Assert.That(() => writer.XmlCref(typeName!, "content")).Throws<ArgumentException>();
	}

	[Test]
	public async Task XmlException_String_WritesCrefAttribute()
	{
		var writer = CodeWriterFactory.ForTests();

		writer.XmlException("InvalidOperationException", "reason");

		await Assert
			.That(writer.ToString())
			.IsEqualTo("/// <exception cref=\"InvalidOperationException\">reason</exception>\n");
	}

	[Test]
	public async Task XmlException_TypeIdentity_WritesRenderedCref()
	{
		var writer = CodeWriterFactory.ForTests();

		writer.XmlException(new TypeIdentity(typeof(ArgumentException)), "reason");

		await Assert
			.That(writer.ToString())
			.IsEqualTo("/// <exception cref=\"global::System.ArgumentException\">reason</exception>\n");
	}

	[Test]
	[Arguments(null)]
	[Arguments("")]
	[Arguments("   ")]
	public async Task XmlException_String_MissingType_Throws(string? exceptionType)
	{
		var writer = CodeWriterFactory.ForTests();

		await Assert.That(() => writer.XmlException(exceptionType!, "content")).Throws<ArgumentException>();
	}

	[Test]
	[Arguments("c", "/// <c>code</c>\n")]
	[Arguments("example", "/// <example>sample</example>\n")]
	[Arguments("para", "/// <para>paragraph</para>\n")]
	public async Task SimpleXmlHelpers_WriteExpectedTag(string tag, string expected)
	{
		var writer = CodeWriterFactory.ForTests();

		switch (tag)
		{
			case "c":
				writer.XmlCode("code");
				break;
			case "example":
				writer.XmlExample("sample");
				break;
			default:
				writer.XmlPara("paragraph");
				break;
		}

		await Assert.That(writer.ToString()).IsEqualTo(expected);
	}

	[Test]
	public async Task XmlParam_WritesNameAttribute()
	{
		var writer = CodeWriterFactory.ForTests();

		writer.XmlParam("value", "description");

		await Assert.That(writer.ToString()).IsEqualTo("/// <param name=\"value\">description</param>\n");
	}

	[Test]
	public async Task XmlParam_MultipleLines_WritesExpandedElement()
	{
		var writer = CodeWriterFactory.ForTests();

		writer.XmlParam("world", "this is a multi line", "description");

		await Assert
			.That(writer.ToString())
			.IsEqualTo(
				"/// <param name=\"world\">\n" + "/// this is a multi line\n" + "/// description\n" + "/// </param>\n"
			);
	}

	[Test]
	public async Task XmlTypeParam_UsesCompactAndExpandedForms()
	{
		var writer = CodeWriterFactory.ForTests();

		writer.XmlTypeParam("T", "the item type");
		writer.XmlTypeParam("TResult", "the first line", "the second line");

		await Assert
			.That(writer.ToString())
			.IsEqualTo(
				"/// <typeparam name=\"T\">the item type</typeparam>\n"
					+ "/// <typeparam name=\"TResult\">\n"
					+ "/// the first line\n"
					+ "/// the second line\n"
					+ "/// </typeparam>\n"
			);
	}

	[Test]
	[Arguments(null)]
	[Arguments("")]
	[Arguments("   ")]
	public async Task XmlParam_MissingName_Throws(string? parameterName)
	{
		var writer = CodeWriterFactory.ForTests();

		await Assert.That(() => writer.XmlParam(parameterName!, "content")).Throws<ArgumentException>();
	}

	[Test]
	public async Task XmlList_DefaultsToBulletItems()
	{
		var writer = CodeWriterFactory.ForTests();

		writer.XmlList(["first", "second"]);

		await Assert
			.That(writer.ToString())
			.IsEqualTo(
				"/// <list type=\"bullet\">\n"
					+ "/// <item>first</item>\n"
					+ "/// <item>second</item>\n"
					+ "/// </list>\n"
			);
	}

	[Test]
	public async Task XmlList_WithDescription_WritesDescriptionBeforeItems()
	{
		var writer = CodeWriterFactory.ForTests();

		writer.XmlList("bullet", "A description", "item");

		await Assert
			.That(writer.ToString())
			.IsEqualTo(
				"/// <list type=\"bullet\">\n"
					+ "/// <description>A description</description>\n"
					+ "/// <item>item</item>\n"
					+ "/// </list>\n"
			);
	}

	[Test]
	public async Task XmlList_CustomType_WritesTypeAttribute()
	{
		var writer = CodeWriterFactory.ForTests();

		writer.XmlList("number", null, "item");

		await Assert
			.That(writer.ToString())
			.IsEqualTo("/// <list type=\"number\">\n/// <item>item</item>\n/// </list>\n");
	}

	[Test]
	[Arguments(null)]
	[Arguments("")]
	[Arguments("   ")]
	public async Task XmlList_MissingType_Throws(string? listType)
	{
		var writer = CodeWriterFactory.ForTests();

		await Assert.That(() => writer.XmlList(listType!, null, "item")).Throws<ArgumentException>();
	}

	[Test]
	public async Task XmlHelpers_WithNoContent_DoNothing()
	{
		var writer = CodeWriterFactory.ForTests();

		writer.XmlSummary([]);
		writer.XmlCodeBlock([]);
		writer.XmlList([]);

		await Assert.That(writer.ToString()).IsEmpty();
	}

	[Test]
	public async Task BuildXmlTag_WithoutAttributes_WritesTag()
	{
		await Assert.That(XmlCommentWriter.BuildXmlTag("summary")).IsEqualTo("<summary>");
	}

	[Test]
	public async Task BuildXmlTag_WithAttributes_WritesValuesAndLowercaseBooleans()
	{
		var result = XmlCommentWriter.BuildXmlTag("list", ("type", "bullet"), ("enabled", true), ("count", 2));

		await Assert.That(result).IsEqualTo("<list type=\"bullet\" enabled=\"true\" count=\"2\">");
	}

	[Test]
	public async Task InlineHelpers_ComposeInsideSummaryAndExample()
	{
		var writer = CodeWriterFactory.ForTests();
		var parameter = XmlCommentWriter.XmlParamRef("value");
		var typeParameter = XmlCommentWriter.XmlTypeParamRef("T");
		var code = XmlCommentWriter.XmlInlineCode("default(T)");
		var see = XmlCommentWriter.XmlSee("MyType");

		writer.XmlSummary($"Pass {parameter} as {typeParameter}; use {code} or {see}.");
		writer.XmlExample(XmlCommentWriter.XmlInlineElement("para", "An inline paragraph."));

		await Assert
			.That(writer.ToString())
			.IsEqualTo(
				"/// <summary>\n"
					+ "/// Pass <paramref name=\"value\" /> as <typeparamref name=\"T\" />; use <c>default(T)</c> or <see cref=\"MyType\" />.\n"
					+ "/// </summary>\n"
					+ "/// <example><para>An inline paragraph.</para></example>\n"
			);
	}

	[Test]
	public async Task XmlList_AcceptsComposableTermAndDescriptionItems()
	{
		var writer = CodeWriterFactory.ForTests();

		writer.XmlList([
			XmlCommentWriter.XmlListItem("first"),
			XmlCommentWriter.XmlListItem("second", "its description"),
		]);

		await Assert
			.That(writer.ToString())
			.IsEqualTo(
				"/// <list type=\"bullet\">\n"
					+ "/// <item>first</item>\n"
					+ "/// <item><term>second</term><description>its description</description></item>\n"
					+ "/// </list>\n"
			);
	}

	[Test]
	public async Task AdditionalBlockHelpers_WriteStandardDocumentationElements()
	{
		var writer = CodeWriterFactory.ForTests();

		writer.XmlRemarks("More information.");
		writer.XmlValue("The current value.");
		writer.XmlCodeBlock("var value = 1;", "return value;");
		writer.XmlPermission("System.Security.PermissionSet", "Required permission.");
		writer.XmlSeeAlso("OtherType");
		writer.XmlInheritDoc("BaseType.Member");
		writer.XmlInclude("docs.xml", "/doc/member[@name='M:Example']/*");

		await Assert
			.That(writer.ToString())
			.IsEqualTo(
				"/// <remarks>More information.</remarks>\n"
					+ "/// <value>The current value.</value>\n"
					+ "/// <code>\n/// var value = 1;\n/// return value;\n/// </code>\n"
					+ "/// <permission cref=\"System.Security.PermissionSet\">Required permission.</permission>\n"
					+ "/// <seealso cref=\"OtherType\" />\n"
					+ "/// <inheritdoc cref=\"BaseType.Member\" />\n"
					+ "/// <include file=\"docs.xml\" path=\"/doc/member[@name=&apos;M:Example&apos;]/*\" />\n"
			);
	}

	[Test]
	public async Task XmlTextAndAttributes_EscapeXmlSpecialCharacters()
	{
		var text = XmlCommentWriter.XmlText("one & two < three > zero ' \"");
		var tag = XmlCommentWriter.BuildSelfClosingXmlTag("see", ("cref", "Dictionary<string, \"value\">"));

		await Assert.That(text).IsEqualTo("one &amp; two &lt; three &gt; zero &apos; &quot;");
		await Assert.That(tag).IsEqualTo("<see cref=\"Dictionary&lt;string, &quot;value&quot;&gt;\" />");
	}

	[Test]
	[Arguments(null)]
	[Arguments("")]
	[Arguments("   ")]
	public async Task BuildXmlTag_MissingTag_Throws(string? tag)
	{
		await Assert.That(() => XmlCommentWriter.BuildXmlTag(tag!)).Throws<ArgumentException>();
	}
}
