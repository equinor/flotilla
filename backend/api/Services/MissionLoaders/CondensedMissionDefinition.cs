#nullable disable
using System.Text.Json.Serialization;
using Api.Database.Models;

namespace Api.Services.MissionLoaders
{
    public class CondensedMissionDefinition
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string InstallationCode { get; set; }

        public string SourceId { get; set; }

        [JsonConstructor]
        public CondensedMissionDefinition() { }

        public CondensedMissionDefinition(MissionDefinition missionDefinition)
        {
            Id = missionDefinition.Id;
            Name = missionDefinition.Name;
            InstallationCode = missionDefinition.InstallationCode;
            SourceId = missionDefinition.Source.SourceId;
        }
    }
}
