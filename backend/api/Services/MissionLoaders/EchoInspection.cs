using Api.Controllers.Models;
using Api.Database.Models;

namespace Api.Services.MissionLoaders
{
    public class EchoInspection
    {
        public EchoInspection()
        {
            InspectionType = InspectionType.Image;
            InspectionPoint = new Position();
        }

        public EchoInspection(
            SensorType echoSensorType,
            Position inspectionPoint,
            string? inspectionPointName
        )
        {
            InspectionType = InspectionTypeFromEchoSensorType(echoSensorType.Key);
            TimeInSeconds = (float?)echoSensorType.TimeInSeconds;
            InspectionPoint = inspectionPoint;
            InspectionPointName =
                inspectionPointName != "Stid Coordinate" ? inspectionPointName : null;
        }

        public InspectionType InspectionType { get; set; }

        public Position InspectionPoint { get; set; }

        public string? InspectionPointName { get; set; }

        public float? TimeInSeconds { get; set; }

        private static InspectionType InspectionTypeFromEchoSensorType(string sensorType)
        {
            return sensorType switch
            {
                "Picture" => InspectionType.Image,
                "ThermicPicture" => InspectionType.ThermalImage,
                "ThermalPicture" => InspectionType.ThermalImage,
                "Audio" => InspectionType.Audio,
                "Video" => InspectionType.Video,
                "ThermicVideo" => InspectionType.ThermalVideo,
                "ThermalVideo" => InspectionType.ThermalVideo,
                "CO2" => InspectionType.CO2Measurement,
                _ => throw new InvalidDataException(
                    $"Echo sensor type '{sensorType}' not supported"
                ),
            };
        }
    }

    public class EchoInspectionComparer : IEqualityComparer<EchoInspection>
    {
        public bool Equals(EchoInspection? e1, EchoInspection? e2)
        {
            if (ReferenceEquals(e1, e2))
            {
                return true;
            }

            if (e2 is null || e1 is null)
            {
                return false;
            }

            return e1.InspectionType == e2.InspectionType
                && e1.TimeInSeconds == e2.TimeInSeconds
                && e1.InspectionPoint.Equals(e2.InspectionPoint);
        }

        public int GetHashCode(EchoInspection e)
        {
            // We cannot incorporate TimeInSeconds here as SQL queries do not handle
            // nullables even with short circuiting logic
            return (int)e.InspectionType;
        }
    }
}
