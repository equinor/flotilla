using System.Collections.Concurrent;

namespace Api.Services;

// In-memory Hangfire and this registration cache have the same process lifetime.
// Neither contains durable scheduling decisions.
public sealed class AutoScheduleJobs(TimeProvider timeProvider)
{
    public DateTimeOffset StartedAt { get; } = timeProvider.GetUtcNow();
    private readonly ConcurrentDictionary<
        (string MissionId, DateTimeOffset ScheduledAt),
        byte
    > _registered = new();

    public bool Contains(string missionId, DateTimeOffset scheduledAt) =>
        _registered.ContainsKey((missionId, scheduledAt));

    public void Register(string missionId, DateTimeOffset scheduledAt) =>
        _registered[(missionId, scheduledAt)] = 0;

    public void Prune(DateTimeOffset now)
    {
        foreach (var entry in _registered.Where(entry => entry.Key.ScheduledAt <= now))
            _registered.TryRemove(entry.Key, out _);
    }
}
