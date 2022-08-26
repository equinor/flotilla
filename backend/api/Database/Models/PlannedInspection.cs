using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Api.Controllers.Models;
using Microsoft.EntityFrameworkCore;
using static Api.Database.Models.IsarStep;

#nullable disable
namespace Api.Database.Models
{
    [Owned]
    public class PlannedInspection
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }

        public InspectionTypeEnum InspectionType { get; set; }

        public float? TimeInSeconds { get; set; }

        public PlannedInspection()
        {
            InspectionType = InspectionTypeEnum.Image;
        }

        public PlannedInspection(EchoInspection echoInspection)
        {
            InspectionType = echoInspection.InspectionType;
            TimeInSeconds = echoInspection.TimeInSeconds;
        }
    }
}
