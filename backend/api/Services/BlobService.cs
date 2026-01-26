using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Api.Utilities;
using Azure;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;

namespace Api.Services
{
    public interface IBlobService
    {
        public Task<byte[]?> DownloadBlob(
            string blobName,
            string containerName,
            string accountName
        );

        public AsyncPageable<BlobItem> FetchAllBlobs(string containerName, string accountName);

        public Task UploadJsonToBlob(
            string json,
            string path,
            string containerName,
            string accountName,
            bool overwrite
        );
    }

    public class BlobService(ILogger<BlobService> logger) : IBlobService
    {
        public async Task<byte[]?> DownloadBlob(
            string blobName,
            string containerName,
            string accountName
        )
        {
            var blobContainerClient = GetBlobContainerClient(containerName, accountName);
            var blobClient = blobContainerClient.GetBlobClient(blobName);

            using var memoryStream = new MemoryStream();
            try
            {
                await blobClient.DownloadToAsync(memoryStream);
            }
            catch (RequestFailedException)
            {
                logger.LogWarning(
                    "Failed to download blob {blobName} from container {containerName}",
                    blobName,
                    containerName
                );
                return null;
            }

            return memoryStream.ToArray();
        }

        public AsyncPageable<BlobItem> FetchAllBlobs(string containerName, string accountName)
        {
            var blobContainerClient = GetBlobContainerClient(containerName, accountName);
            try
            {
                GetBlobsOptions blobOptions = new() { Traits = BlobTraits.Metadata };
                return blobContainerClient.GetBlobsAsync(options: blobOptions);
            }
            catch (RequestFailedException e)
            {
                string errorMessage = $"Failed to fetch blob items because: {e.Message}";
                logger.LogError(e, "{ErrorMessage}", errorMessage);
                throw;
            }
        }

        public async Task UploadJsonToBlob(
            string json,
            string path,
            string containerName,
            string accountName,
            bool overwrite = false
        )
        {
            var blobContainerClient = GetBlobContainerClient(containerName, accountName);

            var blobClient = blobContainerClient.GetBlobClient(path);

            using var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(json));

            try
            {
                await blobClient.UploadAsync(memoryStream, overwrite);
            }
            catch (RequestFailedException e)
            {
                if (e.Status == 404 && e.ErrorCode == "ContainerNotFound")
                {
                    logger.LogError(
                        e,
                        "{ErrorMessage}",
                        $"Unable to find blob container {containerName}"
                    );
                    throw new ConfigException($"Unable to find blob container {containerName}");
                }

                string errorMessage = $"Failed to fetch blob items because: {e.Message}";
                logger.LogError(e, "{ErrorMessage}", errorMessage);
                throw;
            }
        }

        private BlobContainerClient GetBlobContainerClient(string containerName, string accountName)
        {
            var credential = new DefaultAzureCredential();

            var serviceClient = new BlobServiceClient(
                new Uri($"https://{accountName}.blob.core.windows.net"),
                credential
            );
            var containerClient = serviceClient.GetBlobContainerClient(
                containerName.ToLower(CultureInfo.CurrentCulture)
            );
            return containerClient;
        }
    }
}
