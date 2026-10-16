using Api.Database.Models;

namespace Api.Controllers.Models
{
    public record AutoScheduleOccurrenceResponse(
        string Id,
        DateTimeOffset ScheduledAt,
        AutoScheduleOccurrenceStatus Status,
        MissionDefinitionResponse Mission
    );
}
