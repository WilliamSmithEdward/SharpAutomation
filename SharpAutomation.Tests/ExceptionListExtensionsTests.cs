using System.Text.Json;

namespace SharpAutomation.Tests;

public class ExceptionListExtensionsTests
{
    private static List<Exception> Sample() =>
    [
        new TimeoutException("The server did not answer"),
        new IOException("The disk is full"),
        new TimeoutException("The server did not answer again"),
        new FileNotFoundException("missing.txt"),
    ];

    [Fact]
    public void FilterByType_and_ContainsType_match_the_exact_type()
    {
        var errors = Sample();

        Assert.Equal(2, errors.FilterByType<TimeoutException>().Count);
        Assert.Single(errors.FilterByType<IOException>());
        Assert.Empty(errors.FilterByType<Exception>());
        Assert.True(errors.ContainsType<FileNotFoundException>());
        Assert.False(errors.ContainsType<SystemException>());
    }

    [Fact]
    public void CountByType_counts_by_full_type_name()
    {
        var counts = Sample().CountByType();

        Assert.Equal(3, counts.Count);
        Assert.Equal(2, counts["System.TimeoutException"]);
        Assert.Equal(1, counts["System.IO.IOException"]);
        Assert.Equal(1, counts["System.IO.FileNotFoundException"]);
    }

    [Fact]
    public void FlattenMessages_joins_the_messages_with_newlines()
    {
        var errors = Sample().Take(2).ToList();

        Assert.Equal("The server did not answer" + Environment.NewLine + "The disk is full", errors.FlattenMessages());
    }

    [Fact]
    public void ToJSON_lists_message_stack_trace_and_type_name()
    {
        var thrown = Thrown.Exception(new IOException("The disk is full"));

        using var json = JsonDocument.Parse(new List<Exception> { thrown, new TimeoutException("<late> & slow") }.ToJSON());
        var items = json.RootElement.EnumerateArray().ToList();

        Assert.Equal(2, items.Count);
        Assert.Equal("The disk is full", items[0].GetProperty("Message").GetString());
        Assert.Equal("System.IO.IOException", items[0].GetProperty("TypeName").GetString());
        Assert.Contains(nameof(Thrown), items[0].GetProperty("StackTrace").GetString());
        Assert.Equal("<late> & slow", items[1].GetProperty("Message").GetString());
        Assert.Equal(JsonValueKind.Null, items[1].GetProperty("StackTrace").ValueKind);
    }

    [Fact]
    public void ToJSON_escapes_html_characters()
    {
        string json = new TimeoutException("<b>").ToJSON();

        Assert.DoesNotContain("<b>", json);
        Assert.Contains("\\u003Cb\\u003E", json);
    }

    [Fact]
    public void ToHTML_holds_each_exception_type_and_message()
    {
        string html = Sample().ToHTML();

        Assert.StartsWith("<br /><br /><h2>Exceptions:</h2><table", html);
        Assert.EndsWith("</table>", html);
        Assert.Contains("System.TimeoutException", html);
        Assert.Contains("The disk is full", html);
    }

    [Fact]
    public async Task ToLogAsync_appends_a_block_per_exception()
    {
        using var folder = new TempFolder();
        string path = folder.File("errors.log");

        await new List<Exception> { new TimeoutException("first"), new IOException("second") }.ToLogAsync(path);
        await new IOException("third").ToLogAsync(path);

        string log = File.ReadAllText(path);
        Assert.Equal(3, log.Split("Timestamp: ").Length - 1);
        Assert.Contains("Exception: System.TimeoutException", log);
        Assert.Contains("Message: first", log);
        Assert.Contains("Message: second", log);
        Assert.Contains("Message: third", log);
        Assert.True(log.IndexOf("first", StringComparison.Ordinal) < log.IndexOf("third", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ToLogAsync_writes_nothing_for_an_empty_list()
    {
        using var folder = new TempFolder();
        string path = folder.File("errors.log");

        await new List<Exception>().ToLogAsync(path);

        Assert.False(File.Exists(path));
    }

    [Fact]
    public async Task ToLogAsync_and_ToJSON_refuse_a_null_list()
    {
        List<Exception> none = null!;

        await Assert.ThrowsAsync<ArgumentNullException>(() => none.ToLogAsync("unused.log"));
        Assert.Throws<ArgumentNullException>(() => none.ToJSON());
    }
}
