namespace Fixture.Generator;

/// <summary>
/// Internal diagnostic identity shared with the companion code-fix component through
/// <c>InternalsVisibleTo</c>, making the code-fix assembly a genuine runtime dependency of the
/// generator assembly.
/// </summary>
static class FixtureComponentDiagnostics
{
	// Deliberately not a const: a const would be inlined into the code-fix assembly and the
	// component -> component assembly reference would vanish from the metadata.
	internal static readonly string DiagnosticId = "FIX0001";
}
