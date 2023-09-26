using System.Text;

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
        /// Logs a list of exceptions to a file asynchronously.
        /// </summary>
        /// <param name="exceptions">The list of exceptions to log.</param>
        /// <param name="filePath">The path to the file where exceptions will be logged. Default: AppDomain.CurrentDomain.BaseDirectory.</param>
        /// <exception cref="ArgumentNullException">Thrown when the 'exceptions' parameter is null.</exception>
        public static async Task LogToAsync(this List<Exception> exceptions, string filePath = "")
        {
            if (exceptions == null)
                throw new ArgumentNullException(nameof(exceptions));
            
            if (exceptions.Count == 0)
                return;

            if (string.IsNullOrEmpty(filePath)) filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Exceptions.log");

            var stringBuilder = new StringBuilder();

            foreach (var exception in exceptions)
            {
                stringBuilder.AppendLine($"Timestamp: {DateTime.Now}");
                stringBuilder.AppendLine($"Exception: {exception.GetType().FullName}");
                stringBuilder.AppendLine($"Message: {exception.Message}");
                stringBuilder.AppendLine($"StackTrace: {exception.StackTrace}");
                stringBuilder.AppendLine();
            }

            using var writer = new StreamWriter(filePath, true, Encoding.UTF8);
                
            await writer.WriteLineAsync(stringBuilder.ToString());
        }
    }
}
