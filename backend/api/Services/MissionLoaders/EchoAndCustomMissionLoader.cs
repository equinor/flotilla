using Api.Controllers.Models;
using Api.Database.Models;

namespace Api.Services.MissionLoaders
{
    public class EchoAndCustomMissionLoader(IEchoService echoService, ISourceService sourceService)
        : IMissionLoader
    {
        public async Task<IQueryable<CondensedMissionDefinition>> GetAvailableMissions(
            string? installationCode
        )
        {
            return await echoService.GetAvailableMissions(installationCode);
        }

        public async Task<CondensedMissionDefinition?> GetMissionById(string sourceMissionId)
        {
            return await echoService.GetMissionById(sourceMissionId);
        }

        public async Task<List<MissionTask>?> GetTasksForMission(string missionSourceId)
        {
            var customMissionTasks = await sourceService.GetMissionTasksFromSourceId(
                missionSourceId
            );
            if (customMissionTasks != null)
            {
                return customMissionTasks;
            }

            return await echoService.GetTasksForMission(missionSourceId);
        }

        public async Task<List<PlantInfo>> GetPlantInfos()
        {
            return await echoService.GetPlantInfos();
        }
    }
}
