using System.Text.Json.Serialization;

namespace Api.Mqtt.MessageModels
{
#nullable disable
    public class IsarMissionAbortedMessage : MqttMessage
    {
        [JsonPropertyName("isar_id")]
        public string IsarId { get; set; }

        [JsonPropertyName("robot_name")]
        public string RobotName { get; set; }

        [JsonPropertyName("mission_id")]
        public string MissionId { get; set; }

        [JsonPropertyName("can_be_continued")]
        public bool CanBeContinued { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

#nullable enable
        [JsonPropertyName("reason")]
        public string? Reason { get; set; }
    }
}
