namespace SharpAutomation
{
    /// <summary>
    /// Provides a utility to safely run an action and capture any exceptions that may occur.
    /// </summary>
    public static class TryAction
    {
        /// <summary>The longest wait between attempts, in seconds: about 24.8 days, the most <see cref="Thread.Sleep(int)"/> takes.</summary>
        private const int MaxWaitSeconds = int.MaxValue / 1000;

        /// <summary>
        /// Calls an action up to <paramref name="retries"/> + 1 times, until one call returns without throwing, and
        /// catches every exception it throws.
        /// </summary>
        /// <param name="action">The action to be executed.</param>
        /// <param name="retries">The number of times to call the action again after a call that throws (default is 0).</param>
        /// <param name="waitBetweenTriesSeconds">The seconds to block the thread after a call that throws, before the next
        /// call (default is 0). There is no wait after the last call.</param>
        /// <param name="_exceptionList">An optional list that receives each exception the action throws. Without one,
        /// the exceptions are discarded.</param>
        /// <returns>
        /// A boolean value that represents whether the action was ultimately executed successfully.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="action"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown, before the action is called, when <paramref name="retries"/>
        /// is negative or <paramref name="waitBetweenTriesSeconds"/> is negative or above 2,147,483.</exception>
        public static bool Run(Action action, int retries = 0, int waitBetweenTriesSeconds = 0, List<Exception>? _exceptionList = null)
        {
            ArgumentNullException.ThrowIfNull(action);
            var wait = CheckedWait(retries, waitBetweenTriesSeconds);

            for (int attempt = 0; ; attempt++)
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

                if (attempt == retries) return false;

                Thread.Sleep(wait);
            }
        }

        /// <summary>
        /// Calls an action on the thread pool up to <paramref name="retries"/> + 1 times, until one call returns without
        /// throwing, and catches every exception it throws. Pass synchronous code here. An <c>async</c> lambda, or one
        /// that returns a task, goes to the overload that takes a <see cref="Func{Task}"/>, which awaits it.
        /// </summary>
        /// <param name="action">The action to be executed.</param>
        /// <param name="retries">The number of times to call the action again after a call that throws (default is 0).</param>
        /// <param name="waitBetweenTriesSeconds">The seconds to wait after a call that throws, before the next call
        /// (default is 0). There is no wait after the last call.</param>
        /// <param name="_exceptionList">An optional list that receives each exception the action throws. Without one,
        /// the exceptions are discarded.</param>
        /// <returns>
        /// A boolean value that represents whether the action was ultimately executed successfully.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="action"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown, before the action is called, when <paramref name="retries"/>
        /// is negative or <paramref name="waitBetweenTriesSeconds"/> is negative or above 2,147,483.</exception>
        public static Task<bool> RunAsync(Action action, int retries = 0, int waitBetweenTriesSeconds = 0, List<Exception>? _exceptionList = null)
        {
            ArgumentNullException.ThrowIfNull(action);

            return RunAsync(() => Task.Run(action), retries, waitBetweenTriesSeconds, _exceptionList, CancellationToken.None);
        }

        /// <summary>
        /// Calls an asynchronous action, such as an <c>async</c> lambda, up to <paramref name="retries"/> + 1 times, until
        /// the task of one call completes without an exception, and catches every exception those tasks end with.
        /// </summary>
        /// <param name="action">The action to be executed. Each call's task is awaited before the next call.</param>
        /// <param name="retries">The number of times to call the action again after a call that fails (default is 0).</param>
        /// <param name="waitBetweenTriesSeconds">The seconds to wait after a call that fails, before the next call
        /// (default is 0). There is no wait after the last call.</param>
        /// <param name="_exceptionList">An optional list that receives each exception the action ends with. Without one,
        /// the exceptions are discarded.</param>
        /// <param name="cancellationToken">Stops the retries: once it is cancelled, no further call is made, a wait ends
        /// early, and the returned task is cancelled. An <see cref="OperationCanceledException"/> for this token is not
        /// collected as a failure.</param>
        /// <returns>
        /// A boolean value that represents whether the action was ultimately executed successfully.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="action"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown, before the action is called, when <paramref name="retries"/>
        /// is negative or <paramref name="waitBetweenTriesSeconds"/> is negative or above 2,147,483.</exception>
        /// <exception cref="OperationCanceledException">Thrown when <paramref name="cancellationToken"/> is cancelled.</exception>
        public static async Task<bool> RunAsync(Func<Task> action, int retries = 0, int waitBetweenTriesSeconds = 0, List<Exception>? _exceptionList = null, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(action);
            var wait = CheckedWait(retries, waitBetweenTriesSeconds);

            for (int attempt = 0; ; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    await action().ConfigureAwait(false);
                    return true;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _exceptionList?.Add(ex);
                }

                if (attempt == retries) return false;

                await Task.Delay(wait, cancellationToken).ConfigureAwait(false);
            }
        }

        private static TimeSpan CheckedWait(int retries, int waitBetweenTriesSeconds)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(retries);
            ArgumentOutOfRangeException.ThrowIfNegative(waitBetweenTriesSeconds);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(waitBetweenTriesSeconds, MaxWaitSeconds);

            return TimeSpan.FromSeconds(waitBetweenTriesSeconds);
        }
    }
}
