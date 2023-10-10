using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Api.Controllers.Models;
using Api.Database.Models;
namespace Api.Services
{
    public class MockCustomMissionService : ICustomMissionService
    {
        private static readonly Dictionary<string, List<MissionTask>> mockBlobStore = [];

        public Task<string> UploadSource(List<MissionTask> tasks)
        {
            string hash = CalculateHashFromTasks(tasks);
            mockBlobStore.Add(hash, tasks);
            return Task.FromResult(hash);
        }

        public async Task<List<MissionTask>?> GetMissionTasksFromSourceId(string id)
        {
            if (mockBlobStore.TryGetValue(id, out var value))
            {
                var content = value;
                foreach (var task in content)
                {
                    task.Id = Guid.NewGuid().ToString(); // This is needed as tasks are owned by mission runs
                }
                return content;
            }
            await Task.CompletedTask;
            return null;
        }

        public string CalculateHashFromTasks(IList<MissionTask> tasks)
        {
            IList<MissionTask> genericTasks = tasks.Select(task => new MissionTask(task)).ToList();

            string json = JsonSerializer.Serialize(genericTasks);
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));
            return BitConverter.ToString(hash).Replace("-", "", StringComparison.CurrentCulture).ToUpperInvariant();
        }

        public async Task<MissionRun> QueueCustomMissionRun(CustomMissionQuery customMissionQuery, MissionDefinition customMissionDefinition, Robot robot, IList<MissionTask> missionTasks)
        {
            await Task.CompletedTask;
            return new MissionRun();
        }
    }
}
