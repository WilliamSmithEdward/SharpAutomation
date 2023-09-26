namespace SharpAutomation
{
    /// <summary>
    /// Provides a utility to safely run an action and capture any exceptions that may occur.
    /// </summary>
    public static class TryAction
    {
        /// <summary>
        /// Executes an action asynchronously and captures any exceptions that may occur.
        /// </summary>
        /// <param name="action">The action to be executed.</param>
        /// <param name="_exceptionList">An optional list to which exceptions will be added in case of errors.</param>
        /// <param name="retries">The number of times to retry the action in case of exceptions (default is 0).</param>
        /// <param name="waitSeconds">The wait time in seconds between retries (default is 0).</param>
        /// <returns>
        /// A list of exceptions that occurred during the execution of the action.
        /// </returns>
        public static async Task<List<Exception>> RunAsync(Action action, List<Exception>? _exceptionList = null, int retries = 0, int waitSeconds = 0)
        {
            int retryCount = 0;
            var exceptionList = new List<Exception>();

            while (retryCount <= retries)
            {
                try
                {
                    await Task.Run(action);
                    break;
                }
                catch (Exception ex)
                {
                    _exceptionList?.Add(ex);
                    exceptionList.Add(ex);
                }

                await Task.Delay(waitSeconds * 1000);
                retryCount++;
            }

            return exceptionList;
        }
    }
}