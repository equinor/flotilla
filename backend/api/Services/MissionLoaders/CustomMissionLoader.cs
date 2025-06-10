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
        public async Task<IQueryable<CondensedMissionDefinition>> GetAvailableMissions(
            string? installationCode
        )
        {
            var missionDefinitions = await missionDefinitionService.ReadByInstallationCode(
                installationCode ?? ""
            );
            return missionDefinitions.Select(m => new CondensedMissionDefinition(m));
        }

        public async Task<CondensedMissionDefinition?> GetMissionById(string sourceMissionId)
        {
            var missionDefinition = await missionDefinitionService.ReadBySourceId(sourceMissionId);
            return missionDefinition != null
                ? new CondensedMissionDefinition(missionDefinition)
                : null;
        }

        public async Task<List<MissionTask>?> GetTasksForMission(string missionSourceId)
        {
            return await sourceService.GetMissionTasksFromSourceId(missionSourceId);
        }

        public async Task<List<PlantInfo>> GetPlantInfos()
        {
            var installations = await installationService.ReadAll();
            return installations.Select(i => new PlantInfo(i)).ToList();
        }
    }
}
