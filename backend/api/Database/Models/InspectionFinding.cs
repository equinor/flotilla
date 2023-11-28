using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Api.Controllers.Models;
#pragma warning disable CS8618
namespace Api.Database.Models
{
    public class InspectionFinding
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }

        public DateTime InspectionDate { get; set; }

        public string IsarStepId { get; set; }

        public string Finding { get; set; }

        public InspectionFinding(InspectionFindingQuery createInspectionFindingQuery)
        {
            InspectionDate = createInspectionFindingQuery.InspectionDate;
            Finding = createInspectionFindingQuery.Finding;
        }

        public InspectionFinding()
        {
            InspectionDate = DateTime.UtcNow;
            IsarStepId = "string";
            Finding = "string";
        }
    }

}
