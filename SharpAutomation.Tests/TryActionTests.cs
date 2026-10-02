namespace SharpAutomation.Tests;

public class TryActionTests
{
    [Fact]
    public void Run_returns_true_after_the_first_call_that_returns()
    {
        var errors = new List<Exception>();
        int calls = 0;

        bool ok = TryAction.Run(() => calls++, retries: 3, _exceptionList: errors);

        Assert.True(ok);
        Assert.Equal(1, calls);
        Assert.Empty(errors);
    }

    [Fact]
    public void Run_retries_until_a_call_returns_and_keeps_each_exception()
    {
        var errors = new List<Exception>();
        int calls = 0;

        bool ok = TryAction.Run(() =>
        {
            calls++;
            if (calls < 3) throw new TimeoutException($"Attempt {calls} timed out");
        }, retries: 4, _exceptionList: errors);

        Assert.True(ok);
        Assert.Equal(3, calls);
        Assert.Equal(["Attempt 1 timed out", "Attempt 2 timed out"], errors.Select(e => e.Message));
    }

    [Fact]
    public void Run_returns_false_after_retries_plus_one_failed_calls()
    {
        var errors = new List<Exception>();
        int calls = 0;

        bool ok = TryAction.Run(() => { calls++; throw new InvalidOperationException("no"); }, retries: 2, _exceptionList: errors);

        Assert.False(ok);
        Assert.Equal(3, calls);
        Assert.Equal(3, errors.Count);
        Assert.All(errors, e => Assert.IsType<InvalidOperationException>(e));
    }

    [Fact]
    public void Run_without_a_list_drops_the_exceptions()
    {
        Assert.False(TryAction.Run(() => throw new InvalidOperationException("no")));
    }

    [Fact]
    public async Task RunAsync_runs_synchronous_code_with_retries()
    {
        var errors = new List<Exception>();
        int calls = 0;

        bool ok = await TryAction.RunAsync(() =>
        {
            calls++;
            if (calls < 2) throw new IOException("busy");
        }, retries: 1, _exceptionList: errors);

        Assert.True(ok);
        Assert.Equal(2, calls);
        Assert.Single(errors);
    }

    [Fact]
    public async Task RunAsync_returns_false_when_every_call_throws()
    {
        var errors = new List<Exception>();

        bool ok = await TryAction.RunAsync(() => throw new IOException("busy"), retries: 1, _exceptionList: errors);

        Assert.False(ok);
        Assert.Equal(2, errors.Count);
    }
}
