namespace Api.Services.Models
{
    public class IsarMission(IsarStartMissionResponse missionResponse)
    {
        public string IsarMissionId { get; } = missionResponse.MissionId;

        public List<IsarTask> Tasks { get; } =
        [.. missionResponse.Tasks.Select(task => new IsarTask(task))];
    }
}
