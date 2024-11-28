using System.Text.Json.Serialization;
#nullable disable

namespace Api.Services.Models
{
    public class IDAInspectionDataResponse
    {
        [JsonPropertyName("storageAccount")]
        public string StorageAccount { get; set; }

        [JsonPropertyName("blobContainer")]
        public string BlobContainer { get; set; }

        [JsonPropertyName("blobName")]
        public string BlobName { get; set; }

    }
}
