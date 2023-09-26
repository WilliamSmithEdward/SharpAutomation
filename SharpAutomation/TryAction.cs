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
        /// <param name="waitBetweenTriesSeconds">The wait time in seconds between retries (default is 0).</param>
        /// <returns>
        /// A boolean value that represents whether the action was ultimately executed successfully.
        /// </returns>
        public static async Task<bool> RunAsync(Action action, List<Exception>? _exceptionList = null, int retries = 0, int waitBetweenTriesSeconds = 0)
        {
            int retryCount = 0;

            while (retryCount <= retries)
            {
                try
                {
                    await Task.Run(action);
                    return true;
                }
                catch (Exception ex)
                {
                    _exceptionList?.Add(ex);
                }

                await Task.Delay(waitBetweenTriesSeconds * 1000);
                retryCount++;
            }

            return false;
        }
    }
}