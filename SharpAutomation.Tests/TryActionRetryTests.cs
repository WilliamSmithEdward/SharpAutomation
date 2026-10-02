using System.Diagnostics;

namespace SharpAutomation.Tests;

// Async lambdas, waits and argument checks. Cancellation is in TryActionCancellationTests.
public class TryActionRetryTests
{
    [Fact]
    public async Task RunAsync_returns_after_an_async_lambda_has_finished()
    {
        bool finished = false;

        bool ok = await TryAction.RunAsync(async () =>
        {
            await Task.Delay(200, TestContext.Current.CancellationToken);
            finished = true;
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(ok);
        Assert.True(finished);
    }

    // Before the fix the lambda was async void: this exception ended the test process.
    [Fact]
    public async Task RunAsync_catches_what_an_async_lambda_throws_after_its_first_await()
    {
        var errors = new List<Exception>();
        int calls = 0;

        bool ok = await TryAction.RunAsync(async () =>
        {
            calls++;
            await Task.Delay(20, TestContext.Current.CancellationToken);
            if (calls < 3) throw new TimeoutException($"Attempt {calls} timed out");
        }, retries: 3, _exceptionList: errors, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(ok);
        Assert.Equal(3, calls);
        Assert.Equal(["Attempt 1 timed out", "Attempt 2 timed out"], errors.Select(e => e.Message));
    }

    [Fact]
    public async Task RunAsync_returns_false_when_every_awaited_attempt_throws()
    {
        var errors = new List<Exception>();

        bool ok = await TryAction.RunAsync(async () =>
        {
            await Task.Yield();
            throw new IOException("busy");
        }, retries: 2, _exceptionList: errors, cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(ok);
        Assert.Equal(3, errors.Count);
    }

    [Fact]
    public void Run_does_not_wait_after_the_last_attempt()
    {
        var clock = Stopwatch.StartNew();

        bool ok = TryAction.Run(() => throw new IOException("busy"), retries: 1, waitBetweenTriesSeconds: 1);

        Assert.False(ok);
        Assert.InRange(clock.Elapsed, TimeSpan.FromSeconds(0.9), TimeSpan.FromSeconds(1.8));
    }

    [Fact]
    public async Task RunAsync_does_not_wait_after_the_last_attempt()
    {
        var clock = Stopwatch.StartNew();

        bool ok = await TryAction.RunAsync((Action)(() => throw new IOException("busy")), retries: 1, waitBetweenTriesSeconds: 1);

        Assert.False(ok);
        Assert.InRange(clock.Elapsed, TimeSpan.FromSeconds(0.9), TimeSpan.FromSeconds(1.8));
    }

    [Fact]
    public void A_single_failed_attempt_returns_at_once()
    {
        var clock = Stopwatch.StartNew();

        Assert.False(TryAction.Run(() => throw new IOException("busy"), retries: 0, waitBetweenTriesSeconds: 5));

        Assert.True(clock.Elapsed < TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData(-1, 0, "retries")]
    [InlineData(0, -1, "waitBetweenTriesSeconds")]
    [InlineData(0, 2_147_484, "waitBetweenTriesSeconds")]
    public async Task An_argument_out_of_range_is_refused_before_the_action_runs(int retries, int wait, string name)
    {
        int calls = 0;

        Assert.Equal(name, Assert.Throws<ArgumentOutOfRangeException>(
            () => TryAction.Run(() => calls++, retries, wait)).ParamName);
        Assert.Equal(name, (await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => TryAction.RunAsync(() => { calls++; }, retries, wait))).ParamName);
        Assert.Equal(name, (await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => TryAction.RunAsync(async () => { calls++; await Task.Yield(); }, retries, wait,
                cancellationToken: TestContext.Current.CancellationToken))).ParamName);
        Assert.Equal(0, calls);
    }

    [Fact]
    public void The_longest_wait_is_accepted()
    {
        Assert.True(TryAction.Run(() => { }, retries: 0, waitBetweenTriesSeconds: 2_147_483));
    }

    [Fact]
    public async Task A_null_action_throws_ArgumentNullException()
    {
        Assert.Equal("action", Assert.Throws<ArgumentNullException>(() => TryAction.Run(null!)).ParamName);
        Assert.Equal("action", (await Assert.ThrowsAsync<ArgumentNullException>(() => TryAction.RunAsync((Action)null!))).ParamName);
    }
}
