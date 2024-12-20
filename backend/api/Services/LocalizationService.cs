using Api.Database.Models;
using Api.Utilities;
namespace Api.Services
{
    public interface ILocalizationService
    {
        public Task EnsureRobotIsOnSameInstallationAsMission(Robot robot, MissionDefinition missionDefinition);
        public Task<bool> RobotIsOnSameInspectionAreaAsMission(string robotId, string inspectionAreaId);
    }

    public class LocalizationService(ILogger<LocalizationService> logger, IRobotService robotService, IInstallationService installationService, IInspectionAreaService inspectionAreaService) : ILocalizationService
    {

        public async Task EnsureRobotIsOnSameInstallationAsMission(Robot robot, MissionDefinition missionDefinition)
        {
            var missionInstallation = await installationService.ReadByInstallationCode(missionDefinition.InstallationCode, readOnly: true);

            if (missionInstallation is null)
            {
                string errorMessage = $"Could not find installation for installation code {missionDefinition.InstallationCode}";
                logger.LogError("{Message}", errorMessage);
                throw new InstallationNotFoundException(errorMessage);
            }

            if (robot.CurrentInstallation.Id != missionInstallation.Id)
            {
                string errorMessage = $"The robot {robot.Name} is on installation {robot.CurrentInstallation.Name} which is not the same as the mission installation {missionInstallation.Name}";
                logger.LogError("{Message}", errorMessage);
                throw new RobotNotInSameInstallationAsMissionException(errorMessage);
            }
        }

        public async Task<bool> RobotIsOnSameInspectionAreaAsMission(string robotId, string inspectionAreaId)
        {
            var robot = await robotService.ReadById(robotId, readOnly: true);
            if (robot is null)
            {
                string errorMessage = $"The robot with ID {robotId} was not found";
                logger.LogError("{Message}", errorMessage);
                throw new RobotNotFoundException(errorMessage);
            }

            if (robot.RobotCapabilities is not null && robot.RobotCapabilities.Contains(RobotCapabilitiesEnum.auto_localize)) { return true; }

            if (robot.CurrentInspectionArea is null)
            {
                const string ErrorMessage = "The robot is not associated with an inspection area and a mission may not be started";
                logger.LogError("{Message}", ErrorMessage);
                throw new RobotCurrentAreaMissingException(ErrorMessage);
            }

            var missionInspectionArea = await inspectionAreaService.ReadById(inspectionAreaId, readOnly: true);
            if (missionInspectionArea is null)
            {
                const string ErrorMessage = "The mission does not have an associated inspection area";
                logger.LogError("{Message}", ErrorMessage);
                throw new InspectionAreaNotFoundException(ErrorMessage);
            }

            if (robot.CurrentInspectionArea is null)
            {
                const string ErrorMessage = "The robot area is not associated with any inspection area";
                logger.LogError("{Message}", ErrorMessage);
                throw new InspectionAreaNotFoundException(ErrorMessage);
            }

            return robot.CurrentInspectionArea.Id == missionInspectionArea.Id;
        }
    }
}
