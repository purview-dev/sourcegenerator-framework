namespace Purview.SourceGeneratorFramework;

public sealed class EnumValueDefinitionTests
{
	static readonly TypeIdentity Severity = new("Severity", "Test");

	// ---------------------------------------------------------------------------------------------
	// Construction
	// ---------------------------------------------------------------------------------------------

	[Test]
	public async Task Construction_SetsProperties()
	{
		EnumValueDefinition value = new(Severity, "Warning", 3);

		await Assert.That(value.EnumType).IsEqualTo(Severity);
		await Assert.That(value.Name).IsEqualTo("Warning");
		await Assert.That(value.Value).IsEqualTo(3m);
		await Assert.That(value.UnderlyingType).IsEqualTo(EnumUnderlyingType.Int32);
		await Assert.That(value.Aliases.IsDefaultOrEmpty).IsTrue();
	}

	[Test]
	public async Task Construction_WithUnderlyingType_SetsUnderlyingType()
	{
		EnumValueDefinition value = new(Severity, "Warning", 3, underlyingType: EnumUnderlyingType.Byte);

		await Assert.That(value.UnderlyingType).IsEqualTo(EnumUnderlyingType.Byte);
		await Assert.That(value.Value).IsEqualTo(3m);
	}

	[Test]
	public async Task Construction_RepresentsUInt64MaximumExactly()
	{
		const ulong maximum = ulong.MaxValue;
		EnumValueDefinition value = new(Severity, "Maximum", maximum, underlyingType: EnumUnderlyingType.UInt64);

		await Assert.That(value.UnderlyingType).IsEqualTo(EnumUnderlyingType.UInt64);
		await Assert.That(value.Value).IsEqualTo(18446744073709551615m);
	}

	[Test]
	public async Task Construction_RepresentsInt64MinimumExactly()
	{
		const long minimum = long.MinValue;
		EnumValueDefinition value = new(Severity, "Minimum", minimum, underlyingType: EnumUnderlyingType.Int64);

		await Assert.That(value.UnderlyingType).IsEqualTo(EnumUnderlyingType.Int64);
		await Assert.That(value.Value).IsEqualTo(-9223372036854775808m);
	}

	[Test]
	public async Task Construction_WithAliases_NormalizesDefault()
	{
		EnumValueDefinition value = new(Severity, "Tag", 0, ["Tag", "Tags"]);

		await Assert.That(value.Aliases.Length).IsEqualTo(2);
		await Assert.That(value.Aliases[0]).IsEqualTo("Tag");
		await Assert.That(value.Aliases[1]).IsEqualTo("Tags");
	}

	[Test]
	public async Task Construction_EmptyEnumType_Throws()
	{
		await Assert.That(() => new EnumValueDefinition(TypeIdentity.Empty, "Warning", 3)).Throws<ArgumentException>();
	}

	[Test]
	public async Task Construction_EmptyName_Throws()
	{
		await Assert.That(() => new EnumValueDefinition(Severity, " ", 3)).Throws<ArgumentException>();
	}

	// ---------------------------------------------------------------------------------------------
	// FullName
	// ---------------------------------------------------------------------------------------------

	[Test]
	public async Task FullName_CombinesEnumAndMember()
	{
		EnumValueDefinition value = new(Severity, "Warning", 3);

		await Assert.That(value.FullName).IsEqualTo("Test.Severity.Warning");
	}

	// ---------------------------------------------------------------------------------------------
	// Matching
	// ---------------------------------------------------------------------------------------------

	[Test]
	public async Task Matches_MemberName()
	{
		EnumValueDefinition value = new(Severity, "Warning", 3);

		await Assert.That(value.Matches("Warning")).IsTrue();
	}

	[Test]
	public async Task Matches_FullName()
	{
		EnumValueDefinition value = new(Severity, "Warning", 3);

		await Assert.That(value.Matches("Test.Severity.Warning")).IsTrue();
	}

	[Test]
	public async Task Matches_SuffixMemberName()
	{
		EnumValueDefinition value = new(Severity, "Warning", 3);

		await Assert.That(value.Matches("Other.Severity.Warning")).IsTrue();
	}

	[Test]
	public async Task Matches_Alias()
	{
		EnumValueDefinition value = new(Severity, "Tag", 0, ["Tag", "Tags"]);

		await Assert.That(value.Matches("Tags")).IsTrue();
	}

	[Test]
	public async Task Matches_NonMatch_ReturnsFalse()
	{
		EnumValueDefinition value = new(Severity, "Warning", 3);

		await Assert.That(value.Matches("Error")).IsFalse();
		await Assert.That(value.Matches("Warning!")).IsFalse();
		await Assert.That(value.Matches(" ")).IsFalse();
		await Assert.That(value.Matches("")).IsFalse();
	}

	// ---------------------------------------------------------------------------------------------
	// Empty
	// ---------------------------------------------------------------------------------------------

	[Test]
	public async Task Empty_NeverMatches()
	{
		await Assert.That(EnumValueDefinition.Empty.Matches("Warning")).IsFalse();
		await Assert.That(EnumValueDefinition.Empty.Matches("")).IsFalse();
	}
}
