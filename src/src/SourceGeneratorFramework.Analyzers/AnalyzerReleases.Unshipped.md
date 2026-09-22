; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
PSGFR30 | Purview.SourceGeneratorFramework | Warning | Prefer static lambdas in incremental generator pipelines
PSGFR31 | Purview.SourceGeneratorFramework | Warning | Prefer TargetSymbol over GetDeclaredSymbol(TargetNode)
PSGFR32 | Purview.SourceGeneratorFramework | Warning | Avoid NormalizeWhitespace when generating source
PSGFR33 | Purview.SourceGeneratorFramework | Warning | Pipeline model retains a Roslyn object
PSGFR34 | Purview.SourceGeneratorFramework | Info | Prefer extension blocks over classic extension methods
PSGFR35 | Purview.SourceGeneratorFramework | Warning | Extension class name does not match the extended type
PSGFR36 | Purview.SourceGeneratorFramework | Warning | Extension class is not placed in the extended type's namespace/folder
PSGFR37 | Purview.SourceGeneratorFramework | Warning | Extension class extends multiple receiver types
PSGFR38 | Purview.SourceGeneratorFramework | Warning | Extension class is missing EditorBrowsable
PSGFR39 | Purview.SourceGeneratorFramework | Error | Roslyn component must produce a self-contained analyzer
TLB0014 | TypeLibrary | Warning | Type library partial extension is declared in a different namespace
TLB0015 | TypeLibrary | Info | Type library partial extension must be declared 'public static partial'
TLB0016 | TypeLibrary | Error | Enum value member type must be TypeIdentity or EnumValueDefinition
TLB0017 | TypeLibrary | Error | Enum value member references an enum type that is not declared
TLB0018 | TypeLibrary | Error | Duplicate enum value member
TLB0019 | TypeLibrary | Info | Duplicate enum value
TLB0020 | TypeLibrary | Error | Enum values member references a type that is not an enum