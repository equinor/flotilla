

using System.Text.Json;
using System.Text.Json.Serialization;
using Api.Database.Models;
using Api.Options;
using Azure.Storage;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Options;

namespace Api.Services
{

    public interface ISourceService
    {
        Task<Uri> UploadSource(string id, List<MissionTask> tasks);
        List<MissionTask>? GetMissionTasksFromMissionId(string id);
        List<MissionTask>? GetMissionTasksFromURL(string url);
    }

    public class SourceService : ISourceService
    {
        private readonly IOptions<StorageOptions> _storageOptions;
        private readonly ILogger<SourceService> _logger;

        public SourceService(IOptions<StorageOptions> storageOptions, ILogger<SourceService> logger)
        {
            _storageOptions = storageOptions;
            _logger = logger;
        }

        public async Task<bool> CreateContainer(string containerName)
        {
            var blobServiceClient = new BlobServiceClient(_storageOptions.Value.ConnectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync();

            return true;
        }

        public async Task<Uri> UploadFile(string fileName, Stream fileStream)
        {
            var blobServiceClient = new BlobServiceClient(_storageOptions.Value.ConnectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(_storageOptions.Value.CustomMissionContainerName);
            containerClient.CreateIfNotExists();

            var blobClient = containerClient.GetBlobClient(fileName);
            _ = await blobClient.UploadAsync(fileStream, true);
            //var hash = $"0x{BitConverter.ToString(blobProperties.Value.ContentHash).Replace("-", string.Empty)}";
            return blobClient.Uri;
        }

        public Task<Uri> UploadSource(string id, List<MissionTask> tasks)
        {
            var memoryStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(tasks)));

            var taskUri = UploadFile(id, memoryStream);

            return taskUri;
        }

        public List<MissionTask>? GetMissionTasksFromMissionId(string id)
        {
            var blobServiceClient = new BlobServiceClient(_storageOptions.Value.ConnectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(_storageOptions.Value.CustomMissionContainerName);
            containerClient.CreateIfNotExists();

            var blobClient = containerClient.GetBlobClient(id);

            List<MissionTask>? content = null;
            try
            {
                content = blobClient.DownloadContent().Value.Content.ToObjectFromJson<List<MissionTask>>();
            }
            catch (System.Exception)
            {
                return null;
            }

            return content;
        }

        public List<MissionTask>? GetMissionTasksFromURL(string url)
        {
            var blobClient = new BlobClient(new Uri(url));

            List<MissionTask>? content = null;
            try
            {
                content = blobClient.DownloadContent().Value.Content.ToObjectFromJson<List<MissionTask>>();
            }
            catch (System.Exception)
            {
                return null;
            }

            return content;
        }
    }
}
