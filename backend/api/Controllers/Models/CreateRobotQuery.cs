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

        public IList<CreateDocumentationQuery> Documentation { get; set; }

        public string Host { get; set; }

        public int Port { get; set; }

        public IList<RobotCapabilitiesEnum> RobotCapabilities { get; set; }

        public RobotStatus Status { get; set; }
    }
}
