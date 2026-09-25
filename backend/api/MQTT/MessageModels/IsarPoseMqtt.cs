using System.Text.Json.Serialization;
using Api.Services.Models;

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
}
