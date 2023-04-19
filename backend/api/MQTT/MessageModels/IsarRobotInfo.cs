using System.Text.Json.Serialization;
using Api.Controllers.Models;
using Api.Database.Models;

namespace Api.Mqtt.MessageModels
{
#nullable disable
    public class IsarRobotInfoMessage : MqttMessage
    {
        [JsonPropertyName("robot_name")]
        public string RobotName { get; set; }

        [JsonPropertyName("isar_id")]
        public string IsarId { get; set; }

        [JsonPropertyName("robot_model")]
        public RobotModel.RobotType RobotModel { get; set; }

        [JsonPropertyName("robot_serial_number")]
        public string SerialNumber { get; set; }

        [JsonPropertyName("video_streams")]
        public List<CreateVideoStreamQuery> VideoStreamQueries { get; set; }

        [JsonPropertyName("host")]
        public string Host { get; set; }

        [JsonPropertyName("port")]
        public int Port { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }
    }
}
