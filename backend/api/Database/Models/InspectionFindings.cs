using Microsoft.EntityFrameworkCore;

#pragma warning disable CS8618
namespace Api.Database.Models
{
    [Owned]
    public class InspectionFindings
    {

        public string RobotName { get; set; }

        public string InspectionDate { get; set; }

        public string Area { get; set; }

        public string InspectionId { get; set; }

        public string FindingsTag { get; set; }

    }

}
