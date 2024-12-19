using System.Text.Json.Serialization;
using Api.Database.Models;
namespace Api.Controllers.Models
{
    public class RobotResponse
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string IsarId { get; set; }

        public virtual RobotModel Model { get; set; }

        public string SerialNumber { get; set; }

        public Installation CurrentInstallation { get; }

        public InspectionAreaResponse? CurrentInspectionArea { get; set; }

        public float BatteryLevel { get; set; }

        public BatteryState? BatteryState { get; set; }

        public float? PressureLevel { get; set; }

        public IList<DocumentInfo> Documentation { get; set; }

        public string Host { get; set; }

        public int Port { get; set; }

        public bool IsarConnected { get; set; }

        public bool Deprecated { get; set; }

        public RobotFlotillaStatus FlotillaStatus { get; set; }

        public RobotStatus Status { get; set; }

        public Pose Pose { get; set; }

        public string? CurrentMissionId { get; set; }

        public string IsarUri { get; set; }

        public IList<RobotCapabilitiesEnum>? RobotCapabilities { get; set; }

        [JsonConstructor]
#nullable disable
        public RobotResponse() { }
#nullable enable

        public RobotResponse(Robot robot)
        {
            Id = robot.Id;
            Name = robot.Name;
            IsarId = robot.IsarId;
            Model = robot.Model;
            SerialNumber = robot.SerialNumber;
            CurrentInstallation = robot.CurrentInstallation;
            CurrentInspectionArea = robot.CurrentInspectionArea != null ? new InspectionAreaResponse(robot.CurrentInspectionArea) : null;
            BatteryLevel = robot.BatteryLevel;
            BatteryState = robot.BatteryState;
            PressureLevel = robot.PressureLevel;
            Documentation = robot.Documentation;
            Host = robot.Host;
            Port = robot.Port;
            IsarConnected = robot.IsarConnected;
            Deprecated = robot.Deprecated;
            FlotillaStatus = robot.FlotillaStatus;
            Status = robot.Status;
            Pose = robot.Pose;
            CurrentMissionId = robot.CurrentMissionId;
            IsarUri = robot.IsarUri;
            RobotCapabilities = robot.RobotCapabilities;
        }
    }
}
