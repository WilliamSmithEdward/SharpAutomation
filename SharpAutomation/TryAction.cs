namespace SharpAutomation
{
    public static class TryAction
    {
        public static async Task<Exception?> RunAsync(Action action)
        {
            try
            {
                await Task.Run(action);
                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }
    }
}