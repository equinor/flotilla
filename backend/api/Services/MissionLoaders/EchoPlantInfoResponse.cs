using System.Text.Json.Serialization;

namespace Api.Services.MissionLoaders
{
    public class EchoPlantInfoResponse
    {
        [JsonPropertyName("plantCode")]
        public string? PlantCode { get; set; }

        [JsonPropertyName("installationCode")]
        public string? InstallationCode { get; set; }

        [JsonPropertyName("projectDescription")]
        public string? ProjectDescription { get; set; }

        [JsonPropertyName("plantDirectory")]
        public string? PlantDirectory { get; set; }

        [JsonPropertyName("availableInEcho3D")]
        public bool AvailableInEcho3D { get; set; }

        [JsonPropertyName("availableInEcho3DWebReveal")]
        public bool AvailableInEcho3DWebReveal { get; set; }

        [JsonPropertyName("sapId")]
        public int? SapId { get; set; }

        [JsonPropertyName("ayelixSiteId")]
        public int? AyelixSiteId { get; set; }
    }
}
