using Api.Controllers.Models;
using Api.Database.Models;

namespace Api.Services.MissionLoaders
{
    public interface IMissionLoader
    {
        public Task<MissionDefinition?> GetMissionById(string sourceMissionId);

        public Task<IQueryable<MissionDefinition>> GetAvailableMissions(string? installationCode);

        public Task<List<MissionTask>?> GetTasksForMission(string sourceMissionId);

        public Task<List<PlantInfo>> GetPlantInfos();
    }
}
