using Purview.SourceGeneratorFramework.Analyzers;
using Purview.SourceGeneratorFramework.Testing;
using Purview.SourceGeneratorFramework.Testing.TUnit;

namespace Purview.SourceGeneratorFramework.CodeFixers;

public sealed class PreferStructuredCodeWriterStatementCodeFixProviderTests
	: TUnitCodeFixTestBase<
		PreferStructuredCodeWriterStatementAnalyzer,
		PreferStructuredCodeWriterStatementCodeFixProvider
	>
{
	static readonly CodeFixTestOptions Options = new()
	{
		EquivalenceKey = PreferStructuredCodeWriterStatementCodeFixProvider.EquivalenceKey,
		AdditionalAssemblyTypes = [typeof(CodeWriter), typeof(GenerationSettings)],
	};

	[Test]
	public async Task Line_WithReturnStatement_UsesReturn(CancellationToken cancellationToken)
	{
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

		var result = await ApplyCodeFixAsync(source, Options, cancellationToken);

		await Assert.That(result).HasDiagnostic(PreferStructuredCodeWriterStatementAnalyzer.Rule.Id);
		await Assert.That(result.FixedSource).Contains("writer.Return(\"value\")");
	}

	[Test]
	public async Task Line_WithMethodCall_UsesMethodCall(CancellationToken cancellationToken)
	{
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

		var result = await ApplyCodeFixAsync(source, Options, cancellationToken);

		await Assert.That(result.FixedSource).Contains("writer.MethodCall(\"Process\", \"item\")");
	}

	[Test]
	public async Task Line_WithReceiverMethodCall_UsesMethodCallOn(CancellationToken cancellationToken)
	{
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

		var result = await ApplyCodeFixAsync(source, Options, cancellationToken);

		await Assert.That(result.FixedSource).Contains("writer.MethodCallOn(\"variable\", \"Process\", \"item\")");
	}

	[Test]
	public async Task Line_WithAwaitedReceiverMethodCall_UsesAwaitedMethodCallOn(CancellationToken cancellationToken)
	{
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

		var result = await ApplyCodeFixAsync(source, Options, cancellationToken);

		await Assert
			.That(result.FixedSource)
			.Contains("writer.AwaitedMethodCallOn(\"service\", \"LoadAsync\", \"token\")");
	}

	[Test]
	public async Task Line_WithAssignment_UsesAssignment(CancellationToken cancellationToken)
	{
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

		var result = await ApplyCodeFixAsync(source, Options, cancellationToken);

		await Assert.That(result.FixedSource).Contains("writer.Assignment(\"value\", \"42\")");
	}

	[Test]
	public async Task Line_WithComment_UsesComment(CancellationToken cancellationToken)
	{
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

		var result = await ApplyCodeFixAsync(source, Options, cancellationToken);

		await Assert.That(result.FixedSource).Contains("writer.Comment(\"generated note\")");
	}

	[Test]
	public async Task Line_WithUsingDirective_UsesUsing(CancellationToken cancellationToken)
	{
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

		var result = await ApplyCodeFixAsync(source, Options, cancellationToken);

		await Assert.That(result.FixedSource).Contains("writer.Using(\"System\")");
	}

	[Test]
	public async Task Line_WithXmlDocumentationComment_DoesNotRegisterFix(CancellationToken cancellationToken)
	{
		const string source = """
			using Purview.SourceGeneratorFramework;

			class Emitter
			{
				public void Emit()
				{
					var writer = new CodeWriter(new GenerationSettings("G"));
					writer.Line("/// summary");
				}
			}
			""";

		await Assert
			.That(async () => await ApplyCodeFixAsync(source, Options, cancellationToken))
			.Throws<InvalidOperationException>();
	}

	[Test]
	public async Task Line_WithBlockHeader_DoesNotRegisterFix(CancellationToken cancellationToken)
	{
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

		await Assert
			.That(async () => await ApplyCodeFixAsync(source, Options, cancellationToken))
			.Throws<InvalidOperationException>();
	}
}
