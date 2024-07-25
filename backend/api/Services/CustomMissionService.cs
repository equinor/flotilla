using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Api.Database.Models;
namespace Api.Services
{

    public interface ICustomMissionService
    {
        Task<Source> CreateSourceIfOneDoesNotExist(List<MissionTask> tasks);

        Task<List<MissionTask>?> GetMissionTasksFromSourceId(string id);

        string CalculateHashFromTasks(IList<MissionTask> tasks);
    }

    public class CustomMissionService(ILogger<CustomMissionService> logger, ISourceService sourceService) : ICustomMissionService
    {
        public async Task<Source> CreateSourceIfOneDoesNotExist(List<MissionTask> tasks)
        {
            string json = JsonSerializer.Serialize(tasks);
            string hash = CalculateHashFromTasks(tasks);

            var existingSource = await sourceService.ReadById(hash);

            if (existingSource != null) return existingSource;

            var newSource = await sourceService.Create(
                new Source
                {
                    SourceId = hash,
                    Type = MissionSourceType.Custom,
                    CustomMissionTasks = json
                }
            );

            return newSource;
        }

        public async Task<List<MissionTask>?> GetMissionTasksFromSourceId(string id)
        {
            var existingSource = await sourceService.ReadById(id);
            if (existingSource == null || existingSource.CustomMissionTasks == null) return null;

            try
            {
                var content = JsonSerializer.Deserialize<List<MissionTask>>(existingSource.CustomMissionTasks);

                if (content == null) return null;

                foreach (var task in content)
                {
                    task.Id = Guid.NewGuid().ToString(); // This is needed as tasks are owned by mission runs
                    task.IsarTaskId = Guid.NewGuid().ToString(); // This is needed to update the tasks for the correct mission run
                }
                return content;
            }
            catch (Exception e)
            {
                logger.LogWarning("Unable to deserialize custom mission tasks with ID {Id}. {ErrorMessage}", id, e);
                return null;
            }
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
                taskCopy.Inspections = taskCopy.Inspections.Select(i => new Inspection(i, useEmptyIDs: true)).ToList();
                genericTasks.Add(taskCopy);
            }

            string json = JsonSerializer.Serialize(genericTasks);
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));
            return BitConverter.ToString(hash).Replace("-", "", StringComparison.CurrentCulture).ToUpperInvariant();
        }
    }
}
