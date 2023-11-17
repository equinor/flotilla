using Api.Database.Models;
#pragma warning disable CS8618
namespace Api.Controllers.Models
{
    public class RobotResponse(Robot robot)
    {
        public string Id { get; } = robot.Id;

        public string Name { get; } = robot.Name;

        public string IsarId { get; } = robot.IsarId;

        public virtual RobotModel Model { get; } = robot.Model;

        public string SerialNumber { get; } = robot.SerialNumber;

        public string CurrentInstallation { get; } = robot.CurrentInstallation;

        public AreaResponse? CurrentArea { get; } = robot.CurrentArea != null ? new AreaResponse(robot.CurrentArea) : null;

        public float BatteryLevel { get; } = robot.BatteryLevel;

        public float? PressureLevel { get; } = robot.PressureLevel;

        public IList<VideoStream> VideoStreams { get; } = robot.VideoStreams;

        public string Host { get; } = robot.Host;

        public int Port { get; } = robot.Port;

        public bool Enabled { get; } = robot.Enabled;

        public bool MissionQueueFrozen { get; } = robot.MissionQueueFrozen;

        public RobotStatus Status { get; } = robot.Status;

        public Pose Pose { get; } = robot.Pose;

        public string? CurrentMissionId { get; } = robot.CurrentMissionId;

        public string IsarUri { get; } = robot.IsarUri;
    }
}
