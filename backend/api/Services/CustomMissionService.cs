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
        Task<string> UploadSource(List<MissionTask> tasks);

        Task<List<MissionTask>?> GetMissionTasksFromSourceId(string id);

        string CalculateHashFromTasks(IList<MissionTask> tasks);
    }

    public class CustomMissionService(IOptions<StorageOptions> storageOptions, IBlobService blobService) : ICustomMissionService
    {
        public async Task<string> UploadSource(List<MissionTask> tasks)
        {
            string json = JsonSerializer.Serialize(tasks);
            string hash = CalculateHashFromTasks(tasks);
            await blobService.UploadJsonToBlob(json, hash, storageOptions.Value.CustomMissionContainerName, storageOptions.Value.AccountName, false);

            return hash;
        }

        public async Task<List<MissionTask>?> GetMissionTasksFromSourceId(string id)
        {
            List<MissionTask>? content;
            try
            {
                byte[] rawContent = await blobService.DownloadBlob(id, storageOptions.Value.CustomMissionContainerName, storageOptions.Value.AccountName);
                var rawBinaryContent = new BinaryData(rawContent);
                content = rawBinaryContent.ToObjectFromJson<List<MissionTask>>();
                foreach (var task in content)
                {
                    task.Id = Guid.NewGuid().ToString(); // This is needed as tasks are owned by mission runs
                    task.IsarTaskId = Guid.NewGuid().ToString(); // This is needed to update the tasks for the correct mission run
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
            var genericTasks = new List<MissionTask>();
            foreach (var task in tasks)
            {
                var taskCopy = new MissionTask(task)
                {
                    Id = "",
                    IsarTaskId = ""
                };
                genericTasks.Add(taskCopy);
            }

            string json = JsonSerializer.Serialize(genericTasks);
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));
            return BitConverter.ToString(hash).Replace("-", "", StringComparison.CurrentCulture).ToUpperInvariant();
        }
    }
}
