using Api.Database.Models;

namespace Api.Services.MissionLoaders
{
    public class EchoMissionLoader(IEchoService echoService) : IMissionLoader
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
            return await echoService.GetTasksForMission(missionSourceId);
        }
    }
}
