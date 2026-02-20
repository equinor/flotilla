using System.Text.Json.Serialization;

namespace Api.Controllers.Models
{
#nullable disable
    public class FetchCO2Query
    {
        [JsonPropertyName("facility")]
        public string Facility { get; set; }

        [JsonPropertyName("task_start_time")]
        public string TaskStartTime { get; set; }

        [JsonPropertyName("task_end_time")]
        public string TaskEndTime { get; set; }

        [JsonPropertyName("inspection_name")]
        public string InspectionName { get; set; }
    }
}
