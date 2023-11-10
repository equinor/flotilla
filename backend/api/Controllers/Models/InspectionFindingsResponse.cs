using Api.Database.Models;

namespace Api.Controllers.Models
{
    public struct InspectionFindingsResponse
    {

        public string RobotName { get; set; }

        public string InspectionDate { get; set; }

        public string Area { get; set; }

        public string IsarStepId { get; set; }

        public string Findings { get; set; }

        public string MissionRunId { get; set; }

        public InspectionFindingsResponse(InspectionFindings inspectionFindings)
        {
            RobotName = inspectionFindings.RobotName;
            InspectionDate = inspectionFindings.InspectionDate;
            Area = inspectionFindings.Area;
            IsarStepId = inspectionFindings.IsarStepId;
            Findings = inspectionFindings.Findings;
            MissionRunId = inspectionFindings.MissionRunId;
        }
    }
}

