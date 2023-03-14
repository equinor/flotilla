# nullable disable
using System.Text.Json.Serialization;

namespace Api.Controllers.Models
{
    public class EchoMissionResponse
    {
        [JsonPropertyName("robotPlanId")]
        public int Id { get; set; }

        [JsonPropertyName("installationCode")]
        public string InstallationCode { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("createdBy")]
        public string CreatedBy { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("robotOperator")]
        public string RobotOperator { get; set; }

        [JsonPropertyName("createdAt")]
        public string CreatedAt { get; set; }

        [JsonPropertyName("lastModified")]
        public string LastModified { get; set; }

        [JsonPropertyName("inspectionDate")]
        public string InspectionDate { get; set; }

        [JsonPropertyName("planItems")]
        public List<PlanItem> PlanItems { get; set; }
    }

    public class PlanItem
    {
        [JsonPropertyName("planItemId")]
        public int Id { get; set; }

        [JsonPropertyName("tag")]
        public string Tag { get; set; }

        [JsonPropertyName("sortingOrder")]
        public int SortingOrder { get; set; }

        [JsonPropertyName("robotPlanId")]
        public int RobotPlanId { get; set; }

        [JsonPropertyName("sensorTypes")]
        public List<SensorType> SensorTypes { get; set; }

        [JsonPropertyName("poseId")]
        public int? PoseId { get; set; }
    }

    public class SensorType
    {
        [JsonPropertyName("planItemSensorTypeId")]
        public int Id { get; set; }

        [JsonPropertyName("sensorTypeKey")]
        public string Key { get; set; }

        [JsonPropertyName("timeInSeconds")]
        public decimal? TimeInSeconds { get; set; }

        [JsonPropertyName("planItemId")]
        public int PlanItemId { get; set; }
    }
}
