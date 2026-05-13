using System.Text.Json.Serialization;
using Api.Database.Models;

namespace Api.Controllers.Models
{
    public class MissionDefinitionResponse
    {
        public string Id { get; set; }
        public List<TaskDefinition> Tasks { get; set; }
        public string Name { get; set; }
        public string InstallationCode { get; set; }
        public string? Comment { get; set; }
        public AutoScheduleFrequency? AutoScheduleFrequency { get; set; }
        public virtual MissionRun? LastSuccessfulRun { get; set; }
        public InspectionAreaResponse InspectionArea { get; set; }
        public bool IsDeprecated { get; set; }

        [JsonConstructor]
#nullable disable
        public MissionDefinitionResponse() { }

#nullable enable

        public MissionDefinitionResponse(MissionDefinition missionDefinition)
        {
            Id = missionDefinition.Id ?? string.Empty;
            Tasks = missionDefinition.Tasks;
            Name = missionDefinition.Name ?? string.Empty;
            InstallationCode = missionDefinition.InstallationCode ?? string.Empty;
            Comment = missionDefinition.Comment;
            AutoScheduleFrequency =
                (
                    missionDefinition.AutoScheduleFrequency is not null
                    && missionDefinition.AutoScheduleFrequency.HasValidValue()
                )
                    ? missionDefinition.AutoScheduleFrequency
                    : null;
            LastSuccessfulRun = missionDefinition.LastSuccessfulRun;
            InspectionArea = new InspectionAreaResponse(missionDefinition.InspectionArea);
            IsDeprecated = missionDefinition.IsDeprecated;
        }
    }
}
