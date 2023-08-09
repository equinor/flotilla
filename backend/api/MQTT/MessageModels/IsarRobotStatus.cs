using System.Text.Json.Serialization;
using Api.Database.Models;

namespace Api.Mqtt.MessageModels
{
#nullable disable
    public class IsarRobotStatusMessage : MqttMessage
    {
        [JsonPropertyName("robot_name")]
        public string RobotName { get; set; }

        [JsonPropertyName("isar_id")]
        public string IsarId { get; set; }

        [JsonPropertyName("robot_status")]
        public RobotStatus RobotStatus { get; set; }

        [JsonPropertyName("previous_robot_status")]
        public RobotStatus PreviousRobotStatus { get; set; }

        [JsonPropertyName("current_isar_state")]
        public string CurrentState { get; set; }

        [JsonPropertyName("current_mission_id")]
        public string CurrentMissionId { get; set; }

        [JsonPropertyName("current_task_id")]
        public string CurrentTaskId { get; set; }

        [JsonPropertyName("current_step_id")]
        public string CurrentStepId { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }
    }
}
