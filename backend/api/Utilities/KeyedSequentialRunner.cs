namespace Api.Utilities
{
    /// <summary>
    ///     Runs work items with the same key one at a time, in the order they were enqueued.
    ///     Work items with different keys run concurrently.
    /// </summary>
    public class KeyedSequentialRunner(ILogger logger)
    {
        private readonly Dictionary<string, Task> _tails = [];
        private readonly Lock _lock = new();
        private int _queued;

        public Task Enqueue(string key, Func<Task> work)
        {
            Task previous;
            var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (_lock)
            {
                previous = _tails.GetValueOrDefault(key, Task.CompletedTask);
                _tails[key] = done.Task;
            }
            var queued = Interlocked.Increment(ref _queued);

            if (!previous.IsCompleted)
            {
                logger.LogDebug(
                    "Work item for key {Key} is waiting for earlier work, {Queued} work items queued in total",
                    key,
                    queued
                );
            }
            return RunAfter(previous, key, work, done);
        }

        private async Task RunAfter(
            Task previous,
            string key,
            Func<Task> work,
            TaskCompletionSource done
        )
        {
            await previous;
            try
            {
                await work();
            }
            catch (Exception e)
            {
                logger.LogError(e, "Sequential work item for key {Key} failed", key);
            }
            finally
            {
                Interlocked.Decrement(ref _queued);
                done.SetResult();
            }
        }
    }
}
