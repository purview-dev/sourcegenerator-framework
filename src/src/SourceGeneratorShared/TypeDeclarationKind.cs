namespace Purview.SourceGeneratorFramework;

/// <summary>
/// Identifies the C# declaration emitted for a generated type.
/// </summary>
public enum TypeDeclarationKind
{
	/// <summary>
	/// A class declaration.
	/// </summary>
	Class,

	/// <summary>
	/// A struct declaration.
	/// </summary>
	Struct,

	/// <summary>
	/// A record class declaration.
	/// </summary>
	RecordClass,

	/// <summary>
	/// A record struct declaration.
	/// </summary>
	RecordStruct,

	/// <summary>
	/// An interface declaration.
	/// </summary>
	Interface,

	/// <summary>
	/// An enum declaration.
	/// </summary>
	Enum,

	/// <summary>
	/// A delegate declaration.
	/// </summary>
	Delegate,

	/// <summary>
	/// A C# 15 union declaration, such as <c>public union Pet(Cat, Dog);</c>.
	/// </summary>
	/// <remarks>
	/// A union is emitted as a struct-backed declaration with a fixed set of case types. Set
	/// <see cref="TypeDeclarationOptions.UnionCaseTypes"/> to the case types; primary-constructor
	/// parameters and a base type are not valid for a union declaration.
	/// </remarks>
	Union,
}
