using System;
using System.Collections.Generic;
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

        public async Task<List<MissionTask>?> GetMissionTasksFromMissionId(string id)
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
            return null;
        }
    }
}
