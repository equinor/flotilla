using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Api.Database.Models;
using Api.Options;
using Microsoft.Extensions.Options;

namespace Api.Services
{

    public interface ICustomMissionService
    {
        string UploadSource(List<MissionTask> tasks);
        Task<List<MissionTask>?> GetMissionTasksFromSourceId(string id);
        string CalculateHashFromTasks(IList<MissionTask> tasks);
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

        public string UploadSource(List<MissionTask> tasks)
        {
            string json = JsonSerializer.Serialize(tasks);
            string hash = CalculateHashFromTasks(tasks);
            _blobService.UploadJsonToBlob(json, hash, _storageOptions.Value.CustomMissionContainerName, _storageOptions.Value.AccountName, false);

            return hash;
        }

        public async Task<List<MissionTask>?> GetMissionTasksFromSourceId(string id)
        {
            List<MissionTask>? content;
            try
            {
                byte[] rawContent = await _blobService.DownloadBlob(id, _storageOptions.Value.CustomMissionContainerName, _storageOptions.Value.AccountName);
                var rawBinaryContent = new BinaryData(rawContent);
                content = rawBinaryContent.ToObjectFromJson<List<MissionTask>>();
                foreach (var task in content)
                {
                    task.Id = Guid.NewGuid().ToString(); // This is needed as tasks are owned by mission runs
                }
            }
            catch (Exception)
            {
                return null;
            }

            return content;
        }

        public string CalculateHashFromTasks(IList<MissionTask> tasks)
        {
            IList<MissionTask> genericTasks = new List<MissionTask>();
            foreach (var task in tasks)
            {
                var taskCopy = new MissionTask(task);
                genericTasks.Add(taskCopy);
            }

            string json = JsonSerializer.Serialize(genericTasks);
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));
            return BitConverter.ToString(hash).Replace("-", "", StringComparison.CurrentCulture).ToUpperInvariant();
        }
    }
}
