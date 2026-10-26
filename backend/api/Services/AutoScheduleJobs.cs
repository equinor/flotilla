using System.Collections.Concurrent;

namespace Api.Services;

// In-memory Hangfire and this registration cache have the same process lifetime.
// Neither contains durable scheduling decisions.
public sealed class AutoScheduleJobs(TimeProvider timeProvider)
{
    public DateTimeOffset StartedAt { get; } = timeProvider.GetUtcNow();
    private readonly ConcurrentDictionary<string, DateTimeOffset> _registered = new();

    public bool Contains(string scheduleKey) => _registered.ContainsKey(scheduleKey);

    public void Register(string scheduleKey, DateTimeOffset scheduledAt) =>
        _registered[scheduleKey] = scheduledAt;

    public void Prune(DateTimeOffset now)
    {
        foreach (var entry in _registered.Where(entry => entry.Value <= now))
            _registered.TryRemove(entry.Key, out _);
    }
}
