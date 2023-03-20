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

    public class IsarPose
    {
        [JsonPropertyName("position")]
        public IsarPosition Position { get; set; }

        [JsonPropertyName("orientation")]
        public IsarOrientation Orientation { get; set; }

        [JsonPropertyName("frame")]
        public IsarFrame Frame { get; set; }

        public void CopyIsarPoseToRobotPose(Pose robotPose)
        {
            robotPose.Position.X = Position.X;
            robotPose.Position.Y = Position.Y;
            robotPose.Position.Z = Position.Z;

            robotPose.Orientation.X = Orientation.X;
            robotPose.Orientation.Y = Orientation.Y;
            robotPose.Orientation.Z = Orientation.Z;
            robotPose.Orientation.W = Orientation.W;
        }
    }

    public class IsarPosition
    {
        [JsonPropertyName("x")]
        public float X { get; set; }

        [JsonPropertyName("y")]
        public float Y { get; set; }

        [JsonPropertyName("z")]
        public float Z { get; set; }

        [JsonPropertyName("frame")]
        public IsarFrame Frame { get; set; }
    }

    public class IsarOrientation
    {
        [JsonPropertyName("x")]
        public float X { get; set; }

        [JsonPropertyName("y")]
        public float Y { get; set; }

        [JsonPropertyName("z")]
        public float Z { get; set; }

        [JsonPropertyName("w")]
        public float W { get; set; }

        [JsonPropertyName("frame")]
        public IsarFrame Frame { get; set; }
    }

    public class IsarFrame
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
    }
}
