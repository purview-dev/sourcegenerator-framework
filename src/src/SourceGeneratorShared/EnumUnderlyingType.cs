namespace Purview.SourceGeneratorFramework;

/// <summary>
/// Identifies the CLR underlying type of an enum whose values are declared in a generated type library.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
	"Naming",
	"CA1720:Identifier contains type name",
	Justification = "Member names match the CLR type names, as with PurviewTypeLibrary."
)]
public enum EnumUnderlyingType
{
	/// <summary>
	/// The <see cref="byte"/> underlying type.
	/// </summary>
	Byte,

	/// <summary>
	/// The <see cref="sbyte"/> underlying type.
	/// </summary>
	SByte,

	/// <summary>
	/// The <see cref="short"/> underlying type.
	/// </summary>
	Int16,

	/// <summary>
	/// The <see cref="ushort"/> underlying type.
	/// </summary>
	UInt16,

	/// <summary>
	/// The <see cref="int"/> underlying type, the C# default.
	/// </summary>
	Int32,

	/// <summary>
	/// The <see cref="uint"/> underlying type.
	/// </summary>
	UInt32,

	/// <summary>
	/// The <see cref="long"/> underlying type.
	/// </summary>
	Int64,

	/// <summary>
	/// The <see cref="ulong"/> underlying type.
	/// </summary>
	UInt64,
}
