namespace SharpAutomation.Tests;

public class ExceptionListArgumentTests
{
    public static TheoryData<string> Methods() =>
        ["ToHTML", "ToJSON", "FilterByType", "FlattenMessages", "ContainsType", "CountByType"];

    private static object Call(string method, List<Exception> exceptions) => method switch
    {
        "ToHTML" => exceptions.ToHTML(),
        "ToJSON" => exceptions.ToJSON(),
        "FilterByType" => exceptions.FilterByType<IOException>(),
        "FlattenMessages" => exceptions.FlattenMessages(),
        "ContainsType" => exceptions.ContainsType<IOException>(),
        "CountByType" => exceptions.CountByType(),
        _ => throw new ArgumentOutOfRangeException(nameof(method)),
    };

    [Theory]
    [MemberData(nameof(Methods))]
    public void A_null_list_throws_ArgumentNullException_naming_it(string method)
    {
        var error = Assert.Throws<ArgumentNullException>(() => Call(method, null!));

        Assert.Equal("exceptions", error.ParamName);
    }

    [Theory]
    [MemberData(nameof(Methods))]
    public void A_null_exception_in_the_list_throws_ArgumentException_naming_the_list(string method)
    {
        var error = Assert.Throws<ArgumentException>(() => Call(method, [new IOException("one"), null!]));

        Assert.Equal("exceptions", error.ParamName);
    }

    [Fact]
    public async Task ToLogAsync_refuses_a_null_exception_in_the_list_before_writing()
    {
        using var folder = new TempFolder();
        string path = folder.File("errors.log");

        var error = await Assert.ThrowsAsync<ArgumentException>(() => new List<Exception> { new IOException("one"), null! }.ToLogAsync(path));

        Assert.Equal("exceptions", error.ParamName);
        Assert.False(File.Exists(path));
    }
}
