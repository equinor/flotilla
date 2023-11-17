using Api.Database.Models;
using Api.Utilities;
namespace Api.Services.ActionServices
{
    public interface ILastMissionRunService
    {
        public Task<MissionDefinition> SetLastMissionRun(string missionRunId, string missionDefinitionId);
    }

    public class LastMissionRunService(ILogger<LastMissionRunService> logger, IMissionDefinitionService missionDefinitionService, IMissionRunService missionRunService) : ILastMissionRunService
    {
        public async Task<MissionDefinition> SetLastMissionRun(string missionRunId, string missionDefinitionId)
        {
            var missionRun = await missionRunService.ReadById(missionRunId);
            if (missionRun is null)
            {
                string errorMessage = $"Mission run {missionRunId} was not found";
                logger.LogWarning("{Message}", errorMessage);
                throw new MissionNotFoundException(errorMessage);
            }

            var missionDefinition = await missionDefinitionService.ReadById(missionDefinitionId);
            if (missionDefinition == null)
            {
                string errorMessage = $"Mission definition {missionDefinitionId} was not found";
                logger.LogWarning("{Message}", errorMessage);
                throw new MissionNotFoundException(errorMessage);
            }

            missionDefinition.LastSuccessfulRun = missionRun;
            return await missionDefinitionService.Update(missionDefinition);
        }
    }
}
