using System.Text.Json.Serialization;
using Api.Database.Models;

namespace Api.Mqtt.MessageModels
{
#nullable disable
    public class IsarPoseMessage : MqttMessage
    {
        [JsonPropertyName("pose")]
        public IsarPose Pose{ get; set; }

        [JsonPropertyName("robot_id")]
        public string RobotId { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }
    }

    public class IsarPose
    {   
        public IsarPosition position { get; set; }
        public IsarOrientation orientation { get; set; }
        public IsarFrame frame { get; set; }

    }
    public class IsarPosition
    {
        public float x { get; set; }
        public float y { get; set; }
        public float z { get; set; }
        public IsarFrame frame { get; set; }
    }
    public class IsarOrientation
    {
        public float x { get; set; }
        public float y { get; set; }
        public float z { get; set; }
        public float w { get; set; }
        public IsarFrame frame { get; set; }
    }
    public class IsarFrame
    {
        public string name { get; set; }
    }
}
