using Api.Controllers.Models;
using Api.Database.Models;
namespace Api.Services.MissionLoaders
{
    public class EchoMissionLoader(
        IEchoService echoService
        ) : IMissionLoader
    {
        public async Task<IQueryable<MissionDefinition>> GetAvailableMissions(string? installationCode)
        {
            return await echoService.GetAvailableMissions(installationCode);
        }

        public async Task<MissionDefinition?> GetMissionById(string sourceMissionId)
        {
            return await echoService.GetMissionById(sourceMissionId);
        }

        public async Task<List<MissionTask>> GetTasksForMission(string missionId)
        {
            return await echoService.GetTasksForMission(missionId);
        }

        public async Task<List<PlantInfo>> GetPlantInfos()
        {
            return await echoService.GetPlantInfos();
        }
    }
}
