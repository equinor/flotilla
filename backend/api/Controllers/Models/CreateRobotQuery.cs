using Api.Database.Models;

namespace Api.Controllers.Models
{
    public struct CreateRobotQuery
    {
        public string Name { get; set; }

        public RobotModel Model { get; set; }

        public string SerialNumber { get; set; }

        public IList<CreateVideoStreamQuery> VideoStreams { get; set; }

        public string Host { get; set; }

        public int Port { get; set; }

        public bool Enabled { get; set; }

        public RobotStatus Status { get; set; }
    }
}
