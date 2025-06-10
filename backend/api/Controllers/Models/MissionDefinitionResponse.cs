using System.Text.Json.Serialization;
using Api.Database.Models;
using Api.Services;

namespace Api.Controllers.Models
{
    public class MissionDefinitionResponse
    {
        public string Id { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string InstallationCode { get; set; } = string.Empty;

        public string? Comment { get; set; }

        public TimeSpan? InspectionFrequency { get; set; }

        public AutoScheduleFrequency? AutoScheduleFrequency { get; set; }

        public virtual MissionRun? LastSuccessfulRun { get; set; }

        public InspectionAreaResponse? InspectionArea { get; set; }

        public bool IsDeprecated { get; set; }

        public string SourceId { get; set; } = string.Empty;

        public MapMetadata? Map { get; set; }

        [JsonConstructor]
        public MissionDefinitionResponse() { }

        public MissionDefinitionResponse(MissionDefinition missionDefinition)
        {
            Id = missionDefinition.Id;
            Name = missionDefinition.Name;
            InstallationCode = missionDefinition.InstallationCode;
            Comment = missionDefinition.Comment;
            InspectionFrequency = missionDefinition.InspectionFrequency;
            AutoScheduleFrequency =
                (
                    missionDefinition.AutoScheduleFrequency is not null
                    && missionDefinition.AutoScheduleFrequency.HasValidValue()
                )
                    ? missionDefinition.AutoScheduleFrequency
                    : null;
            InspectionArea =
                missionDefinition.InspectionArea != null
                    ? new InspectionAreaResponse(missionDefinition.InspectionArea)
                    : null;
            LastSuccessfulRun = missionDefinition.LastSuccessfulRun;
            IsDeprecated = missionDefinition.IsDeprecated;
            SourceId = missionDefinition.Source.SourceId;
            Map = missionDefinition.Map;
        }
    }

    public class MissionDefinitionWithTasksResponse(
        IMissionDefinitionTaskService service,
        MissionDefinition missionDefinition
    )
    {
        public string Id { get; } = missionDefinition.Id;

        public List<MissionTask> Tasks { get; } =
            service.GetTasksFromSource(missionDefinition.Source).Result!;

        public string Name { get; } = missionDefinition.Name;

        public string InstallationCode { get; } = missionDefinition.InstallationCode;

        public string? Comment { get; } = missionDefinition.Comment;

        public TimeSpan? InspectionFrequency { get; } = missionDefinition.InspectionFrequency;

        public AutoScheduleFrequency? AutoScheduleFrequency { get; } =
            (
                missionDefinition.AutoScheduleFrequency is not null
                && missionDefinition.AutoScheduleFrequency.HasValidValue()
            )
                ? missionDefinition.AutoScheduleFrequency
                : null;

        public virtual MissionRun? LastSuccessfulRun { get; } = missionDefinition.LastSuccessfulRun;

        public InspectionArea? InspectionArea { get; } = missionDefinition.InspectionArea;

        public bool IsDeprecated { get; } = missionDefinition.IsDeprecated;

        public MapMetadata? Map { get; } = missionDefinition.Map;
    }
}
