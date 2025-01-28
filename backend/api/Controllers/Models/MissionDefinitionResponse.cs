#nullable enable
using System.Text.Json.Serialization;
using Api.Database.Models;
using Api.Services;

namespace Api.Controllers.Models
{
    public class MissionDefinitionResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("installationCode")]
        public string InstallationCode { get; set; } = string.Empty;

        [JsonPropertyName("comment")]
        public string? Comment { get; set; }

        [JsonPropertyName("inspectionFrequency")]
        public TimeSpan? InspectionFrequency { get; set; }

        [JsonPropertyName("lastSuccessfulRun")]
        public virtual MissionRun? LastSuccessfulRun { get; set; }

        [JsonPropertyName("inspectionGroups")]
        public List<InspectionGroupResponse> InspectionGroups { get; set; } = [];

        [JsonPropertyName("isDeprecated")]
        public bool IsDeprecated { get; set; }

        [JsonPropertyName("sourceId")]
        public string SourceId { get; set; } = string.Empty;

        [JsonPropertyName("map")]
        public MapMetadata? Map { get; set; }

        [JsonConstructor]
        public MissionDefinitionResponse() { }

        public MissionDefinitionResponse(MissionDefinition missionDefinition)
        {
            Id = missionDefinition.Id;
            Name = missionDefinition.Name;
            InstallationCode = missionDefinition.Installation.InstallationCode;
            Comment = missionDefinition.Comment;
            InspectionFrequency = missionDefinition.InspectionFrequency;
            InspectionGroups =
                missionDefinition.InspectionGroups != null
                    ? (List<InspectionGroupResponse>)missionDefinition.InspectionGroups
                    : [];
            LastSuccessfulRun = missionDefinition.LastSuccessfulRun;
            IsDeprecated = missionDefinition.IsDeprecated;
            SourceId = missionDefinition.Source.SourceId;
            Map = missionDefinition.Map;
        }
    }

    public class MissionDefinitionWithTasksResponse(
        IMissionDefinitionService service,
        MissionDefinition missionDefinition
    )
    {
        [JsonPropertyName("id")]
        public string Id { get; } = missionDefinition.Id;

        [JsonPropertyName("tasks")]
        public List<MissionTask> Tasks { get; } =
            service.GetTasksFromSource(missionDefinition.Source).Result!;

        [JsonPropertyName("name")]
        public string Name { get; } = missionDefinition.Name;

        [JsonPropertyName("installationCode")]
        public string InstallationCode { get; } = missionDefinition.Installation.InstallationCode;

        [JsonPropertyName("comment")]
        public string? Comment { get; } = missionDefinition.Comment;

        [JsonPropertyName("inspectionFrequency")]
        public TimeSpan? InspectionFrequency { get; } = missionDefinition.InspectionFrequency;

        [JsonPropertyName("lastSuccessfulRun")]
        public virtual MissionRun? LastSuccessfulRun { get; } = missionDefinition.LastSuccessfulRun;

        [JsonPropertyName("inspectionGroups")]
        public List<InspectionGroup> InspectionGroups { get; } =
            (List<InspectionGroup>)missionDefinition.InspectionGroups;

        [JsonPropertyName("isDeprecated")]
        public bool IsDeprecated { get; } = missionDefinition.IsDeprecated;

        [JsonPropertyName("map")]
        public MapMetadata? Map { get; } = missionDefinition.Map;
    }
}
