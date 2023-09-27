using System;

namespace SharpAutomation
{
    /// <summary>
    /// Provides extension methods for working with exceptions.
    /// </summary>
    public static class ExceptionExtensions
    {
        /// <summary>
        /// Converts an exception to a JSON representation.
        /// </summary>
        /// <param name="exception">The exception to convert to JSON.</param>
        /// <returns>A JSON representation of the exception.</returns>
        public static string ToJSON(this Exception exception)
        {
            return new List<Exception>() { exception }.ToJSON();
        }

        /// <summary>
        /// Converts an exception to an HTML representation for display purposes.
        /// </summary>
        /// <param name="exception">The exception to convert to HTML.</param>
        /// <returns>An HTML representation of the exceptions for display.</returns>
        public static string ToHTML(this Exception exception)
        {
            return new List<Exception>() { exception }.ToHTML();
        }

        /// <summary>
        /// Logs an exception to a file asynchronously.
        /// </summary>
        /// <param name="exception">The exception to log.</param>
        /// <param name="logFilePath">The path to the file where exception will be logged. Default: AppDomain.CurrentDomain.BaseDirectory.</param>
        public static async Task ToLogAsync(this Exception exception, string logFilePath = "")
        {
            await new List<Exception>() { exception }.ToLogAsync(logFilePath);
        }
    }
}
