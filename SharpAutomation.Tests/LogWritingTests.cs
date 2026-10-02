namespace SharpAutomation.Tests;

// What Log.WriteEntryAsync and ToLogAsync do with the file. Log.AppendEntryAsync and the
// default file name are in LogAppendEntryTests.
public class LogWritingTests
{
    internal const string Separator = "----------------------------------------------------------------------------";

    [Fact]
    public void WriteEntryAsync_has_written_a_long_entry_when_it_returns()
    {
        using var folder = new TempFolder();
        string path = folder.File("long.log");
        string entry = new('x', 200_000);

        Log.WriteEntryAsync(entry, path);

        Assert.Contains("Entry: " + entry, File.ReadAllText(path));
    }

    // Before the fix this exception was raised on the thread pool and ended the test process.
    [Fact]
    public void WriteEntryAsync_throws_to_the_caller_when_the_folder_is_missing()
    {
        using var folder = new TempFolder();
        string path = Path.Combine(folder.Root, "missing", "import.log");

        Assert.Throws<DirectoryNotFoundException>(() => Log.WriteEntryAsync("entry", path));
    }

    [Fact]
    public async Task Writes_to_one_file_at_the_same_time_all_arrive_whole()
    {
        using var folder = new TempFolder();
        string path = folder.File("busy.log");
        const int writers = 40;

        var writes = Enumerable.Range(0, writers).Select(i => i % 2 == 0
            ? Task.Run(() => Log.WriteEntryAsync($"entry {i}", path))
            : Task.Run(() => new InvalidOperationException($"entry {i}").ToLogAsync(path)));
        await Task.WhenAll(writes);

        Assert.Equal(Expected(writers), Logged(path));
        Assert.Equal(writers, File.ReadAllLines(path).Count(l => l == Separator));
    }

    [Fact]
    public async Task A_file_another_writer_holds_for_a_moment_is_written_once_it_is_free()
    {
        using var folder = new TempFolder();
        string path = folder.File("held.log");
        var held = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
        var release = Task.Run(async () =>
        {
            await Task.Delay(300, TestContext.Current.CancellationToken);
            await held.DisposeAsync();
        }, TestContext.Current.CancellationToken);

        await new InvalidOperationException("after the other writer").ToLogAsync(path);
        Log.WriteEntryAsync("and an entry", path);
        await release;

        string log = File.ReadAllText(path);
        Assert.Contains("Message: after the other writer", log);
        Assert.Contains("Entry: and an entry", log);
    }

    [Fact]
    public async Task A_line_break_in_logged_text_cannot_start_a_line_of_its_own()
    {
        using var folder = new TempFolder();
        string path = folder.File("forged.log");

        Log.WriteEntryAsync(Forged, path);
        await new InvalidOperationException(Forged).ToLogAsync(path);

        AssertNotForged(path, entries: 1, exceptions: 1);
    }

    [Fact]
    public async Task A_stack_trace_is_kept_line_by_line_and_indented()
    {
        using var folder = new TempFolder();
        string path = folder.File("trace.log");
        var thrown = Thrown.Exception(() => Outer());

        await thrown.ToLogAsync(path);

        string[] frames = thrown.StackTrace!.Split(Environment.NewLine);
        Assert.True(frames.Length >= 3);
        string[] lines = File.ReadAllLines(path);
        int start = Array.IndexOf(lines, "StackTrace: " + frames[0]);
        Assert.True(start >= 0);
        for (int i = 1; i < frames.Length; i++) Assert.Equal("  " + frames[i], lines[start + i]);

        static void Outer() => Inner();
        static void Inner() => throw new IOException("The disk is full");
    }

    /// <summary>Text that, logged as it is, would look like a separator and a second entry.</summary>
    internal const string Forged = "ok\r\n" + Separator + "\r\n\r\nTimestamp: 01/01/2000 00:00:00\nEntry: forged\rException: Fake";

    internal static void AssertNotForged(string path, int entries, int exceptions)
    {
        string[] lines = File.ReadAllLines(path);
        Assert.Equal(entries + exceptions, lines.Count(l => l.StartsWith("Timestamp: ")));
        Assert.Equal(entries, lines.Count(l => l.StartsWith("Entry: ")));
        Assert.Equal(exceptions, lines.Count(l => l.StartsWith("Exception: ")));
        Assert.Equal(entries + exceptions, lines.Count(l => l == Separator));
        Assert.Contains("  Timestamp: 01/01/2000 00:00:00", lines);
        Assert.Contains("  Entry: forged", lines);
    }

    internal static IEnumerable<string> Expected(int writers) =>
        Enumerable.Range(0, writers).Select(i => $"entry {i}").Order(StringComparer.Ordinal);

    internal static IEnumerable<string> Logged(string path) =>
        File.ReadAllLines(path)
            .Where(l => l.StartsWith("Entry: entry ") || l.StartsWith("Message: entry "))
            .Select(l => l[(l.IndexOf(':') + 2)..])
            .Order(StringComparer.Ordinal);
}
