using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Api.Controllers.Models;
#pragma warning disable CS8618
namespace Api.Database.Models
{
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

        public InspectionFindings(InspectionFindingsQuery createInspectionFindingQuery)
        {
            RobotName = createInspectionFindingQuery.RobotName;
            InspectionDate = createInspectionFindingQuery.InspectionDate;
            Area = createInspectionFindingQuery.Area;
            IsarStepId = createInspectionFindingQuery.IsarStepId;
            Findings = createInspectionFindingQuery.Findings;
        }

        public InspectionFindings()
        {
            RobotName = "string";
            InspectionDate = "string";
            Area = "string";
            IsarStepId = "string";
            Findings = "string";
        }
    }

}
