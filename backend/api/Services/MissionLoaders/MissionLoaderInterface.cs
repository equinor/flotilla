using Api.Controllers.Models;
using Api.Database.Models;
namespace Api.Services.MissionLoaders
{
    public interface IMissionLoader
    {
        public Task<MissionDefinition?> GetMissionById(string missionId);

        public Task<List<MissionDefinition>> GetAvailableMissions(string? installationCode);

        public Task<List<MissionTask>> GetTasksForMission(string missionId);

        public Task<List<PlantInfo>> GetPlantInfos(); // Facility service
    }
}
