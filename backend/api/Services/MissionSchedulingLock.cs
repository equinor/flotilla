using System.Collections.Concurrent;

namespace Api.Services
{
    /// <summary>
    /// Provides a per-robot asynchronous lock used to serialize mission-start
    /// attempts for a given robot.
    ///
    /// Multiple independent callers (the scheduling controller, the auto scheduler
    /// and the MQTT mission event handler) may concurrently attempt to start the
    /// next queued mission run for the same robot. Without serialization two
    /// threads can pick and start the exact same mission run at the same time,
    /// which both duplicates the ISAR call and lets a losing thread mark an
    /// already-running mission as failed.
    /// </summary>
    public interface IMissionSchedulingLock
    {
        /// <summary>
        /// Acquires the scheduling lock for the given robot. The returned
        /// <see cref="IDisposable"/> releases the lock when disposed.
        /// </summary>
        Task<IDisposable> LockAsync(string robotId, CancellationToken cancellationToken = default);
    }

    public class MissionSchedulingLock : IMissionSchedulingLock
    {
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

        public async Task<IDisposable> LockAsync(
            string robotId,
            CancellationToken cancellationToken = default
        )
        {
            var semaphore = _locks.GetOrAdd(robotId, _ => new SemaphoreSlim(1, 1));
            await semaphore.WaitAsync(cancellationToken);
            return new Releaser(semaphore);
        }

        private sealed class Releaser(SemaphoreSlim semaphore) : IDisposable
        {
            private SemaphoreSlim? _semaphore = semaphore;

            public void Dispose()
            {
                // Guard against double dispose releasing the semaphore twice.
                var semaphore = Interlocked.Exchange(ref _semaphore, null);
                semaphore?.Release();
            }
        }
    }
}
