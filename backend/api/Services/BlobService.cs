using System.Globalization;
using Api.Options;
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
    }

    public class BlobService : IBlobService
    {
        private readonly IOptions<AzureAdOptions> _azureOptions;
        private readonly ILogger<BlobService> _logger;

        public BlobService(ILogger<BlobService> logger, IOptions<AzureAdOptions> azureOptions)
        {
            _logger = logger;
            _azureOptions = azureOptions;
        }

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
                _logger.LogError(e, "{ErrorMessage}", errorMessage);
                throw;
            }

        }

        private BlobContainerClient GetBlobContainerClient(string containerName, string accountName)
        {
            var serviceClient = new BlobServiceClient(
                new Uri($"https://{accountName}.blob.core.windows.net"),
                new ClientSecretCredential(
                    _azureOptions.Value.TenantId,
                    _azureOptions.Value.ClientId,
                    _azureOptions.Value.ClientSecret
                )
            );
            var containerClient = serviceClient.GetBlobContainerClient(containerName.ToLower(CultureInfo.CurrentCulture));
            return containerClient;
        }
    }
}
