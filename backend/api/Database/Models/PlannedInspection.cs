using Api.Controllers.Models;
using Microsoft.EntityFrameworkCore;
using static Api.Database.Models.IsarStep;

namespace Api.Database.Models
{
    [Owned]
    public class PlannedInspection
    {
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
