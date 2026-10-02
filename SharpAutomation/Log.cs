using System.Globalization;
using System.Text;

namespace SharpAutomation
{
    /// <summary>
    /// Provides static methods for logging entries.
    /// </summary>
    public static class Log
    {
        /// <summary>
        /// Appends a timestamp and an entry to a log file, and returns once they are written. Despite its name the
        /// method writes synchronously and returns <c>void</c>; an exception from the write, such as
        /// <see cref="DirectoryNotFoundException"/> for a missing folder, is thrown to the caller. To await the write,
        /// use <see cref="AppendEntryAsync"/>.
        /// </summary>
        /// <param name="entry">The entry to be logged. A line break in it is followed by two spaces in the file.</param>
        /// <param name="logFilePath">The file to append to. Default: a file named <c>yyyyMMdd_HHmmss.log</c> from the local
        /// time, in AppDomain.CurrentDomain.BaseDirectory. A relative path is resolved against the current directory.</param>
        /// <exception cref="IOException">Thrown when the file cannot be written, or another process holds it for more than
        /// about two seconds.</exception>
        public static void WriteEntryAsync(string entry, string logFilePath = "")
        {
            LogFile.Append(PathOrDefault(logFilePath), Format(entry));
        }

        /// <summary>
        /// Appends a timestamp and an entry to a log file.
        /// </summary>
        /// <param name="entry">The entry to be logged. A line break in it is followed by two spaces in the file.</param>
        /// <param name="logFilePath">The file to append to. Default: a file named <c>yyyyMMdd_HHmmss.log</c> from the local
        /// time, in AppDomain.CurrentDomain.BaseDirectory. A relative path is resolved against the current directory.</param>
        /// <param name="cancellationToken">Cancels the wait for the file.</param>
        /// <returns>A task that completes when the entry is written.</returns>
        /// <exception cref="IOException">Thrown when the file cannot be written, or another process holds it for more than
        /// about two seconds.</exception>
        public static Task AppendEntryAsync(string entry, string logFilePath = "", CancellationToken cancellationToken = default)
        {
            return LogFile.AppendAsync(PathOrDefault(logFilePath), Format(entry), cancellationToken);
        }

        /// <summary>The file a write without a path goes to: one per second, named from the local time.</summary>
        internal static string DefaultLogFilePath(DateTime now) =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture) + ".log");

        private static string PathOrDefault(string logFilePath) =>
            string.IsNullOrEmpty(logFilePath) ? DefaultLogFilePath(DateTime.Now) : logFilePath;

        private static string Format(string entry)
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.AppendLine(LogFile.Field("Timestamp", DateTime.Now.ToString()));
            stringBuilder.AppendLine(LogFile.Field("Entry", entry));
            stringBuilder.AppendLine();
            stringBuilder.AppendLine(LogFile.Separator);
            stringBuilder.AppendLine();

            return stringBuilder.ToString();
        }
    }
}
