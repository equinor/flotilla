using Api.Database.Models;
using Api.Utilities;

namespace Api.Services
{
    public interface ILocalizationService
    {
        public Task EnsureRobotIsOnSameInstallationAsMission(
            Robot robot,
            MissionDefinition missionDefinition
        );
    }

    public class LocalizationService(
        ILogger<LocalizationService> logger,
        IInstallationService installationService
    ) : ILocalizationService
    {
        public async Task EnsureRobotIsOnSameInstallationAsMission(
            Robot robot,
            MissionDefinition missionDefinition
        )
        {
            var missionInstallation = await installationService.ReadByInstallationCode(
                missionDefinition.InstallationCode,
                readOnly: true
            );

            if (missionInstallation is null)
            {
                string errorMessage =
                    $"Could not find installation for installation code {missionDefinition.InstallationCode}";
                logger.LogError("{Message}", errorMessage);
                throw new InstallationNotFoundException(errorMessage);
            }

            if (robot.CurrentInstallation.Id != missionInstallation.Id)
            {
                string errorMessage =
                    $"The robot {robot.Name} is on installation {robot.CurrentInstallation.Name} which is not the same as the mission installation {missionInstallation.Name}";
                logger.LogError("{Message}", errorMessage);
                throw new RobotNotInSameInstallationAsMissionException(errorMessage);
            }
        }
    }
}
