using Api.Database.Models;

namespace Api.Controllers.Models
{
    public struct InspectionFindingsQuery
    {

        public string RobotName { get; set; }

        public string InspectionDate { get; set; }

        public string Area { get; set; }

        public string IsarStepId { get; set; }

        public string Findings { get; set; }

        public string MissionRunId { get; set; }


    }
}

