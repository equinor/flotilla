using System.Text.Json.Serialization;

namespace Api.Mqtt.MessageModels
{
#nullable disable
    public class SaraInspectionResultMessage : MqttMessage
    {
        [JsonPropertyName("inspection_id")]
        public string InspectionId { get; set; }

        [JsonPropertyName("workflow_id")]
        public required Guid WorkflowId { get; set; }

        [JsonPropertyName("analysis_run_id")]
        public required Guid AnalysisRunId { get; set; }

        [JsonPropertyName("analysis_id")]
        public required Guid AnalysisId { get; set; }
    }

    public class InspectionResultMessage
    {
        [JsonPropertyName("inspectionId")]
        public string InspectionId { get; set; }
    }
}
