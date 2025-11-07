using System.Net;
using System.Text.Json;
using Api.Database.Models;
using Api.Services.Models;
using Api.Utilities;
using Microsoft.Identity.Abstractions;

namespace Api.Services
{
    public interface IIsarService
    {
        public Task<IsarMission> StartMission(Robot robot, MissionRun missionRun);

        public Task ReturnHome(Robot robot);

        public Task StopMission(Robot robot, string? missionId = "");

        public Task PauseMission(Robot robot);

        public Task ResumeMission(Robot robot);

        public Task<MediaConfig?> GetMediaStreamConfig(Robot robot);

        public Task ReleaseInterventionNeeded(string robotIsarUri);

        public Task SendToLockdown(string robotIsarUri);

        public Task ReleaseFromLockdown(string robotIsarUri);

        public Task SetMaintenanceMode(string robotIsarUri);

        public Task ReleaseMaintenanceMode(string robotIsarUri);
    }

    public class IsarService(IDownstreamApi isarApi, ILogger<IsarService> logger) : IIsarService
    {
        public const string ServiceName = "IsarApi";

        public async Task<IsarMission> StartMission(Robot robot, MissionRun missionRun)
        {
            var isarMissionDefinition = new
            {
                mission_definition = new IsarMissionDefinition(missionRun),
            };

            HttpResponseMessage? response;
            try
            {
                response = await CallApi(
                    HttpMethod.Post,
                    robot.IsarUri,
                    "schedule/start-mission",
                    isarMissionDefinition
                );
            }
            catch (Exception e)
            {
                logger.LogError(
                    "Encountered an exception when making an API call to ISAR: {Message}",
                    e.Message
                );
                throw new IsarCommunicationException(e.Message);
            }

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.Conflict)
                    throw new RobotBusyException("Robot was not available when starting mission");

                (string message, int statusCode) = GetErrorDescriptionForFailedIsarRequest(
                    response
                );
                string errorResponse = await response.Content.ReadAsStringAsync();
                logger.LogError("{Message}: {ErrorResponse}", message, errorResponse);
                throw new MissionException(message, statusCode);
            }
            if (response.Content is null)
            {
                logger.LogError("Could not read content from mission");
                throw new MissionException("Could not read content from mission");
            }

            var isarMissionResponse =
                await response.Content.ReadFromJsonAsync<IsarStartMissionResponse>();
            if (isarMissionResponse is null)
            {
                logger.LogError("Failed to deserialize mission from ISAR");
                throw new JsonException("Failed to deserialize mission from ISAR");
            }

            var isarMission = new IsarMission(isarMissionResponse);

