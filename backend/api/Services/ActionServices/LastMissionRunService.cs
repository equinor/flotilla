using Api.Database.Models;

namespace Api.Services.ActionServices
{
    public interface ILastMissionRunService
    {
        public Task<MissionDefinition> SetLastMissionRun(
            string missionRunId,
            string missionDefinitionId
        );
    }

    public class LastMissionRunService(IMissionDefinitionService missionDefinitionService)
        : ILastMissionRunService
    {
        public async Task<MissionDefinition> SetLastMissionRun(
            string missionRunId,
            string missionDefinitionId
        )
        {
            return await missionDefinitionService.UpdateLastSuccessfulMissionRun(
                missionRunId,
                missionDefinitionId
            );
        }
    }
}
