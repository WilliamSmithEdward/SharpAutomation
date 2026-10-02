using System.Diagnostics;

namespace SharpAutomation.Tests;

// The RunAsync overload for a Func<Task>, with its cancellation token.
public class TryActionCancellationTests
{
    [Fact]
    public async Task Cancelling_during_the_wait_stops_the_retries()
    {
        using var cancel = new CancellationTokenSource();
        var errors = new List<Exception>();
        int calls = 0;
        var clock = Stopwatch.StartNew();

        var run = TryAction.RunAsync(() =>
        {
            calls++;
            cancel.CancelAfter(100);
            throw new IOException("busy");
        }, retries: 5, waitBetweenTriesSeconds: 30, _exceptionList: errors, cancellationToken: cancel.Token);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run);
        Assert.True(clock.Elapsed < TimeSpan.FromSeconds(10));
        Assert.Equal(1, calls);
        Assert.Single(errors);
    }

    [Fact]
    public async Task A_cancelled_token_runs_nothing()
    {
        int calls = 0;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => TryAction.RunAsync(
            () => { calls++; return Task.CompletedTask; }, cancellationToken: new CancellationToken(canceled: true)));

        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task Cancellation_the_action_observes_is_not_collected_as_a_failure()
    {
        using var cancel = new CancellationTokenSource();
        var errors = new List<Exception>();

        var run = TryAction.RunAsync(async () =>
        {
            cancel.Cancel();
            await Task.Delay(Timeout.Infinite, cancel.Token);
        }, retries: 3, _exceptionList: errors, cancellationToken: cancel.Token);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run);
        Assert.Empty(errors);
    }

    [Fact]
    public async Task Without_cancellation_an_OperationCanceledException_is_an_ordinary_failure()
    {
        var errors = new List<Exception>();

        bool ok = await TryAction.RunAsync(
            () => Task.FromException(new OperationCanceledException("the server gave up")),
            _exceptionList: errors, cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(ok);
        Assert.IsType<OperationCanceledException>(Assert.Single(errors));
    }

    [Fact]
    public async Task A_null_action_throws_ArgumentNullException()
    {
        Assert.Equal("action", (await Assert.ThrowsAsync<ArgumentNullException>(
            () => TryAction.RunAsync((Func<Task>)null!, cancellationToken: TestContext.Current.CancellationToken))).ParamName);
    }
}
