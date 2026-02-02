using Api.Utilities;

namespace Api.Services
{
    public interface IErrorHandlingService
    {
        public Task HandleLosingConnectionToIsar(string robotId);
    }

    public class ErrorHandlingService(
        ILogger<ErrorHandlingService> logger,
        IRobotService robotService,
        IMissionRunService missionRunService
    ) : IErrorHandlingService
    {
        public async Task HandleLosingConnectionToIsar(string robotId)
        {
            try
            {
                await missionRunService.UpdateCurrentRobotMissionToFailed(robotId);
                await robotService.UpdateCurrentMissionId(robotId, null);
            }
            catch (RobotNotFoundException)
            {
                logger.LogError("Robot with ID: {RobotId} was not found in the database", robotId);
            }
        }
    }
}
