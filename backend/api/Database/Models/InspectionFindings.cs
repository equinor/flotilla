using Microsoft.EntityFrameworkCore;
using Api.Controllers.Models;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
#pragma warning disable CS8618
namespace Api.Database.Models
{
    [Owned]
    public class InspectionFindings
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }

        public string RobotName { get; set; }

        public string InspectionDate { get; set; }

        public string Area { get; set; }

        public string IsarStepId { get; set; }

        public string Findings { get; set; }

        public string MissionRunId { get; set; }

        public InspectionFindings(InspectionFindingsQuery createInspectionFindingQuery)
        {
            RobotName = createInspectionFindingQuery.RobotName;
            InspectionDate = createInspectionFindingQuery.InspectionDate;
            Area = createInspectionFindingQuery.Area;
            IsarStepId = createInspectionFindingQuery.IsarStepId;
            Findings = createInspectionFindingQuery.Findings;
            MissionRunId = createInspectionFindingQuery.MissionRunId;
        }

        public InspectionFindings()
        {
            RobotName = "string";
            InspectionDate = "string";
            Area = "string";
            IsarStepId = "string";
            Findings = "string";
            MissionRunId = "string";
        }
    }

}
