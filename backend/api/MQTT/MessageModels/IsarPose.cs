using System.Text.Json.Serialization;
using Api.Database.Models;

namespace Api.Mqtt.MessageModels
{
#nullable disable

    public class IsarPoseMessage : MqttMessage
    {
        [JsonPropertyName("pose")]
        public IsarPose Pose { get; set; }

        [JsonPropertyName("robot_name")]
        public string RobotName { get; set; }

        [JsonPropertyName("isar_id")]
        public string IsarId { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Naming",
        "IDE1006: Naming rule violation",
        Justification = "Need to keep lower letter variable names for MQTT to be able to cast message to class"
    )]
    public class IsarPose
    {
        public IsarPosition position { get; set; }
        public IsarOrientation orientation { get; set; }
        public IsarFrame frame { get; set; }

        public void CopyIsarPoseToRobotPose(Pose robotPose)
        {
            robotPose.Frame = frame.name;

            robotPose.Position.X = position.x;
            robotPose.Position.Y = position.y;
            robotPose.Position.Z = position.z;

            robotPose.Orientation.X = orientation.x;
            robotPose.Orientation.Y = orientation.y;
            robotPose.Orientation.Z = orientation.z;
            robotPose.Orientation.W = orientation.w;
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Naming",
        "IDE1006: Naming rule violation",
        Justification = "Need to keep lower letter variable names for MQTT to be able to cast message to class"
    )]
    public class IsarPosition
    {
        public float x { get; set; }
        public float y { get; set; }
        public float z { get; set; }
        public IsarFrame frame { get; set; }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Naming",
        "IDE1006: Naming rule violation",
        Justification = "Need to keep lower letter variable names for MQTT to be able to cast message to class"
    )]
    public class IsarOrientation
    {
        public float x { get; set; }
        public float y { get; set; }
        public float z { get; set; }
        public float w { get; set; }
        public IsarFrame frame { get; set; }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Naming",
        "IDE1006: Naming rule violation",
        Justification = "Need to keep lower letter variable names for MQTT to be able to cast message to class"
    )]
    public class IsarFrame
    {
        public string name { get; set; }
    }
}
