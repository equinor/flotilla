using static Api.Database.Models.IsarStep;

namespace Api.Controllers.Models
{
    public class EchoInspection
    {
        public InspectionTypeEnum InspectionType { get; set; }

        public float? TimeInSeconds { get; set; }

        public EchoInspection(InspectionTypeEnum inspectionType, float? timeInSeconds)
        {
            InspectionType = inspectionType;
            TimeInSeconds = timeInSeconds;
        }
    }
}
