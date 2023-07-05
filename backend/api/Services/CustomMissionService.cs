

using System.Text.Json;
using Api.Database.Models;
using Api.Options;
using Microsoft.Extensions.Options;

namespace Api.Services
{

    public interface ICustomMissionService
    {
        Task<string> UploadSource(List<MissionTask> tasks);
        Task<List<MissionTask>?> GetMissionTasksFromMissionId(string id);
    }

    public class CustomMissionService : ICustomMissionService
    {
        private readonly IOptions<StorageOptions> _storageOptions;
        private readonly IBlobService _blobService;

        public CustomMissionService(IOptions<StorageOptions> storageOptions, IBlobService blobService)
        {
            _storageOptions = storageOptions;
            _blobService = blobService;
        }

        public async Task<string> UploadSource(List<MissionTask> tasks)
        {
            string json = JsonSerializer.Serialize(tasks);
            string id = Guid.NewGuid().ToString();
            _blobService.UploadJsonToBlob(json, id, _storageOptions.Value.CustomMissionContainerName, _storageOptions.Value.AccountName, false);

            return id;
        }

        public async Task<List<MissionTask>?> GetMissionTasksFromMissionId(string id)
        {
            List<MissionTask>? content;
            try
            {
                byte[] rawContent = await _blobService.DownloadBlob(id, _storageOptions.Value.CustomMissionContainerName, _storageOptions.Value.AccountName);
                var rawBinaryContent = new BinaryData(rawContent);
                content = rawBinaryContent.ToObjectFromJson<List<MissionTask>>();
            }
            catch (Exception)
            {
                return null;
            }

            return content;
        }
    }
}
