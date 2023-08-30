using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Api.Database.Models;

namespace Api.Services
{
    public class MockCustomMissionService : ICustomMissionService
    {
        private static readonly Dictionary<string, List<MissionTask>> mockBlobStore = new();

        public string UploadSource(List<MissionTask> tasks)
        {
            string id = Guid.NewGuid().ToString();
            mockBlobStore.Add(id, tasks);
            return id;
        }

        public async Task<List<MissionTask>?> GetMissionTasksFromSourceId(string id)
        {
            if (mockBlobStore.ContainsKey(id))
            {
                var content = mockBlobStore[id];
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
            string json = JsonSerializer.Serialize(tasks);
            var hasher = SHA256.Create();
            byte[] hash = hasher.ComputeHash(Encoding.UTF8.GetBytes(json));
            return BitConverter.ToString(hash);
        }
    }
}
