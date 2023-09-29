using System.Text;

namespace SharpAutomation
{
    /// <summary>
    /// Provides static methods for logging entries.
    /// </summary>
    public static class Log
    {
        /// <summary>
        /// Writes an entry to a log asynchronously.
        /// </summary>
        /// <param name="entry">The entry to be logged.</param>
        /// <param name="logFilePath">The file path for the log. Default: AppDomain.CurrentDomain.BaseDirectory.</param>
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
