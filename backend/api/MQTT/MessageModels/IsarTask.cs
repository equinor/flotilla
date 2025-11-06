using System.Text.Json.Serialization;
using Api.Database.Models;

namespace Api.Mqtt.MessageModels
{
#nullable disable
    public class IsarTaskMessage : MqttMessage
    {
        [JsonPropertyName("robot_name")]
        public string RobotName { get; set; }

        [JsonPropertyName("isar_id")]
        public string IsarId { get; set; }

        [JsonPropertyName("mission_id")]
        public string MissionId { get; set; }

        [JsonPropertyName("task_id")]
        public string TaskId { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("task_type")]
        public string TaskType { get; set; }

        [JsonPropertyName("error_description")]
        public string ErrorDescription { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        public bool IsInspectionTask(string isarTaskType)
        {
            return isarTaskType switch
            {
                "record_audio" => true,
                "take_image" => true,
                "take_video" => true,
                "take_thermal_image" => true,
                "take_thermal_video" => true,
                "take_co2_measurement" => true,
                "return_to_home" => false,

                _ => throw new ArgumentException($"ISAR Task type '{isarTaskType}' not supported"),
            };
        }
    }
}
