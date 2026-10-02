namespace SharpAutomation.Tests;

// Log.AppendEntryAsync, the awaitable way to write an entry, and the default file name.
public class LogAppendEntryTests
{
    [Fact]
    public async Task AppendEntryAsync_can_be_awaited_and_throws_to_the_caller()
    {
        using var folder = new TempFolder();
        string path = folder.File("import.log");

        await Log.AppendEntryAsync("Import started", path, TestContext.Current.CancellationToken);
        await Log.AppendEntryAsync("Import finished", path, TestContext.Current.CancellationToken);

        Assert.Equal(["Entry: Import started", "Entry: Import finished"],
            File.ReadAllLines(path).Where(l => l.StartsWith("Entry: ")));
        await Assert.ThrowsAsync<DirectoryNotFoundException>(
            () => Log.AppendEntryAsync("entry", Path.Combine(folder.Root, "missing", "x.log"), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task AppendEntryAsync_honours_cancellation()
    {
        using var folder = new TempFolder();
        string path = folder.File("import.log");

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => Log.AppendEntryAsync("entry", path, new CancellationToken(canceled: true)));
        Assert.False(File.Exists(path));
    }

    [Fact]
    public async Task AppendEntryAsync_writes_beside_the_other_writers()
    {
        using var folder = new TempFolder();
        string path = folder.File("busy.log");
        const int writers = 30;

        var writes = Enumerable.Range(0, writers).Select(i => (i % 3) switch
        {
            0 => Task.Run(() => Log.AppendEntryAsync($"entry {i}", path)),
            1 => Task.Run(() => Log.WriteEntryAsync($"entry {i}", path)),
            _ => Task.Run(() => new InvalidOperationException($"entry {i}").ToLogAsync(path)),
        });
        await Task.WhenAll(writes);

        Assert.Equal(LogWritingTests.Expected(writers), LogWritingTests.Logged(path));
    }

    [Fact]
    public async Task AppendEntryAsync_waits_for_a_file_another_writer_holds_for_a_moment()
    {
        using var folder = new TempFolder();
        string path = folder.File("held.log");
        var held = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
        var release = Task.Run(async () =>
        {
            await Task.Delay(300, TestContext.Current.CancellationToken);
            await held.DisposeAsync();
        }, TestContext.Current.CancellationToken);

        await Log.AppendEntryAsync("after the other writer", path, TestContext.Current.CancellationToken);
        await release;

        Assert.Contains("Entry: after the other writer", File.ReadAllText(path));
    }

    [Fact]
    public async Task AppendEntryAsync_indents_line_breaks_in_the_entry()
    {
        using var folder = new TempFolder();
        string path = folder.File("forged.log");

        await Log.AppendEntryAsync(LogWritingTests.Forged, path, TestContext.Current.CancellationToken);

        LogWritingTests.AssertNotForged(path, entries: 1, exceptions: 0);
    }

    [Fact]
    public void The_default_file_name_uses_a_24_hour_clock()
    {
        var afternoon = new DateTime(2026, 10, 2, 13, 5, 9);
        var night = new DateTime(2026, 10, 2, 1, 5, 9);

        Assert.Equal(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "20261002_130509.log"), Log.DefaultLogFilePath(afternoon));
        Assert.Equal(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "20261002_010509.log"), Log.DefaultLogFilePath(night));
    }
}
