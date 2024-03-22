using Api.Database.Models;

namespace Api.Controllers.Models
{
    public struct CreateRobotQuery
    {
        public string Name { get; set; }

        public string IsarId { get; set; }

        public RobotType RobotType { get; set; }

        public string SerialNumber { get; set; }

        public string CurrentInstallationCode { get; set; }

        public string? CurrentAreaName { get; set; }

        public IList<CreateVideoStreamQuery> VideoStreams { get; set; }

        public string Host { get; set; }

        public int Port { get; set; }

        public bool IsarConnected { get; set; }

        public RobotStatus Status { get; set; }
    }
}
