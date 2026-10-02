using System.Text;

namespace SharpAutomation
{
    /// <summary>
    /// Appends text to log files for <see cref="Log"/> and the exception extensions, and formats their fields.
    /// </summary>
    internal static class LogFile
    {
        internal const string Separator = "----------------------------------------------------------------------------";

        // Writes to one file are taken in turn within the process. The file's full path picks one of these gates;
        // two files that share a gate only wait for each other. A fixed set, so a process that writes a new file
        // every second, as Log does by default, does not collect one gate per file.
        private static readonly SemaphoreSlim[] Gates = Enumerable.Range(0, 64).Select(_ => new SemaphoreSlim(1, 1)).ToArray();

        private static readonly StringComparer PathComparer =
            OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

        // Another process may hold the file for a moment: retry for up to about two seconds before giving up.
        private const int Attempts = 40;
        private static readonly TimeSpan Pause = TimeSpan.FromMilliseconds(50);

        /// <summary>
        /// "Name: value", with each line break in the value followed by two spaces, so that only the library starts a
        /// line of the log at its first column and logged text cannot pass for a field, a separator or an entry.
        /// </summary>
        internal static string Field(string name, string? value)
        {
            var text = (value ?? "").Replace("\r\n", "\n").Replace('\r', '\n');
            return name + ": " + text.Replace("\n", Environment.NewLine + "  ");
        }

        internal static void Append(string path, string text)
        {
            var fullPath = Path.GetFullPath(path);
            var gate = GateFor(fullPath);
            gate.Wait();
            try
            {
                for (int attempt = 1; ; attempt++)
                {
                    try
                    {
                        using var writer = Open(fullPath);
                        writer.WriteLine(text);
                        return;
                    }
                    catch (IOException e) when (IsHeldByAnother(e) && attempt < Attempts)
                    {
                        Thread.Sleep(Pause);
                    }
                }
            }
            finally
            {
                gate.Release();
            }
        }

        internal static async Task AppendAsync(string path, string text, CancellationToken cancellationToken)
        {
            var fullPath = Path.GetFullPath(path);
            var gate = GateFor(fullPath);
            await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                for (int attempt = 1; ; attempt++)
                {
                    try
                    {
                        await using var writer = Open(fullPath);
                        await writer.WriteLineAsync(text.AsMemory(), cancellationToken).ConfigureAwait(false);
                        return;
                    }
                    catch (IOException e) when (IsHeldByAnother(e) && attempt < Attempts)
                    {
                        await Task.Delay(Pause, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                gate.Release();
            }
        }

        private static SemaphoreSlim GateFor(string fullPath) =>
            Gates[(PathComparer.GetHashCode(fullPath) & int.MaxValue) % Gates.Length];

        // Shared with readers, as before, so a log can be watched while it is written.
        private static StreamWriter Open(string fullPath) =>
            new(new FileStream(fullPath, FileMode.Append, FileAccess.Write, FileShare.Read), Encoding.UTF8);

        // A sharing violation is a plain IOException; a missing folder, a path too long and the like are subclasses,
        // and waiting does not cure them.
        private static bool IsHeldByAnother(IOException e) => e.GetType() == typeof(IOException);
    }
}
