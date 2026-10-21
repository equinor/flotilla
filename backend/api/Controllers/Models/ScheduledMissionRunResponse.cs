using Api.Database.Models;

namespace Api.Controllers.Models
{
    public record ScheduledMissionRunResponse(
        string Id,
        DateTimeOffset ScheduledAt,
        MissionStatus Status,
        MissionDefinitionResponse Mission
    );
}
