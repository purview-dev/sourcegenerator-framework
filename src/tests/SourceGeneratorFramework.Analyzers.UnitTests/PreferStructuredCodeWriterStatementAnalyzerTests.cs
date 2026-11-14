using Microsoft.CodeAnalysis;
using Purview.SourceGeneratorFramework.Testing;
using Purview.SourceGeneratorFramework.Testing.TUnit;

namespace Purview.SourceGeneratorFramework.Analyzers;

public sealed class PreferStructuredCodeWriterStatementAnalyzerTests
	: TUnitDiagnosticAnalyzerTestBase<PreferStructuredCodeWriterStatementAnalyzer>
{
	static readonly AnalyzerTestOptions Options = new()
	{
		AdditionalAssemblyTypes = [typeof(CodeWriter), typeof(GenerationSettings)],
	};

	[Test]
	public async Task Rule_GivenStructuredStatementSuggestion_IsWarning()
	{
		// A suggestion that a consumer is expected to act on must appear as a build warning rather than
		// only as an IDE suggestion.
		await Assert
			.That(PreferStructuredCodeWriterStatementAnalyzer.Rule.DefaultSeverity)
			.IsEqualTo(DiagnosticSeverity.Warning);
	}

	[Test]
	public async Task Line_WithReturnStatement_ReportsDiagnostic(CancellationToken cancellationToken)
	{
		// Arrange
		const string source = """
			using Purview.SourceGeneratorFramework;

			class Emitter
			{
				public void Emit()
				{
					var writer = new CodeWriter(new GenerationSettings("G"));
					writer.Line("return value;");
				}
			}
			""";

		// Act
		var result = await AnalyzeAsync(source, Options, cancellationToken);

		// Assert
		var diagnostic = await Assert.That(result).HasDiagnostic(PreferStructuredCodeWriterStatementAnalyzer.Rule.Id);
		await Assert.That(diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture)).Contains("Return");
	}

	[Test]
	public async Task Line_WithThrowStatement_ReportsDiagnostic(CancellationToken cancellationToken)
	{
		// Arrange
		const string source = """
			using Purview.SourceGeneratorFramework;

			class Emitter
			{
				public void Emit()
				{
					var writer = new CodeWriter(new GenerationSettings("G"));
					writer.Line("throw new global::System.InvalidOperationException(\"failed\");");
				}
			}
			""";

		// Act
		var result = await AnalyzeAsync(source, Options, cancellationToken);

		// Assert
		await Assert.That(result).HasDiagnostics(1);
		await Assert.That(result).HasDiagnostic(PreferStructuredCodeWriterStatementAnalyzer.Rule.Id);
	}

	[Test]
	public async Task Line_WithAwaitedMethodCall_ReportsDiagnostic(CancellationToken cancellationToken)
	{
		// Arrange
		const string source = """
			using Purview.SourceGeneratorFramework;

			class Emitter
			{
				public void Emit()
				{
					var writer = new CodeWriter(new GenerationSettings("G"));
					writer.Line("await LoadAsync(token);");
				}
			}
			""";

		// Act
		var result = await AnalyzeAsync(source, Options, cancellationToken);

		// Assert
		await Assert.That(result).HasDiagnostics(1);
		await Assert.That(result).HasDiagnostic(PreferStructuredCodeWriterStatementAnalyzer.Rule.Id);
	}

	[Test]
	public async Task Line_WithMethodCall_ReportsDiagnostic(CancellationToken cancellationToken)
	{
		// Arrange
		const string source = """
			using Purview.SourceGeneratorFramework;

			class Emitter
			{
				public void Emit()
				{
					var writer = new CodeWriter(new GenerationSettings("G"));
					writer.Line("Process(item);");
				}
			}
			""";

		// Act
		var result = await AnalyzeAsync(source, Options, cancellationToken);

		// Assert
		var diagnostic = await Assert.That(result).HasDiagnostic(PreferStructuredCodeWriterStatementAnalyzer.Rule.Id);
		await Assert
			.That(diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture))
			.Contains("MethodCall");
	}

	[Test]
	public async Task Line_WithIfStatement_SuggestsIfBlock(CancellationToken cancellationToken)
	{
		// Arrange
		const string source = """
			using Purview.SourceGeneratorFramework;

			class Emitter
			{
				public void Emit()
				{
					var writer = new CodeWriter(new GenerationSettings("G"));
					writer.Line("if (enabled)");
				}
			}
			""";

		// Act
		var result = await AnalyzeAsync(source, Options, cancellationToken);

		// Assert
		var diagnostic = await Assert.That(result).HasDiagnostic(PreferStructuredCodeWriterStatementAnalyzer.Rule.Id);
		await Assert.That(diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture)).Contains("IfBlock");
	}

	[Test]
	public async Task Line_WithElseIfStatement_SuggestsElseIf(CancellationToken cancellationToken)
	{
		// Arrange
		const string source = """
			using Purview.SourceGeneratorFramework;

			class Emitter
			{
				public void Emit()
				{
					var writer = new CodeWriter(new GenerationSettings("G"));
					writer.Line("else if (retry)");
				}
			}
			""";

		// Act
		var result = await AnalyzeAsync(source, Options, cancellationToken);

		// Assert
		var diagnostic = await Assert.That(result).HasDiagnostic(PreferStructuredCodeWriterStatementAnalyzer.Rule.Id);
		await Assert.That(diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture)).Contains("ElseIf");
	}

	[Test]
	public async Task Line_WithElseStatement_SuggestsElse(CancellationToken cancellationToken)
	{
		// Arrange
		const string source = """
			using Purview.SourceGeneratorFramework;

			class Emitter
			{
				public void Emit()
				{
					var writer = new CodeWriter(new GenerationSettings("G"));
					writer.Line("else");
				}
			}
			""";

		// Act
		var result = await AnalyzeAsync(source, Options, cancellationToken);

		// Assert
		var diagnostic = await Assert.That(result).HasDiagnostic(PreferStructuredCodeWriterStatementAnalyzer.Rule.Id);
		await Assert.That(diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture)).Contains("Else");
	}

	[Test]
	public async Task Line_WithReceiverMethodCall_SuggestsMethodCallOn(CancellationToken cancellationToken)
	{
		// Arrange
		const string source = """
			using Purview.SourceGeneratorFramework;

			class Emitter
			{
				public void Emit()
				{
					var writer = new CodeWriter(new GenerationSettings("G"));
					writer.Line("variable.Process(item);");
				}
			}
			""";

		// Act
		var result = await AnalyzeAsync(source, Options, cancellationToken);

		// Assert
		var diagnostic = await Assert.That(result).HasDiagnostic(PreferStructuredCodeWriterStatementAnalyzer.Rule.Id);
		await Assert
			.That(diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture))
			.Contains("MethodCallOn");
	}

	[Test]
	public async Task Line_WithAwaitedReceiverMethodCall_SuggestsAwaitedMethodCallOn(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source = """
			using Purview.SourceGeneratorFramework;

			class Emitter
			{
				public void Emit()
				{
					var writer = new CodeWriter(new GenerationSettings("G"));
					writer.Line("await service.LoadAsync(token);");
				}
			}
			""";

		// Act
		var result = await AnalyzeAsync(source, Options, cancellationToken);

		// Assert
		var diagnostic = await Assert.That(result).HasDiagnostic(PreferStructuredCodeWriterStatementAnalyzer.Rule.Id);
		await Assert
			.That(diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture))
			.Contains("AwaitedMethodCallOn");
	}

	[Test]
	public async Task Line_WithAssignment_ReportsDiagnostic(CancellationToken cancellationToken)
	{
		// Arrange
		const string source = """
			using Purview.SourceGeneratorFramework;

			class Emitter
			{
				public void Emit()
				{
					var writer = new CodeWriter(new GenerationSettings("G"));
					writer.Line("value = 42;");
				}
			}
			""";

		// Act
		var result = await AnalyzeAsync(source, Options, cancellationToken);

		// Assert
		await Assert.That(result).HasDiagnostics(1);
		await Assert.That(result).HasDiagnostic(PreferStructuredCodeWriterStatementAnalyzer.Rule.Id);
	}

	[Test]
	public async Task Line_WithUsingDirective_ReportsDiagnostic(CancellationToken cancellationToken)
	{
		// Arrange
		const string source = """
			using Purview.SourceGeneratorFramework;

			class Emitter
			{
				public void Emit()
				{
					var writer = new CodeWriter(new GenerationSettings("G"));
					writer.Line("using System;");
				}
			}
			""";

		// Act
		var result = await AnalyzeAsync(source, Options, cancellationToken);

		// Assert
		await Assert.That(result).HasDiagnostics(1);
		await Assert.That(result).HasDiagnostic(PreferStructuredCodeWriterStatementAnalyzer.Rule.Id);
	}

	[Test]
	public async Task Line_WithComment_ReportsDiagnostic(CancellationToken cancellationToken)
	{
		// Arrange
		const string source = """
			using Purview.SourceGeneratorFramework;

			class Emitter
			{
				public void Emit()
				{
					var writer = new CodeWriter(new GenerationSettings("G"));
					writer.Line("// generated note");
				}
			}
			""";

		// Act
		var result = await AnalyzeAsync(source, Options, cancellationToken);

		// Assert
		await Assert.That(result).HasDiagnostics(1);
		await Assert.That(result).HasDiagnostic(PreferStructuredCodeWriterStatementAnalyzer.Rule.Id);
	}

	[Test]
	public async Task Line_WithBlankLine_DoesNotReportDiagnostic(CancellationToken cancellationToken)
	{
		// Arrange
		const string source = """
			using Purview.SourceGeneratorFramework;

			class Emitter
			{
				public void Emit()
				{
					var writer = new CodeWriter(new GenerationSettings("G"));
					writer.Line();
				}
			}
			""";

		// Act
		var result = await AnalyzeAsync(source, Options, cancellationToken);

		// Assert
		await Assert.That(result).HasNoDiagnostics();
	}

	[Test]
	public async Task Line_WithDeclaration_DoesNotReportDiagnostic(CancellationToken cancellationToken)
	{
		// Arrange
		const string source = """
			using Purview.SourceGeneratorFramework;

			class Emitter
			{
				public void Emit()
				{
					var writer = new CodeWriter(new GenerationSettings("G"));
					writer.Line("public class C { }");
				}
			}
			""";

		// Act
		var result = await AnalyzeAsync(source, Options, cancellationToken);

		// Assert
		await Assert.That(result).HasNoDiagnostics();
	}

	[Test]
	public async Task Line_WithUsingStatement_DoesNotReportDiagnostic(CancellationToken cancellationToken)
	{
		// Arrange
		const string source = """
			using Purview.SourceGeneratorFramework;

			class Emitter
			{
				public void Emit()
				{
					var writer = new CodeWriter(new GenerationSettings("G"));
					writer.Line("using (var stream = Open())");
				}
			}
			""";

		// Act
		var result = await AnalyzeAsync(source, Options, cancellationToken);

		// Assert
		await Assert.That(result).HasNoDiagnostics();
	}

	[Test]
	public async Task Line_WithMultilineStatement_DoesNotReportDiagnostic(CancellationToken cancellationToken)
	{
		// Arrange
		const string source = """
			using Purview.SourceGeneratorFramework;

			class Emitter
			{
				public void Emit()
				{
					var writer = new CodeWriter(new GenerationSettings("G"));
					writer.Line("return value\n\t+ other;");
				}
			}
			""";

		// Act
		var result = await AnalyzeAsync(source, Options, cancellationToken);

		// Assert
		await Assert.That(result).HasNoDiagnostics();
	}

	[Test]
	public async Task MethodCall_DoesNotReportDiagnostic(CancellationToken cancellationToken)
	{
		// Arrange
		const string source = """
			using Purview.SourceGeneratorFramework;

			class Emitter
			{
				public void Emit()
				{
					var writer = new CodeWriter(new GenerationSettings("G"));
					writer.MethodCall("Run", "value");
				}
			}
			""";

		// Act
		var result = await AnalyzeAsync(source, Options, cancellationToken);

		// Assert
		await Assert.That(result).HasNoDiagnostics();
	}

	[Test]
	public async Task Line_OnOtherType_DoesNotReportDiagnostic(CancellationToken cancellationToken)
	{
		// Arrange
		const string source = """
			class Emitter
			{
				public void Emit()
				{
					var writer = new OtherWriter();
					writer.Line("return value;");
				}
			}

			class OtherWriter
			{
				public void Line(string value) { }
			}
			""";

		// Act
		var result = await AnalyzeAsync(source, Options, cancellationToken);

		// Assert
		await Assert.That(result).HasNoDiagnostics();
	}
}
