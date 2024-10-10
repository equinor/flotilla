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

        [Required]
        public DateTime InspectionDate { get; set; }

        [Required]
        public string IsarTaskId { get; set; }

        [Required]
        public string Finding { get; set; }

        public InspectionFinding(InspectionFindingQuery createInspectionFindingQuery)
        {
            InspectionDate = createInspectionFindingQuery.InspectionDate;
            Finding = createInspectionFindingQuery.Finding;
        }

        public InspectionFinding()
        {
            InspectionDate = DateTime.UtcNow;
            IsarTaskId = "string";
            Finding = "string";
        }
    }

}
