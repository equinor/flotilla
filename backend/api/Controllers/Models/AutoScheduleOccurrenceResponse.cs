namespace Api.Controllers.Models;

// An occurrence is derived from recurrence, not a persisted lifecycle record.
public record AutoScheduleOccurrenceResponse(
    string Id,
    DateTimeOffset ScheduledAt,
    AutoScheduleOccurrenceStatus Status,
    MissionDefinitionResponse Mission
);

public enum AutoScheduleOccurrenceStatus
{
    Scheduled,
    Skipped,
}
