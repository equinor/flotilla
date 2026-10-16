namespace Api.Database.Models
{
    public enum AutoScheduleOccurrenceStatus
    {
        Scheduled,
        Skipped,
        Dispatched,
        Cancelled,
        Missed,
        Failed,
    }

    public class AutoScheduleOccurrence
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public required string MissionDefinitionId { get; set; }
        public DateTimeOffset ScheduledAt { get; set; }
        public AutoScheduleOccurrenceStatus Status { get; set; }
        public string? JobId { get; set; }
        public string? SchedulerInstance { get; set; }
    }
}