            logger.LogInformation(
                "ISAR Mission '{MissionId}' started on robot '{RobotId}'",
                isarMission.IsarMissionId,
                robot.Id
            );
            return isarMission;
        }

        public async Task ReturnHome(Robot robot)
        {
            HttpResponseMessage? response;
            try
            {
                response = await CallApi(HttpMethod.Post, robot.IsarUri, "schedule/return-home");
            }
            catch (Exception e)
            {
                logger.LogError(
                    "Encountered an exception when making an API call to ISAR: {Message}",
                    e.Message
                );
                throw new IsarCommunicationException(e.Message);
            }

            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                logger.LogWarning(
                    "Could not return robot {id} home as ISAR reported that it was in an invalid state",
                    robot.Id
                );
                return;
            }

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.Conflict)
                    throw new RobotBusyException("Robot was not available when starting mission");

                (string message, int statusCode) = GetErrorDescriptionForFailedIsarRequest(
                    response
                );
                string errorResponse = await response.Content.ReadAsStringAsync();
                logger.LogError("{Message}: {ErrorResponse}", message, errorResponse);
                throw new MissionException(message, statusCode);
            }
        }

        public async Task StopMission(Robot robot, string? missionId = "")
        {
            logger.LogInformation(
                "Stopping mission on robot '{Id}' on ISAR at '{Uri}'",
                robot.Id,
                robot.IsarUri
            );

            var content = new { mission_id = new IsarStopMissionDefinition(missionId) };

            var response = await CallApi(
                HttpMethod.Post,
                robot.IsarUri,
                "schedule/stop-mission",
                content
            );

            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                logger.LogWarning(
                    "Did not stop robot with Id {id} as it reported that it did not have a mission or that it was already stopping",
                    robot.Id
                );
                return;
            }

            if (!response.IsSuccessStatusCode)
            {
                (string message, int statusCode) = GetErrorDescriptionForFailedIsarRequest(
                    response
                );
                string errorResponse = await response.Content.ReadAsStringAsync();
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    logger.LogError($"No mission found for the mission_id sent: {missionId}");
                }
                if (response.StatusCode == HttpStatusCode.Conflict)
                {
                    logger.LogError("No mission found to stop");
                }

                logger.LogError("{Message}: {ErrorResponse}", message, errorResponse);
                throw new MissionStoppedException(message, statusCode);
            }
        }

        public async Task PauseMission(Robot robot)
        {
            logger.LogInformation(
                "Pausing mission on robot '{Id}' on ISAR at '{Uri}'",
                robot.Id,
                robot.IsarUri
            );
            var response = await CallApi(HttpMethod.Post, robot.IsarUri, "schedule/pause-mission");

            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                logger.LogWarning(
                    "Did not pause robot with Id {id} as it was either already paused, or did not have a mission",
                    robot.Id
                );
                return;
            }

            if (!response.IsSuccessStatusCode)
            {
                (string message, int statusCode) = GetErrorDescriptionForFailedIsarRequest(
                    response
                );
                string errorResponse = await response.Content.ReadAsStringAsync();
                logger.LogError("{Message}: {ErrorResponse}", message, errorResponse);
                throw new MissionPauseException(message, statusCode);
            }
        }

        public async Task ResumeMission(Robot robot)
        {
            logger.LogInformation(
                "Resuming mission on robot '{Id}' on ISAR at '{Uri}'",
                robot.Id,
                robot.IsarUri
            );
            var response = await CallApi(HttpMethod.Post, robot.IsarUri, "schedule/resume-mission");

            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                logger.LogWarning(
                    "Did not resume robot with Isar Id {id} to dock as it is no longer running a misson",
                    robot.Id
                );
                return;
            }

            if (!response.IsSuccessStatusCode)
            {
                (string message, int statusCode) = GetErrorDescriptionForFailedIsarRequest(
                    response
                );
                string errorResponse = await response.Content.ReadAsStringAsync();
                logger.LogError("{Message}: {ErrorResponse}", message, errorResponse);
                throw new MissionResumeException(message, statusCode);
            }
        }

        public async Task ReleaseInterventionNeeded(string robotIsarUri)
        {
            HttpResponseMessage? response;
            try
            {
                response = await CallApi(
                    HttpMethod.Post,
                    robotIsarUri,
                    "schedule/release-intervention-needed"
                );
            }
            catch (Exception e)
            {
                logger.LogError(
                    "Encountered an exception when making an API call to ISAR: {Message}",
                    e.Message
                );
                throw new IsarCommunicationException(e.Message);
            }

            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                logger.LogWarning(
                    "Did not release intervention needed for robot with Isar Uri {uri} to dock as it was reported as already doing so",
                    robotIsarUri
                );
                return;
            }

            if (!response.IsSuccessStatusCode)
            {
                (string message, int statusCode) = GetErrorDescriptionForFailedIsarRequest(
                    response
                );
                string errorResponse = await response.Content.ReadAsStringAsync();
                logger.LogError("{Message}: {ErrorResponse}", message, errorResponse);
                throw new IsarCommunicationException(message);
            }
        }

        public async Task SendToLockdown(string robotIsarUri)
        {
            HttpResponseMessage? response;
            try
            {
                response = await CallApi(HttpMethod.Post, robotIsarUri, "schedule/lockdown");
            }
            catch (Exception e)
            {
                logger.LogError(
                    "Encountered an exception when making an API call to ISAR: {Message}",
                    e.Message
                );
                throw new IsarCommunicationException(e.Message);
            }

            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                logger.LogWarning(
                    "Did not send robot with Isar Uri {uri} to lockdown as it was reported as already doing so",
                    robotIsarUri
                );
                return;
            }

            if (!response.IsSuccessStatusCode)
            {
                (string message, int statusCode) = GetErrorDescriptionForFailedIsarRequest(
                    response
                );
                string errorResponse = await response.Content.ReadAsStringAsync();
                logger.LogError("{Message}: {ErrorResponse}", message, errorResponse);
                throw new IsarCommunicationException(message);
            }
        }

        public async Task ReleaseFromLockdown(string robotIsarUri)
        {
            HttpResponseMessage? response;
            try
            {
                response = await CallApi(
                    HttpMethod.Post,
                    robotIsarUri,
                    "schedule/release-lockdown"
                );
            }
            catch (Exception e)
            {
                logger.LogError(
                    "Encountered an exception when making an API call to ISAR: {Message}",
                    e.Message
                );
                throw new IsarCommunicationException(e.Message);
            }

            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                logger.LogWarning(
                    "Did not release robot with Isar Uri {uri} from lockdown as it was reported as already doing so",
                    robotIsarUri
                );
                return;
            }

            if (!response.IsSuccessStatusCode)
            {
                (string message, int statusCode) = GetErrorDescriptionForFailedIsarRequest(
                    response
                );
                string errorResponse = await response.Content.ReadAsStringAsync();
                logger.LogError("{Message}: {ErrorResponse}", message, errorResponse);
                throw new IsarCommunicationException(message);
            }
        }

        public async Task SetMaintenanceMode(string robotIsarUri)
        {
            HttpResponseMessage? response;
            try
            {
                response = await CallApi(
                    HttpMethod.Post,
                    robotIsarUri,
                    "schedule/maintenance-mode"
                );
            }
            catch (Exception e)
            {
                logger.LogError(
                    "Encountered an exception when making an API call to ISAR: {Message}",
                    e.Message
                );
                throw new IsarCommunicationException(e.Message);
            }

            if (!response.IsSuccessStatusCode)
            {
                (string message, int statusCode) = GetErrorDescriptionForFailedIsarRequest(
                    response
                );
                string errorResponse = await response.Content.ReadAsStringAsync();
                logger.LogError("{Message}: {ErrorResponse}", message, errorResponse);
                throw new IsarCommunicationException(message);
            }
        }

        public async Task ReleaseMaintenanceMode(string robotIsarUri)
        {
            HttpResponseMessage? response;
            try
            {
                response = await CallApi(
                    HttpMethod.Post,
                    robotIsarUri,
                    "schedule/release-maintenance-mode"
                );
            }
            catch (Exception e)
            {
                logger.LogError(
                    "Encountered an exception when making an API call to ISAR: {Message}",
                    e.Message
                );
                throw new IsarCommunicationException(e.Message);
            }

            if (!response.IsSuccessStatusCode)
            {
                (string message, int statusCode) = GetErrorDescriptionForFailedIsarRequest(
                    response
                );
                string errorResponse = await response.Content.ReadAsStringAsync();
                logger.LogError("{Message}: {ErrorResponse}", message, errorResponse);
                throw new IsarCommunicationException(message);
            }
        }

        /// <summary>
        ///     Helper method to call the downstream API
        /// </summary>
        /// <param name="method"> The HttpMethod to use</param>
        /// <param name="isarBaseUri">The base uri from ISAR (Should come from robot object)</param>
        /// <param name="relativeUri">The endpoint at ISAR (Ex: schedule/start-mission) </param>
        /// <param name="contentObject">The object to send in a post method call</param>
        /// <returns></returns>
        private async Task<HttpResponseMessage> CallApi(
            HttpMethod method,
            string isarBaseUri,
            string relativeUri,
            object? contentObject = null
        )
        {
            var content = contentObject is null
                ? null
                : new StringContent(
                    JsonSerializer.Serialize(contentObject),
                    null,
                    "application/json"
                );
            var response = await isarApi.CallApiForAppAsync(
                ServiceName,
                options =>
                {
                    options.HttpMethod = method.Method;
                    options.BaseUrl = isarBaseUri;
                    options.RelativePath = relativeUri;
                },
                content
            );
            return response;
        }

        private static (string, int) GetErrorDescriptionForFailedIsarRequest(
            HttpResponseMessage response
        )
        {
            var statusCode = response.StatusCode;
            string description = (int)statusCode switch
            {
                StatusCodes.Status408RequestTimeout =>
                    "A timeout occurred when communicating with the ISAR state machine",
                StatusCodes.Status409Conflict =>
                    "A conflict occurred when interacting with the ISAR state machine. This could imply the state machine is in a state that does not allow the current action you attempted.",
                StatusCodes.Status500InternalServerError =>
                    "An internal server error occurred in ISAR",
                StatusCodes.Status401Unauthorized => "Flotilla failed to authorize towards ISAR",
                _ => $"An unexpected status code '{statusCode}' was received from ISAR",
            };

            return (description, (int)statusCode);
        }

        public async Task<MediaConfig?> GetMediaStreamConfig(Robot robot)
        {
            string mediaStreamPath = "/media/media-stream-config";
            var response = await CallApi(HttpMethod.Get, robot.IsarUri, mediaStreamPath);

            if (!response.IsSuccessStatusCode)
            {
                (string message, _) = GetErrorDescriptionForFailedIsarRequest(response);
                string errorResponse = await response.Content.ReadAsStringAsync();
                logger.LogError("{Message}: {ErrorResponse}", message, errorResponse);
                throw new ConfigException(message);
            }

            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                logger.LogDebug(
                    $"Robot with id {robot.Id} did not return any content for media stream config. This is likely because the robot doesn't have a media stream."
                );
                return null;
            }

            IsarMediaConfigMessage? isarMediaConfigResponse;
            try
            {
                isarMediaConfigResponse =
                    await response.Content.ReadFromJsonAsync<IsarMediaConfigMessage>();
            }
            catch (JsonException)
            {
                string errorMessage =
                    $"Could not parse content from new robot media stream config. {await response.Content.ReadAsStringAsync()}";
                logger.LogError("{ErrorMessage}", errorMessage);
                throw new ConfigException(errorMessage);
            }

            if (isarMediaConfigResponse == null)
            {
                string errorMessage =
                    $"Parsing of robot media stream config resulted in empty config. {await response.Content.ReadAsStringAsync()}";
                logger.LogError("{ErrorMessage}", errorMessage);
                throw new ConfigException(errorMessage);
            }

            bool parseSuccess = Enum.TryParse(
                isarMediaConfigResponse.MediaConnectionType,
                out MediaConnectionType connectionType
            );

            if (!parseSuccess)
            {
                string errorMessage =
                    $"Could not parse connection type from new robot media stream config. {isarMediaConfigResponse.MediaConnectionType}";
                logger.LogError("{ErrorMessage}", errorMessage);
                throw new ConfigException(errorMessage);
            }

            return new MediaConfig
            {
                Url = isarMediaConfigResponse.Url,
                Token = isarMediaConfigResponse.Token,
                RobotId = robot.Id,
                MediaConnectionType = connectionType,
            };
        }
    }
}
