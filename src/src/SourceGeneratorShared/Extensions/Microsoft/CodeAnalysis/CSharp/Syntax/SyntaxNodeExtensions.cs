#pragma warning disable CS1591
using System.ComponentModel;

namespace Microsoft.CodeAnalysis.CSharp.Syntax;

[System.Diagnostics.CodeAnalysis.SuppressMessage(
	"Design",
	"CA1034:Nested types should not be visible",
	Justification = "The extension(...) block is lowered to an unnamed nested type; the language requires it to be nested in a static class, and PSGFR34 mandates extension blocks over classic 'this'-parameter methods."
)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static class SyntaxNodeExtensions
{
	extension(MethodDeclarationSyntax method)
	{
		/// <summary>
		/// Gets the declared accessibility of a method declaration syntax node.
		/// </summary>
		/// <returns>The declared accessibility, or null if no explicit accessibility modifier is present.</returns>
		public Accessibility? GetDeclaredAccessibility()
		{
			if (method == null)
				throw new ArgumentNullException(nameof(method));

			if (method.Modifiers.Any(SyntaxKind.PublicKeyword))
				return Accessibility.Public;

			if (method.Modifiers.Any(SyntaxKind.PrivateKeyword))
				return Accessibility.Private;

			if (method.Modifiers.Any(SyntaxKind.ProtectedKeyword))
			{
				return method.Modifiers.Any(SyntaxKind.InternalKeyword)
					? Accessibility.ProtectedOrInternal
					: Accessibility.Protected;
			}

			if (method.Modifiers.Any(SyntaxKind.InternalKeyword))
				return Accessibility.Internal;

			return null; // No explicit accessibility modifier.
		}
	}
}
