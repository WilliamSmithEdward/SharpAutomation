using System.Text;

namespace SharpAutomation
{
    /// <summary>
    /// Provides static methods for logging entries.
    /// </summary>
    public static class Log
    {
        /// <summary>
        /// Appends a timestamp and an entry to a log file. The method returns <c>void</c>: it cannot be awaited, it may
        /// return before the write finishes, and an exception from the write is raised on the thread pool, not to the caller.
        /// </summary>
        /// <param name="entry">The entry to be logged.</param>
        /// <param name="logFilePath">The file to append to. Default: a file named <c>yyyyMMdd_hhmmss.log</c> from the local
        /// time, in AppDomain.CurrentDomain.BaseDirectory. A relative path is resolved against the current directory.</param>
        public static async void WriteEntryAsync(string entry, string logFilePath = "")
        {
            if (string.IsNullOrEmpty(logFilePath)) logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DateTime.Now.ToString("yyyyMMdd_hhmmss") + ".log");

            var stringBuilder = new StringBuilder();

            stringBuilder.AppendLine($"Timestamp: {DateTime.Now}");
            stringBuilder.AppendLine($"Entry: {entry}");
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("----------------------------------------------------------------------------");
            stringBuilder.AppendLine();

            using var writer = new StreamWriter(logFilePath, true, Encoding.UTF8);

            await writer.WriteLineAsync(stringBuilder.ToString());
        }
    }
}
