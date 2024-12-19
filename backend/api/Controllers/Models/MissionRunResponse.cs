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

        public InspectionAreaResponse? InspectionArea { get; set; }

        public virtual RobotResponse Robot { get; set; }

        public MissionStatus Status { get; set; }

        public bool IsCompleted;

        public DateTime DesiredStartTime { get; set; }

        public DateTime? StartTime { get; private set; }

        public DateTime? EndTime { get; private set; }

        public uint? EstimatedDuration { get; set; }

        public IList<MissionTask> Tasks { get; set; }

        public MissionRunType MissionRunType { get; set; }

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
            InspectionArea = mission.InspectionArea != null ? new InspectionAreaResponse(mission.InspectionArea) : null;
            Robot = new RobotResponse(mission.Robot);
            Status = mission.Status;
            IsCompleted = mission.IsCompleted;
            DesiredStartTime = mission.DesiredStartTime;
            StartTime = mission.StartTime;
            EndTime = mission.EndTime;
            EstimatedDuration = mission.EstimatedDuration;
            Tasks = mission.Tasks;
            MissionRunType = mission.MissionRunType;
        }

    }
}
