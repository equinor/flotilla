using Api.Database.Models;

namespace Api.Controllers.Models
{
    public class EchoInspection
    {
        public InspectionType InspectionType { get; set; }

        public float? TimeInSeconds { get; set; }

        public EchoInspection()
        {
            InspectionType = InspectionType.Image;
        }

        public EchoInspection(SensorType echoSensorType)
        {
            InspectionType = InspectionTypeFromEchoSensorType(echoSensorType.Key);
            TimeInSeconds = (float?)echoSensorType.TimeInSeconds;
        }

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
                _
                  => throw new InvalidDataException(
                      $"Echo sensor type '{sensorType}' not supported"
                  )
            };
        }
    }
}
