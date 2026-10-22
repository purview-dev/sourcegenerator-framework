#if NETSTANDARD2_0

using System.ComponentModel;

// netstandard2.0 has no NotNullWhenAttribute, so it is polyfilled here. The C# compiler recognises the
// nullability attributes by name regardless of accessibility, so this is internal: a public polyfill
// would export a type from lib/netstandard2.0 that lib/net8.0 does not have, which is an API-surface
// difference between the package's own frameworks (CP0001) and makes the type appear to be part of this
// package's contract when it is an implementation detail.
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace System.Diagnostics.CodeAnalysis;

#pragma warning restore IDE0130

[EditorBrowsable(EditorBrowsableState.Never)]
[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
sealed class NotNullWhenAttribute(bool returnValue) : Attribute
{
	public bool ReturnValue { get; } = returnValue;
}

#endif
