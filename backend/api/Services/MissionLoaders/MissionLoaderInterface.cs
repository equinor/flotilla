using Api.Controllers.Models;
using Api.Database.Models;

namespace Api.Services.MissionLoaders
{
    public interface IMissionLoader
    {
        public Task<CondensedMissionDefinition?> GetMissionById(string sourceMissionId);

        public Task<IQueryable<CondensedMissionDefinition>> GetAvailableMissions(
            string? installationCode
        );

        public Task<List<MissionTask>?> GetTasksForMission(string sourceMissionId);
    }
}
