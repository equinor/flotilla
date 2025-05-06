using System.Text.Json;
using Api.Database.Models;

namespace Api.Services
{
    public interface IAutoScheduleService
    {
        public Dictionary<TimeOnly, string> DeserializeAutoScheduleJobs(
            MissionDefinition missionDefinition
        );
        public void ReportSkipAutoScheduleToSignalR(
            string message,
            MissionDefinition missionDefinition
        );
        public void ReportAutoScheduleFailToSignalR(
            string message,
            MissionDefinition missionDefinition
        );
    }

    public class AutoScheduleService(
        ILogger<AutoScheduleService> logger,
        ISignalRService signalRService
    ) : IAutoScheduleService
    {
        public Dictionary<TimeOnly, string> DeserializeAutoScheduleJobs(
            MissionDefinition missionDefinition
        )
        {
            var existingJobs = new Dictionary<TimeOnly, string>();
            try
            {
                existingJobs =
                    JsonSerializer.Deserialize<Dictionary<TimeOnly, string>>(
                        missionDefinition.AutoScheduleFrequency?.AutoScheduledJobs ?? "{}"
                    ) ?? new();
            }
            catch (JsonException ex)
            {
                logger.LogError($"JSON deserialization failed: {ex.Message}");
            }
            catch (Exception ex)
            {
                logger.LogError($"An unexpected error occurred: {ex.Message}");
            }

            return existingJobs;
        }

        public void ReportSkipAutoScheduleToSignalR(
            string message,
            MissionDefinition missionDefinition
        )
        {
            logger.LogInformation(message);
            signalRService.ReportAutoScheduleToSignalR(
                "skipAutoMission",
                missionDefinition.Name,
                message,
                missionDefinition.InstallationCode
            );
        }

        public void ReportAutoScheduleFailToSignalR(
            string message,
            MissionDefinition missionDefinition
        )
        {
            logger.LogError(message);

            signalRService.ReportAutoScheduleToSignalR(
                "AutoScheduleFail",
                missionDefinition.Name,
                message,
                missionDefinition.InstallationCode
            );
        }
    }
}
