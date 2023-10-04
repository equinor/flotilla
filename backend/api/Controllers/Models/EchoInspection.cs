﻿using Api.Database.Models;

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

    public class EchoInspectionComparer : IEqualityComparer<EchoInspection>
    {
        public bool Equals(EchoInspection? e1, EchoInspection? e2)
        {
            if (ReferenceEquals(e1, e2))
                return true;

            if (e2 is null || e1 is null)
                return false;

            return e1.InspectionType == e2.InspectionType
                && e1.TimeInSeconds == e2.TimeInSeconds;
        }

        public int GetHashCode(EchoInspection e)
        {
            // We cannot incorporate TimeInSeconds here are SQL queries do not handle 
            // nullables even with short circuiting logic
            return (int)e.InspectionType;
        }
    }
}
