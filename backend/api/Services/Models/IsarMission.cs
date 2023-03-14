namespace Api.Services.Models
{
    public class IsarMission
    {
        public string IsarMissionId { get; set; }

        public List<IsarTask> Tasks { get; set; }

        public IsarMission(IsarStartMissionResponse missionResponse)
        {
            IsarMissionId = missionResponse.MissionId;
            Tasks = missionResponse.Tasks.Select(task => new IsarTask(task)).ToList();
        }
    }
}
