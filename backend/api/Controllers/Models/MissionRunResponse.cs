using System.Text.Json.Serialization;
using Api.Database.Models;
namespace Api.Controllers.Models
{
    public class MissionRunResponse
    {
        public string Id { get; set; }

        public string? MissionId { get; set; }

        public string? IsarMissionId { get; set; }

        public string Name { get; set; }

        public string? Description { get; set; }

        public string? StatusReason { get; set; }

        public string? Comment { get; set; }

        public string InstallationCode { get; set; }

        public AreaResponse? Area { get; set; }

        public virtual RobotResponse Robot { get; set; }

        public MissionStatus Status { get; set; }

        public bool IsCompleted;

        public DateTimeOffset DesiredStartTime { get; set; }

        public DateTimeOffset? StartTime { get; private set; }

        public DateTimeOffset? EndTime { get; private set; }

        public uint? EstimatedDuration { get; set; }

        public IList<MissionTask> Tasks { get; set; }

        public MapMetadata? Map { get; set; }

        public MissionRunPriority MissionRunPriority { get; set; }

        [JsonConstructor]
#nullable disable
        public MissionRunResponse() { }
#nullable enable

        public MissionRunResponse(MissionRun mission)
        {
            Id = mission.Id;
            MissionId = mission.MissionId;
            IsarMissionId = mission.IsarMissionId;
            Name = mission.Name;
            Description = mission.Description;
            StatusReason = mission.StatusReason;
            Comment = mission.Comment;
            InstallationCode = mission.InstallationCode;
            Area = mission.Area != null ? new AreaResponse(mission.Area) : null;
            Robot = new RobotResponse(mission.Robot);
            Status = mission.Status;
            IsCompleted = mission.IsCompleted;
            DesiredStartTime = mission.DesiredStartTime.ToLocalTime();
            StartTime = mission.StartTime?.ToLocalTime();
            EndTime = mission.EndTime?.ToLocalTime();
            EstimatedDuration = mission.EstimatedDuration;
            Tasks = mission.Tasks;
            Map = mission.Map;
            MissionRunPriority = mission.MissionRunPriority;
        }

    }
}
