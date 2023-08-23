#nullable enable
using System.Text.Json.Serialization;
using Api.Database.Models;
using Api.Services;

namespace Api.Controllers.Models
{
    public class CondensedMissionDefinitionResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("installationCode")]
        public string InstallationCode { get; set; }

        [JsonPropertyName("comment")]
        public string? Comment { get; set; }

        [JsonPropertyName("inspectionFrequency")]
        public TimeSpan? InspectionFrequency { get; set; }

        [JsonPropertyName("lastRun")]
        public virtual MissionRun? LastRun { get; set; }

        [JsonPropertyName("area")]
        public AreaResponse? Area { get; set; }

        [JsonPropertyName("isDeprecated")]
        public bool IsDeprecated { get; set; }

        [JsonPropertyName("sourceType")]
        public MissionSourceType SourceType { get; set; }

        public CondensedMissionDefinitionResponse(MissionDefinition missionDefinition)
        {
            Id = missionDefinition.Id;
            Name = missionDefinition.Name;
            InstallationCode = missionDefinition.InstallationCode;
            Comment = missionDefinition.Comment;
            InspectionFrequency = missionDefinition.InspectionFrequency;
            Area = new AreaResponse(missionDefinition.Area);
            LastRun = missionDefinition.LastRun;
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

        [JsonPropertyName("lastRun")]
        public virtual MissionRun? LastRun { get; set; }

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
            Tasks = service.GetTasksFromSource(missionDefinition.Source, missionDefinition.InstallationCode).Result;
            LastRun = missionDefinition.LastRun;
            IsDeprecated = missionDefinition.IsDeprecated;
            SourceType = missionDefinition.Source.Type;
        }
    }
}
