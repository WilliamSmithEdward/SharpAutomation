namespace SharpAutomation.Tests;

/// <summary>
/// A folder of its own under the system temp folder for one test, deleted afterwards. Every file a
/// test writes, log files included, goes here.
/// </summary>
internal sealed class TempFolder : IDisposable
{
    public TempFolder()
    {
        Root = Path.Combine(Path.GetTempPath(), "sharpautomation-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Root);
    }

    public string Root { get; }

    public string File(string name) => Path.Combine(Root, name);

    public void Dispose()
    {
        try { Directory.Delete(Root, recursive: true); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}

/// <summary>
/// Exceptions with a stack trace, as the library meets them: thrown and caught.
/// </summary>
internal static class Thrown
{
    public static Exception Exception(Exception exception)
    {
        try { throw exception; }
        catch (Exception caught) { return caught; }
    }

    /// <summary>The exception <paramref name="action"/> throws, with every frame it passed through.</summary>
    public static Exception Exception(Action action)
    {
        try { action(); }
        catch (Exception caught) { return caught; }
        throw new InvalidOperationException("the action did not throw");
    }
}
