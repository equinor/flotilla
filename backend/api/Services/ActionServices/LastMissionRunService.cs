using Api.Database.Models;
using Api.Utilities;
namespace Api.Services.ActionServices
{
    public interface ILastMissionRunService
    {
        public Task<MissionDefinition> SetLastMissionRun(string missionRunId, string missionDefinitionId);
    }

    public class LastMissionRunService : ILastMissionRunService
    {
        private readonly ILogger<LastMissionRunService> _logger;
        private readonly IMissionDefinitionService _missionDefinitionService;
        private readonly IMissionRunService _missionRunService;

        public LastMissionRunService(ILogger<LastMissionRunService> logger, IMissionDefinitionService missionDefinitionService, IMissionRunService missionRunService)
        {
            _logger = logger;
            _missionDefinitionService = missionDefinitionService;
            _missionRunService = missionRunService;
        }

        public async Task<MissionDefinition> SetLastMissionRun(string missionRunId, string missionDefinitionId)
        {
            var missionRun = await _missionRunService.ReadById(missionRunId);
            if (missionRun is null)
            {
                string errorMessage = $"Mission run {missionRunId} was not found";
                _logger.LogWarning("{Message}", errorMessage);
                throw new MissionNotFoundException(errorMessage);
            }

            var missionDefinition = await _missionDefinitionService.ReadById(missionDefinitionId);
            if (missionDefinition == null)
            {
                string errorMessage = $"Mission definition {missionDefinitionId} was not found";
                _logger.LogWarning("{Message}", errorMessage);
                throw new MissionNotFoundException(errorMessage);
            }

            missionDefinition.LastSuccessfulRun = missionRun;
            return await _missionDefinitionService.Update(missionDefinition);
        }
    }
}
