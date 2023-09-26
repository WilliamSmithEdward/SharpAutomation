using System.Text;
using System.Text.Json;

namespace SharpAutomation
{
    /// <summary>
    /// Provides extension methods for working with lists of exceptions.
    /// </summary>
    public static class ExceptionListExtensions
    {
        /// <summary>
        /// Converts a list of exceptions to an HTML representation for display purposes.
        /// </summary>
        /// <param name="exceptions">The list of exceptions to convert to HTML.</param>
        /// <returns>An HTML representation of the exceptions for display.</returns>
        public static string ToHTML(this List<Exception> exceptions)
        {
            var html = "<br /><br /><h2>Exceptions:</h2>";

            html += "<table style='border-collapse: collapse; width: 100%;'>";

            foreach (var exception in exceptions)
            {
                html += "<tr>";
                html += "<td colspan='2'><strong>Type:</strong></td></tr>";
                html += "<tr><td colspan='2' style='padding-left: 20px;'>" + exception.GetType().FullName + "</td></tr>";
                html += "<tr><td colspan='2'><strong>Message:</strong></td></tr>";
                html += "<tr><td colspan='2' style='padding-left: 20px;'>" + exception.Message + "</td></tr>";
                html += "<tr><td colspan='2'><strong>Stack Trace:</strong></td></tr>";
                html += "<tr><td colspan='2'><div style='padding-left: 20px;'><pre>" + exception.StackTrace + "</pre></div></td></tr>";
                html += "<tr><td colspan='2' style='padding: 10px 0;'></td></tr>";
                html += "</tr>";
            }

            html += "</table>";

            return html;
        }

        /// <summary>
        /// Converts a list of exceptions to a JSON representation.
        /// </summary>
        /// <param name="exceptions">The list of exceptions to convert to JSON.</param>
        /// <returns>A JSON representation of the exceptions.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the 'exceptions' parameter is null.</exception>
        public static string ToJSON(this List<Exception> exceptions)
        {
            if (exceptions == null)
                throw new ArgumentNullException(nameof(exceptions));

            var exceptionDtos = exceptions.Select(exception => new
            {
                exception.Message,
                exception.StackTrace,
                TypeName = exception.GetType().FullName
            });

            return JsonSerializer.Serialize(exceptionDtos, new JsonSerializerOptions
            {
                WriteIndented = true // Set this to true for pretty-printing the JSON
            });
        }

        /// <summary>
        /// Logs a list of exceptions to a file asynchronously.
        /// </summary>
        /// <param name="exceptions">The list of exceptions to log.</param>
        /// <param name="logFilePath">The path to the file where exceptions will be logged. Default: AppDomain.CurrentDomain.BaseDirectory.</param>
        /// <exception cref="ArgumentNullException">Thrown when the 'exceptions' parameter is null.</exception>
        public static async Task ToLogAsync(this List<Exception> exceptions, string logFilePath = "")
        {
            if (exceptions == null)
                throw new ArgumentNullException(nameof(exceptions));
            
            if (exceptions.Count == 0)
                return;

            if (string.IsNullOrEmpty(logFilePath)) logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Exceptions.log");

            var stringBuilder = new StringBuilder();

            foreach (var exception in exceptions)
            {
                stringBuilder.AppendLine($"Timestamp: {DateTime.Now}");
                stringBuilder.AppendLine($"Exception: {exception.GetType().FullName}");
                stringBuilder.AppendLine($"Message: {exception.Message}");
                stringBuilder.AppendLine($"StackTrace: {exception.StackTrace}");
                stringBuilder.AppendLine();
                stringBuilder.AppendLine("----------------------------------------------------------------------------");
                stringBuilder.AppendLine();
            }

            using var writer = new StreamWriter(logFilePath, true, Encoding.UTF8);
                
            await writer.WriteLineAsync(stringBuilder.ToString());
        }

        /// <summary>
        /// Filters exceptions in the list by a specified exception type.
        /// </summary>
        /// <typeparam name="T">The type of exception to filter by.</typeparam>
        /// <param name="exceptions">The list of exceptions to filter.</param>
        /// <returns>A list of exceptions filtered by the specified type.</returns>
        public static List<Exception> FilterByType<T>(this List<Exception> exceptions) where T : Exception
        {
            return exceptions.Where(e => e.GetType() == typeof(T)).ToList();
        }

        /// <summary>
        /// Flattens exception messages into a single string with newline separators.
        /// </summary>
        /// <param name="exceptions">The list of exceptions to flatten messages from.</param>
        /// <returns>A string containing flattened exception messages.</returns>
        public static string FlattenMessages(this List<Exception> exceptions)
        {
            return string.Join(Environment.NewLine, exceptions.Select(e => e.Message));
        }

        /// <summary>
        /// Checks if the list contains exceptions of a specified type.
        /// </summary>
        /// <typeparam name="T">The type of exception to check for.</typeparam>
        /// <param name="exceptions">The list of exceptions to check.</param>
        /// <returns>True if the list contains exceptions of the specified type; otherwise, false.</returns>
        public static bool ContainsType<T>(this List<Exception> exceptions) where T : Exception
        {
            return exceptions.Any(e => e.GetType() == typeof(T));
        }

        /// <summary>
        /// Counts the occurrences of each exception type in the list.
        /// </summary>
        /// <param name="exceptions">The list of exceptions to count by type.</param>
        /// <returns>A dictionary where keys are exception type names and values are their occurrence counts.</returns>
        public static Dictionary<string, int> CountByType(this List<Exception> exceptions)
        {
            var typeCounts = exceptions.GroupBy(x => x.GetType()).Select(x => new
            {
                TypeName = x.Key.FullName ?? string.Empty,
                Count = x.Count()
            }).ToDictionary(x => x.TypeName, x => x.Count);

            return typeCounts;
        }
    }
}
