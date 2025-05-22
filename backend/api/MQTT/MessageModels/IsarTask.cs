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

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        public MissionTaskType GetMissionTaskTypeFromIsarTask(string isarTaskType)
        {
            return isarTaskType switch
            {
                "record_audio" => MissionTaskType.Inspection,
                "take_image" => MissionTaskType.Inspection,
                "take_video" => MissionTaskType.Inspection,
                "take_thermal_image" => MissionTaskType.Inspection,
                "take_thermal_video" => MissionTaskType.Inspection,
                "return_to_home" => MissionTaskType.ReturnHome,
                "take_co2_measurement" => MissionTaskType.Inspection,

                _ => throw new ArgumentException($"ISAR Task type '{isarTaskType}' not supported"),
            };
        }
    }
}
