#nullable enable
using System.Text.Json.Serialization;
using Api.Database.Models;
using Api.Services;

namespace Api.Controllers.Models
{
    public class CondensedMissionDefinitionResponse
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

        [JsonPropertyName("area")]
        public AreaResponse? Area { get; set; }

        [JsonPropertyName("isDeprecated")]
        public bool IsDeprecated { get; set; }

        [JsonPropertyName("sourceType")]
        public MissionSourceType SourceType { get; set; }

        [JsonConstructor]
        public CondensedMissionDefinitionResponse() { }

        public CondensedMissionDefinitionResponse(MissionDefinition missionDefinition)
        {
            Id = missionDefinition.Id;
            Name = missionDefinition.Name;
            InstallationCode = missionDefinition.InstallationCode;
            Comment = missionDefinition.Comment;
            InspectionFrequency = missionDefinition.InspectionFrequency;
            Area = missionDefinition.Area != null ? new AreaResponse(missionDefinition.Area) : null;
            LastSuccessfulRun = missionDefinition.LastSuccessfulRun;
            IsDeprecated = missionDefinition.IsDeprecated;
            SourceType = missionDefinition.Source.Type;
        }
    }

    public class MissionDefinitionResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("tasks")]
        public List<MissionTask> Tasks { get; set; } = new();

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("installationCode")]
        public string InstallationCode { get; set; }

        [JsonPropertyName("comment")]
        public string? Comment { get; set; }

        [JsonPropertyName("inspectionFrequency")]
        public TimeSpan? InspectionFrequency { get; set; }

        [JsonPropertyName("lastSuccessfulRun")]
        public virtual MissionRun? LastSuccessfulRun { get; set; }

        [JsonPropertyName("area")]
        public Area? Area { get; set; }

        [JsonPropertyName("isDeprecated")]
        public bool IsDeprecated { get; set; }

        [JsonPropertyName("sourceType")]
        public MissionSourceType SourceType { get; set; }

        public MissionDefinitionResponse(IMissionDefinitionService service, MissionDefinition missionDefinition)
        {
            Id = missionDefinition.Id;
            Name = missionDefinition.Name;
            InstallationCode = missionDefinition.InstallationCode;
            Comment = missionDefinition.Comment;
            InspectionFrequency = missionDefinition.InspectionFrequency;
            Area = missionDefinition.Area;
            Tasks = service.GetTasksFromSource(missionDefinition.Source, missionDefinition.InstallationCode).Result!;
            LastSuccessfulRun = missionDefinition.LastSuccessfulRun;
            IsDeprecated = missionDefinition.IsDeprecated;
            SourceType = missionDefinition.Source.Type;
        }
    }
}
