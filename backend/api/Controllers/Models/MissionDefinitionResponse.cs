﻿#nullable enable
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

        [JsonPropertyName("area")]
        public AreaResponse? Area { get; set; }

        [JsonPropertyName("isDeprecated")]
        public bool IsDeprecated { get; set; }

        [JsonPropertyName("sourceId")]
        public string SourceId { get; set; } = string.Empty;

        [JsonConstructor]
        public MissionDefinitionResponse() { }

        public MissionDefinitionResponse(MissionDefinition missionDefinition)
        {
            Id = missionDefinition.Id;
            Name = missionDefinition.Name;
            InstallationCode = missionDefinition.InstallationCode;
            Comment = missionDefinition.Comment;
            InspectionFrequency = missionDefinition.InspectionFrequency;
            Area = missionDefinition.Area != null ? new AreaResponse(missionDefinition.Area) : null;
            LastSuccessfulRun = missionDefinition.LastSuccessfulRun;
            IsDeprecated = missionDefinition.IsDeprecated;
            SourceId = missionDefinition.Source.SourceId;
        }
    }

    public class MissionDefinitionWithTasksResponse(IMissionDefinitionService service, MissionDefinition missionDefinition)
    {
        [JsonPropertyName("id")]
        public string Id { get; } = missionDefinition.Id;

        [JsonPropertyName("tasks")]
        public List<MissionTask> Tasks { get; } = service.GetTasksFromSource(missionDefinition.Source).Result!;

        [JsonPropertyName("name")]
        public string Name { get; } = missionDefinition.Name;

        [JsonPropertyName("installationCode")]
        public string InstallationCode { get; } = missionDefinition.InstallationCode;

        [JsonPropertyName("comment")]
        public string? Comment { get; } = missionDefinition.Comment;

        [JsonPropertyName("inspectionFrequency")]
        public TimeSpan? InspectionFrequency { get; } = missionDefinition.InspectionFrequency;

        [JsonPropertyName("lastSuccessfulRun")]
        public virtual MissionRun? LastSuccessfulRun { get; } = missionDefinition.LastSuccessfulRun;

        [JsonPropertyName("area")]
        public Area? Area { get; } = missionDefinition.Area;

        [JsonPropertyName("isDeprecated")]
        public bool IsDeprecated { get; } = missionDefinition.IsDeprecated;
    }
}
