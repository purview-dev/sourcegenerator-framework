using Purview.SourceGeneratorFramework.TestGenerators;
using Purview.SourceGeneratorFramework.Testing.Models;

namespace Purview.SourceGeneratorFramework;

/// <summary>
/// Covers how the runners surface framework log entries: in order, scoped to the run that wrote them, and
/// only when logging is enabled.
/// </summary>
public class GeneratorLoggingTests
{
	const string Source = "public class Anything { }";

	// The framework pipeline logs entries of its own around the generator's, so the generator's messages are
	// asserted as an ordered subsequence rather than as the whole set.
	static async Task AssertWrittenInOrderAsync(IEnumerable<LogEntry> entries)
	{
		var messages = entries.Select(static entry => entry.Message).ToList();
		var positions = OrderedLoggingTestGenerator.Messages.Select(message => messages.IndexOf(message)).ToArray();

		await Assert.That(positions).DoesNotContain(-1);
		await Assert.That(positions.SequenceEqual(positions.Order())).IsTrue();
	}

	[Test]
	public async Task RunAsync_PreservesTheOrderEntriesWereWritten(CancellationToken cancellationToken)
	{
		// Arrange
		SourceGeneratorTestRunner<OrderedLoggingTestGenerator> runner = new();

		// Act
		using var result = await runner.RunAsync(Source, cancellationToken: cancellationToken);

		// Assert
		await AssertWrittenInOrderAsync(result.LogEntries);
	}

	[Test]
	public async Task RunAsync_GivenLoggingDisabled_CapturesNothing(CancellationToken cancellationToken)
	{
		// Arrange
		SourceGeneratorTestRunner<OrderedLoggingTestGenerator> runner = new();

		// Act
		using var result = await runner.RunAsync(
			Source,
			new SourceGeneratorTestOptions { EnableLogging = false },
			cancellationToken
		);

		// Assert
		await Assert.That(result.LogEntries).IsEmpty();
	}

	[Test]
	public async Task RunIncrementalAsync_ScopesEntriesToTheRunThatWroteThem(CancellationToken cancellationToken)
	{
		// Arrange - the incremental runner shares one driver across the sequence, so the sink has to be
		// registered for the whole sequence and drained between runs.
		SourceGeneratorTestRunner<OrderedLoggingTestGenerator> runner = new();

		// Act - this test is about draining, not about incrementality, and it needs a fully cached second
		// run to have anything to prove. Reusing the compilation for the identical rerun is what makes the
		// output step cacheable here; the default of a fresh compilation per run would re-execute it.
		var result = await runner.RunIncrementalAsync(
			[Source],
			new SourceGeneratorTestOptions { ReuseCompilationForIdenticalSources = true },
			cancellationToken
		);

		// Assert - the first run executes the output step and writes its entries in order.
		await AssertWrittenInOrderAsync(result.Runs[0].LogEntries);

		// The rerun reuses the cached output, so it writes nothing. Were the entries accumulated across the
		// sequence rather than drained per run, this would still hold the first run's.
		await Assert.That(result.Runs[1].LogEntries).IsEmpty();
	}

	[Test]
	public async Task RunIncrementalAsync_GivenLoggingDisabled_CapturesNothing(CancellationToken cancellationToken)
	{
		// Arrange
		SourceGeneratorTestRunner<OrderedLoggingTestGenerator> runner = new();

		// Act
		var result = await runner.RunIncrementalAsync(
			[Source],
			new SourceGeneratorTestOptions { EnableLogging = false },
			cancellationToken
		);

		// Assert
		foreach (var run in result.Runs)
			await Assert.That(run.LogEntries).IsEmpty();
	}
}
