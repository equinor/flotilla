using Api.Database.Models;

#pragma warning disable CS8618
namespace Api.Controllers.Models
{
    public class MissionRunDto
    {
        public MissionRunDto(MissionRun mission)
        {
            Id = mission.Id;
            IsarMissionId = mission.IsarMissionId;
            Name = mission.Name;
            Description = mission.Description;
            StatusReason = mission.StatusReason;
            Comment = mission.Comment;
            InstallationCode = mission.InstallationCode;
            Robot = mission.Robot;
            Status = mission.Status;
            IsCompleted = mission.IsCompleted;
            MapMetadata = mission.MapMetadata;
            Error = mission.GetError(); // TODO: rethink whether nested errors are needed here
            DesiredStartTime = mission.DesiredStartTime;
            StartTime = mission.StartTime;
            EndTime = mission.EndTime;
            EstimatedDuration = mission.EstimatedDuration;
            Tasks = mission.Tasks.Select(t => new MissionTaskDto(t)).ToList();
        }

        public string Id { get; set; }

        public int? EchoMissionId { get; set; }

        public string? IsarMissionId { get; set; }

        public string Name { get; set; }

        public string? Description { get; set; }

        public string? StatusReason { get; set; }

        public string? Comment { get; set; }

        public string InstallationCode { get; set; }

        public Robot Robot { get; set; }

        public MissionStatus Status { get; set; }

        public bool IsCompleted { get; set; }

        public MapMetadata? MapMetadata { get; set; }

        public string? Error { get; set; }

        public DateTimeOffset DesiredStartTime { get; set; }

        public DateTimeOffset? StartTime { get; private set; }

        public DateTimeOffset? EndTime { get; private set; }

        public uint? EstimatedDuration { get; set; }

        public IList<MissionTaskDto> Tasks { get; set; }
    }
}
