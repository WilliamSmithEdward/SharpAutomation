using System;

namespace SharpAutomation
{
    /// <summary>
    /// Provides extension methods for working with exceptions.
    /// </summary>
    public static class ExceptionExtensions
    {
        /// <summary>
        /// Converts an exception to an HTML fragment, a heading and a table holding the exception's type name, message and
        /// stack trace. The text is HTML-encoded, so it shows as written and cannot add markup; the inner exception is
        /// not included.
        /// </summary>
        /// <param name="exception">The exception to convert to HTML.</param>
        /// <returns>The HTML fragment.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the 'exception' parameter is null.</exception>
        public static string ToHTML(this Exception exception)
        {
            ArgumentNullException.ThrowIfNull(exception);

            return new List<Exception>() { exception }.ToHTML();
        }

        /// <summary>
        /// Converts an exception to an indented JSON array holding one object with its <c>Message</c>, <c>StackTrace</c>
        /// and <c>TypeName</c>. The inner exception is not included.
        /// </summary>
        /// <param name="exception">The exception to convert to JSON.</param>
        /// <returns>The JSON text.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the 'exception' parameter is null.</exception>
        public static string ToJSON(this Exception exception)
        {
            ArgumentNullException.ThrowIfNull(exception);

            return new List<Exception>() { exception }.ToJSON();
        }

        /// <summary>
        /// Appends the exception's timestamp, type name, message and stack trace to a log file.
        /// </summary>
        /// <param name="exception">The exception to log.</param>
        /// <param name="logFilePath">The file to append to. Default: <c>Exceptions.log</c> in AppDomain.CurrentDomain.BaseDirectory.
        /// A relative path is resolved against the current directory. The file is created if missing; its folder is not.</param>
        /// <returns>A task that completes when the entry is written.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the 'exception' parameter is null.</exception>
        public static async Task ToLogAsync(this Exception exception, string logFilePath = "")
        {
            ArgumentNullException.ThrowIfNull(exception);

            await new List<Exception>() { exception }.ToLogAsync(logFilePath);
        }
    }
}
