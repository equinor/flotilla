using Api.Database.Models;
using Api.Services.MissionLoaders;

namespace Api.Services
{
    public interface IMissionDefinitionTaskService
    {
        public Task<List<MissionTask>?> GetTasksFromSource(Source source);
    }

    public class MissionDefinitionTaskService(IMissionLoader missionLoader)
        : IMissionDefinitionTaskService
    {
        public async Task<List<MissionTask>?> GetTasksFromSource(Source source)
        {
            return await missionLoader.GetTasksForMission(source.SourceId);
        }
    }
}
