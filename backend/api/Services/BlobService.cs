﻿using System.Globalization;
using System.Text;
using Api.Options;
using Api.Utilities;
using Azure;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;
namespace Api.Services
{
    public interface IBlobService
    {
        public Task<byte[]> DownloadBlob(string blobName, string containerName, string accountName);

        public AsyncPageable<BlobItem> FetchAllBlobs(string containerName, string accountName);

        public Task UploadJsonToBlob(string json, string path, string containerName, string accountName, bool overwrite);
    }

    public class BlobService(ILogger<BlobService> logger, IOptions<AzureAdOptions> azureOptions) : IBlobService
    {
        public async Task<byte[]> DownloadBlob(string blobName, string containerName, string accountName)
        {
            var blobContainerClient = GetBlobContainerClient(containerName, accountName);
            var blobClient = blobContainerClient.GetBlobClient(blobName);

            using var memoryStream = new MemoryStream();
            await blobClient.DownloadToAsync(memoryStream);

            return memoryStream.ToArray();
        }

        public AsyncPageable<BlobItem> FetchAllBlobs(string containerName, string accountName)
        {
            var blobContainerClient = GetBlobContainerClient(containerName, accountName);
            try
            {
                return blobContainerClient.GetBlobsAsync(BlobTraits.Metadata);
            }
            catch (RequestFailedException e)
            {
                string errorMessage = $"Failed to fetch blob items because: {e.Message}";
                logger.LogError(e, "{ErrorMessage}", errorMessage);
                throw;
            }
        }

        public async Task UploadJsonToBlob(string json, string path, string containerName, string accountName, bool overwrite = false)
        {
            var blobContainerClient = GetBlobContainerClient(containerName, accountName);

            var blobClient = blobContainerClient.GetBlobClient(path);

            using var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(json));

            try { await blobClient.UploadAsync(memoryStream, overwrite); }
            catch (RequestFailedException e)
            {
                if (e.Status == 404 && e.ErrorCode == "ContainerNotFound")
                {
                    logger.LogError(e, "{ErrorMessage}", $"Unable to find blob container {containerName}");
                    throw new ConfigException($"Unable to find blob container {containerName}");
                }
                else
                {
                    string errorMessage = $"Failed to fetch blob items because: {e.Message}";
                    logger.LogError(e, "{ErrorMessage}", errorMessage);
                    throw;
                }
            }
        }

        private BlobContainerClient GetBlobContainerClient(string containerName, string accountName)
        {
            var serviceClient = new BlobServiceClient(
                new Uri($"https://{accountName}.blob.core.windows.net"),
                new ClientSecretCredential(
                    azureOptions.Value.TenantId,
                    azureOptions.Value.ClientId,
                    azureOptions.Value.ClientSecret
                )
            );
            var containerClient = serviceClient.GetBlobContainerClient(containerName.ToLower(CultureInfo.CurrentCulture));
            return containerClient;
        }
    }
}
