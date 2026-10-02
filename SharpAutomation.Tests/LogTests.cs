namespace SharpAutomation.Tests;

public class LogTests
{
    [Fact]
    public void WriteEntryAsync_appends_a_timestamp_and_the_entry()
    {
        using var folder = new TempFolder();
        string path = folder.File("import.log");

        Log.WriteEntryAsync("Import started", path);
        Log.WriteEntryAsync("Import finished", path);

        string[] lines = File.ReadAllLines(path);
        Assert.Equal(["Entry: Import started", "Entry: Import finished"], lines.Where(l => l.StartsWith("Entry: ")));
        Assert.Equal(2, lines.Count(l => l.StartsWith("Timestamp: ")));
    }
}
