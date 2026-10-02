using System.Net;
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
        /// Converts a list of exceptions to an HTML fragment, a heading and a table holding each exception's type name,
        /// message and stack trace. The text is HTML-encoded, so it shows as written and cannot add markup; inner
        /// exceptions are not included.
        /// </summary>
        /// <param name="exceptions">The list of exceptions to convert to HTML.</param>
        /// <returns>The HTML fragment.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the 'exceptions' parameter is null.</exception>
        public static string ToHTML(this List<Exception> exceptions)
        {
            ArgumentNullException.ThrowIfNull(exceptions);

            var html = new StringBuilder("<br /><br /><h2>Exceptions:</h2>");

            html.Append("<table style='border-collapse: collapse; width: 100%;'>");

            foreach (var exception in exceptions)
            {
                html.Append("<tr><td colspan='2'><strong>Type:</strong></td></tr>");
                html.Append("<tr><td colspan='2' style='padding-left: 20px;'>").Append(WebUtility.HtmlEncode(exception.GetType().FullName)).Append("</td></tr>");
                html.Append("<tr><td colspan='2'><strong>Message:</strong></td></tr>");
                html.Append("<tr><td colspan='2' style='padding-left: 20px;'>").Append(WebUtility.HtmlEncode(exception.Message)).Append("</td></tr>");
                html.Append("<tr><td colspan='2'><strong>Stack Trace:</strong></td></tr>");
                html.Append("<tr><td colspan='2'><div style='padding-left: 20px;'><pre>").Append(WebUtility.HtmlEncode(exception.StackTrace)).Append("</pre></div></td></tr>");
                html.Append("<tr><td colspan='2' style='padding: 10px 0;'></td></tr>");
            }

            html.Append("</table>");

            return html.ToString();
        }

        /// <summary>
        /// Converts a list of exceptions to an indented JSON array holding one object per exception, with its
        /// <c>Message</c>, <c>StackTrace</c> and <c>TypeName</c>. Inner exceptions are not included.
        /// </summary>
        /// <param name="exceptions">The list of exceptions to convert to JSON.</param>
        /// <returns>The JSON text.</returns>
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
        /// Appends each exception's timestamp, type name, message and stack trace to a log file. An empty list writes nothing.
        /// </summary>
        /// <param name="exceptions">The list of exceptions to log.</param>
        /// <param name="logFilePath">The file to append to. Default: <c>Exceptions.log</c> in AppDomain.CurrentDomain.BaseDirectory.
        /// A relative path is resolved against the current directory. The file is created if missing; its folder is not.</param>
        /// <returns>A task that completes when the entries are written.</returns>
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
        /// Returns the exceptions in the list whose type is exactly <typeparamref name="T"/>. Exceptions of a type derived
        /// from <typeparamref name="T"/> are not included.
        /// </summary>
        /// <typeparam name="T">The type of exception to filter by.</typeparam>
        /// <param name="exceptions">The list of exceptions to filter.</param>
        /// <returns>A new list of the matching exceptions, in their original order.</returns>
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
        /// Checks whether the list holds an exception whose type is exactly <typeparamref name="T"/>. Exceptions of a type
        /// derived from <typeparamref name="T"/> do not count.
        /// </summary>
        /// <typeparam name="T">The type of exception to check for.</typeparam>
        /// <param name="exceptions">The list of exceptions to check.</param>
        /// <returns>True if the list holds an exception of exactly that type; otherwise, false.</returns>
        public static bool ContainsType<T>(this List<Exception> exceptions) where T : Exception
        {
            return exceptions.Any(e => e.GetType() == typeof(T));
        }

        /// <summary>
        /// Counts the occurrences of each exception type in the list.
        /// </summary>
        /// <param name="exceptions">The list of exceptions to count by type.</param>
        /// <returns>A dictionary from each exception type's full name to the number of exceptions of exactly that type.</returns>
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
