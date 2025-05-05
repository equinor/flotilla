using Api.Controllers.Models;
using Api.Database.Models;

namespace Api.Services.MissionLoaders
{
    public class CustomMissionLoader(
        IInstallationService installationService,
        IMissionDefinitionService missionDefinitionService,
        ISourceService sourceService
    ) : IMissionLoader
    {
        public async Task<IQueryable<MissionDefinition>> GetAvailableMissions(
            string? installationCode
        )
        {
            return await missionDefinitionService.ReadByInstallationCode(installationCode ?? "");
        }

        public async Task<MissionDefinition?> GetMissionById(string sourceMissionId)
        {
            return await missionDefinitionService.ReadBySourceId(sourceMissionId);
        }

        public async Task<List<MissionTask>?> GetTasksForMission(string missionSourceId)
        {
            return await sourceService.GetMissionTasksFromSourceId(missionSourceId);
        }

        public async Task<List<PlantInfo>> GetPlantInfos()
        {
            var installations = await installationService.ReadAll();
            return [.. installations.Select(i => new PlantInfo(i))];
        }
    }
}
