﻿using System.Text.Json.Serialization;

namespace Api.Services.Models
{
    public class IDAInspectionDataResponse
    {
        [JsonPropertyName("storageAccount")]
        public required string StorageAccount { get; set; }

        [JsonPropertyName("blobContainer")]
        public required string BlobContainer { get; set; }

        [JsonPropertyName("blobName")]
        public required string BlobName { get; set; }

    }
}
