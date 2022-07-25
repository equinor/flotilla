using System.Text.Json.Serialization;
using Api.Database.Models;

namespace Api.Mqtt.MessageModels
{
#nullable disable
    public class IsarPoseMessage : MqttMessage
    {
        [JsonPropertyName("pose")]
        public Pose Pose{ get; set; }

        [JsonPropertyName("robot_id")]
        public string RobotId { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }
    }
}