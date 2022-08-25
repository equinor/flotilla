using Api.Database.Models;

namespace Api.Controllers.Models
{
    public struct CreateReportQuery
    {
        public string RobotId { get; set; }
        public string IsarMissionId { get; set; }
        public string EchoMissionId { get; set; }
        public string Log { get; set; }
        public DateTimeOffset StartTime { get; set; }
        public MissionStatus ReportStatus { get; set; }
    }
}
