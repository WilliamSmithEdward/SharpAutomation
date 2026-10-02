namespace SharpAutomation
{
    /// <summary>
    /// Provides a utility to safely run an action and capture any exceptions that may occur.
    /// </summary>
    public static class TryAction
    {
        /// <summary>
        /// Calls an action up to <paramref name="retries"/> + 1 times, until one call returns without throwing, and
        /// catches every exception it throws.
        /// </summary>
        /// <param name="action">The action to be executed.</param>
        /// <param name="retries">The number of times to call the action again after a call that throws (default is 0).</param>
        /// <param name="waitBetweenTriesSeconds">The seconds to block the thread after each call that throws, the last one
        /// included (default is 0).</param>
        /// <param name="_exceptionList">An optional list that receives each exception the action throws. Without one,
        /// the exceptions are discarded.</param>
        /// <returns>
        /// A boolean value that represents whether the action was ultimately executed successfully.
        /// </returns>
        public static bool Run(Action action, int retries = 0, int waitBetweenTriesSeconds = 0, List<Exception>? _exceptionList = null)
        {
            int retryCount = 0;

            while (retryCount <= retries)
            {
                try
                {
                    action();
                    return true;
                }
                catch (Exception ex)
                {
                    _exceptionList?.Add(ex);
                }

                Thread.Sleep(waitBetweenTriesSeconds * 1000);
                retryCount++;
            }

            return false;
        }

        /// <summary>
        /// Calls an action on the thread pool up to <paramref name="retries"/> + 1 times, until one call returns without
        /// throwing, and catches every exception it throws. Pass synchronous code only: an <c>async</c> lambda becomes
        /// <c>async void</c>, is not awaited, and its exceptions are not caught.
        /// </summary>
        /// <param name="action">The action to be executed.</param>
        /// <param name="retries">The number of times to call the action again after a call that throws (default is 0).</param>
        /// <param name="waitBetweenTriesSeconds">The seconds to wait after each call that throws, the last one included
        /// (default is 0).</param>
        /// <param name="_exceptionList">An optional list that receives each exception the action throws. Without one,
        /// the exceptions are discarded.</param>
        /// <returns>
        /// A boolean value that represents whether the action was ultimately executed successfully.
        /// </returns>
        public static async Task<bool> RunAsync(Action action, int retries = 0, int waitBetweenTriesSeconds = 0, List<Exception>? _exceptionList = null)
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