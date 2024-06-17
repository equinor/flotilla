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
        public Task<IsarMission> StartMission(Robot robot, MissionRun missionRun, bool isFirstMissionInQueue = false);

        public Task<IsarControlMissionResponse> StopMission(Robot robot);

        public Task<IsarControlMissionResponse> PauseMission(Robot robot);

        public Task<IsarControlMissionResponse> ResumeMission(Robot robot);

        public Task<IsarMission> StartMoveArm(Robot robot, string armPosition);
    }

    public class IsarService(IDownstreamApi isarApi, ILogger<IsarService> logger) : IIsarService
    {
        public const string ServiceName = "IsarApi";

        public async Task<IsarMission> StartMission(Robot robot, MissionRun missionRun, bool isFirstMissionInQueue = false)
        {
            var response = await CallApi(
                HttpMethod.Post,
                robot.IsarUri,
                "schedule/start-mission",
                new
                {
                    mission_definition = new IsarMissionDefinition(missionRun, includeStartPose: isFirstMissionInQueue)
                }
            );

            if (!response.IsSuccessStatusCode)
            {
                (string message, int statusCode) = GetErrorDescriptionFoFailedIsarRequest(response);
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

        public async Task<IsarControlMissionResponse> StopMission(Robot robot)
        {
            logger.LogInformation(
                "Stopping mission on robot '{Id}' on ISAR at '{Uri}'",
                robot.Id,
                robot.IsarUri
            );
            var response = await CallApi(HttpMethod.Post, robot.IsarUri, "schedule/stop-mission");

            if (!response.IsSuccessStatusCode)
            {
                (string message, int statusCode) = GetErrorDescriptionFoFailedIsarRequest(response);
                string errorResponse = await response.Content.ReadAsStringAsync();
                if (response.StatusCode == HttpStatusCode.Conflict && errorResponse.Contains("idle", StringComparison.CurrentCultureIgnoreCase))
                {
                    logger.LogError("No mission was running for robot '{Id}", robot.Id);
                    throw new MissionNotFoundException($"No mission was running for robot {robot.Id}");
                }

                logger.LogError("{Message}: {ErrorResponse}", message, errorResponse);
                throw new MissionException(message, statusCode);
            }
            if (response.Content is null)
            {
                logger.LogError("Could not read content from mission");
                throw new MissionException("Could not read content from mission");
            }

            var isarMissionResponse =
                await response.Content.ReadFromJsonAsync<IsarControlMissionResponse>();
            if (isarMissionResponse is null)
            {
                logger.LogError("Failed to deserialize mission from ISAR");
                throw new JsonException("Failed to deserialize mission from ISAR");
            }

            return isarMissionResponse;
        }

        public async Task<IsarControlMissionResponse> PauseMission(Robot robot)
        {
            logger.LogInformation(
                "Pausing mission on robot '{Id}' on ISAR at '{Uri}'",
                robot.Id,
                robot.IsarUri
            );
            var response = await CallApi(HttpMethod.Post, robot.IsarUri, "schedule/pause-mission");

            if (!response.IsSuccessStatusCode)
            {
                (string message, int statusCode) = GetErrorDescriptionFoFailedIsarRequest(response);
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
                await response.Content.ReadFromJsonAsync<IsarControlMissionResponse>();
            if (isarMissionResponse is null)
            {
                logger.LogError("Failed to deserialize mission from ISAR");
                throw new JsonException("Failed to deserialize mission from ISAR");
            }
            return isarMissionResponse;
        }

        public async Task<IsarControlMissionResponse> ResumeMission(Robot robot)
        {
            logger.LogInformation(
                "Resuming mission on robot '{Id}' on ISAR at '{Uri}'",
                robot.Id,
                robot.IsarUri
            );
            var response = await CallApi(HttpMethod.Post, robot.IsarUri, "schedule/resume-mission");

            if (!response.IsSuccessStatusCode)
            {
                (string message, int statusCode) = GetErrorDescriptionFoFailedIsarRequest(response);
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
                await response.Content.ReadFromJsonAsync<IsarControlMissionResponse>();
            if (isarMissionResponse is null)
            {
                logger.LogError("Failed to deserialize mission from ISAR");
                throw new JsonException("Failed to deserialize mission from ISAR");
            }

            return isarMissionResponse;
        }

        public async Task<IsarMission> StartMoveArm(Robot robot, string armPosition)
        {
            string armPositionPath = $"schedule/move_arm/{armPosition}";
            var response = await CallApi(
                HttpMethod.Post,
                robot.IsarUri,
                armPositionPath
            );

            if (!response.IsSuccessStatusCode)
            {
                (string message, int statusCode) = GetErrorDescriptionFoFailedIsarRequest(response);
                string errorResponse = await response.Content.ReadAsStringAsync();
                logger.LogError("{Message}: {ErrorResponse}", message, errorResponse);
                throw new MissionException(message, statusCode);
            }
            if (response.Content is null)
            {
                string errorMessage = "Could not read content from start move arm";
                logger.LogError("{ErrorMessage}", errorMessage);
                throw new MissionException(errorMessage);
            }

            var isarMissionResponse =
                await response.Content.ReadFromJsonAsync<IsarStartMissionResponse>();
            if (isarMissionResponse is null)
            {
                string errorMessage = $"Failed to move arm to '{armPosition}' from ISAR";
                logger.LogError("{ErrorMessage}", errorMessage);
                throw new JsonException(errorMessage);
            }

            var isarMission = new IsarMission(isarMissionResponse);

            logger.LogInformation(
                "ISAR move arm to '{ArmPosition}' started on robot '{RobotId}'",
                armPosition,
                robot.Id
            );
            return isarMission;
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

        private static (string, int) GetErrorDescriptionFoFailedIsarRequest(HttpResponseMessage response)
        {
            var statusCode = response.StatusCode;
            string description = (int)statusCode switch
            {
                StatusCodes.Status408RequestTimeout
                    => "A timeout occurred when communicating with the ISAR state machine",
                StatusCodes.Status409Conflict
                    => "A conflict occurred when interacting with the ISAR state machine. This could imply the state machine is in a state that does not allow the current action you attempted.",
                StatusCodes.Status500InternalServerError
                    => "An internal server error occurred in ISAR",
                StatusCodes.Status401Unauthorized => "Flotilla failed to authorize towards ISAR",
                _ => $"An unexpected status code '{statusCode}' was received from ISAR"
            };

            return (description, (int)statusCode);
        }
    }
}
