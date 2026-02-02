using System.Text.Json.Serialization;
using Api.Database.Models;

namespace Api.Controllers.Models
{
    public class RobotResponse
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string IsarId { get; set; }

        public string SerialNumber { get; set; }

        public Installation CurrentInstallation { get; }

        public string? CurrentInspectionAreaId { get; set; }

        public IList<DocumentInfo> Documentation { get; set; }

        public string Host { get; set; }

        public int Port { get; set; }

        public bool Deprecated { get; set; }

        public RobotStatus Status { get; set; }

        public string? CurrentMissionId { get; set; }

        public string IsarUri { get; set; }

        public RobotType Type { get; set; }

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
            SerialNumber = robot.SerialNumber;
            CurrentInstallation = robot.CurrentInstallation;
            CurrentInspectionAreaId = robot.CurrentInspectionAreaId;
            Documentation = robot.Documentation;
            Host = robot.Host;
            Port = robot.Port;
            Deprecated = robot.Deprecated;
            Status = robot.Status;
            CurrentMissionId = robot.CurrentMissionId;
            IsarUri = robot.IsarUri;
            RobotCapabilities = robot.RobotCapabilities;
            Type = robot.Type;
        }
    }
}
