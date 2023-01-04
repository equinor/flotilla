namespace Api.Utilities
{
    public static class MyExtensions
    {
        public static void Reset(this System.Timers.Timer timer)
        {
            timer.Stop();
            timer.Start();
        }
    }
}
