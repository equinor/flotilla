using System.Text.Json;
using Api.Database.Models;
using Api.Services.Models;
using Api.Utilities;
using Microsoft.Identity.Web;

namespace Api.Services
{
    public interface IIsarService
    {
        public abstract Task<IsarMission> StartMission(Robot robot, MissionRun missionRun);

        public abstract Task<IsarControlMissionResponse> StopMission(Robot robot);

        public abstract Task<IsarControlMissionResponse> PauseMission(Robot robot);

        public abstract Task<IsarControlMissionResponse> ResumeMission(Robot robot);

        public abstract Task<IsarMission> StartLocalizationMission(Robot robot, Pose localizationMission);

        public abstract Task<IsarMission> StartMoveArm(Robot robot, string armPosition);
    }

    public class IsarService : IIsarService
    {
        public const string ServiceName = "IsarApi";
        private readonly IDownstreamWebApi _isarApi;
        private readonly ILogger<IsarService> _logger;

        public IsarService(ILogger<IsarService> logger, IDownstreamWebApi downstreamWebApi)
        {
            _logger = logger;
            _isarApi = downstreamWebApi;
        }

        /// <summary>
        /// Helper method to call the downstream API
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
            var response = await _isarApi.CallWebApiForAppAsync(
                ServiceName,
                options =>
                {
                    options.HttpMethod = method;
                    options.BaseUrl = isarBaseUri;
                    options.RelativePath = relativeUri;
                },
                content
            );
            return response;
        }

        public async Task<IsarMission> StartMission(Robot robot, MissionRun missionRun)
        {
            var response = await CallApi(
                HttpMethod.Post,
                robot.IsarUri,
                "schedule/start-mission",
                new { mission_definition = new IsarMissionDefinition(missionRun) }
            );

            if (!response.IsSuccessStatusCode)
            {
                var (message, statusCode) = GetErrorDescriptionFoFailedIsarRequest(response);
                string errorResponse = await response.Content.ReadAsStringAsync();
                _logger.LogError("{Message}: {ErrorResponse}", message, errorResponse);
                throw new MissionException(message, statusCode);
            }
            if (response.Content is null)
            {
                _logger.LogError("Could not read content from mission");
                throw new MissionException("Could not read content from mission");
            }

            var isarMissionResponse =
                await response.Content.ReadFromJsonAsync<IsarStartMissionResponse>();
            if (isarMissionResponse is null)
            {
                _logger.LogError("Failed to deserialize mission from ISAR");
                throw new JsonException("Failed to deserialize mission from ISAR");
            }

            var isarMission = new IsarMission(isarMissionResponse);

            _logger.LogInformation(
                "ISAR Mission '{MissionId}' started on robot '{RobotId}'",
                isarMission.IsarMissionId,
                robot.Id
            );
            return isarMission;
        }

        public async Task<IsarControlMissionResponse> StopMission(Robot robot)
        {
            _logger.LogInformation(
                "Stopping mission on robot '{Id}' on ISAR at '{Uri}'",
                robot.Id,
                robot.IsarUri
            );
            var response = await CallApi(HttpMethod.Post, robot.IsarUri, "schedule/stop-mission");

            if (!response.IsSuccessStatusCode)
            {
                var (message, statusCode) = GetErrorDescriptionFoFailedIsarRequest(response);
                string errorResponse = await response.Content.ReadAsStringAsync();
                _logger.LogError("{Message}: {ErrorResponse}", message, errorResponse);
                throw new MissionException(message, statusCode);
            }
            if (response.Content is null)
            {
                _logger.LogError("Could not read content from mission");
                throw new MissionException("Could not read content from mission");
            }

            var isarMissionResponse =
                await response.Content.ReadFromJsonAsync<IsarControlMissionResponse>();
            if (isarMissionResponse is null)
            {
                _logger.LogError("Failed to deserialize mission from ISAR");
                throw new JsonException("Failed to deserialize mission from ISAR");
            }

            return isarMissionResponse;
        }

        public async Task<IsarControlMissionResponse> PauseMission(Robot robot)
        {
            _logger.LogInformation(
                "Pausing mission on robot '{Id}' on ISAR at '{Uri}'",
                robot.Id,
                robot.IsarUri
            );
            var response = await CallApi(HttpMethod.Post, robot.IsarUri, "schedule/pause-mission");

            if (!response.IsSuccessStatusCode)
            {
                var (message, statusCode) = GetErrorDescriptionFoFailedIsarRequest(response);
                string errorResponse = await response.Content.ReadAsStringAsync();
                _logger.LogError("{Message}: {ErrorResponse}", message, errorResponse);
                throw new MissionException(message, statusCode);
            }
            if (response.Content is null)
            {
                _logger.LogError("Could not read content from mission");
                throw new MissionException("Could not read content from mission");
            }

            var isarMissionResponse =
                await response.Content.ReadFromJsonAsync<IsarControlMissionResponse>();
            if (isarMissionResponse is null)
            {
                _logger.LogError("Failed to deserialize mission from ISAR");
                throw new JsonException("Failed to deserialize mission from ISAR");
            }
            return isarMissionResponse;
        }

        public async Task<IsarControlMissionResponse> ResumeMission(Robot robot)
        {
            _logger.LogInformation(
                "Resuming mission on robot '{Id}' on ISAR at '{Uri}'",
                robot.Id,
                robot.IsarUri
            );
            var response = await CallApi(HttpMethod.Post, robot.IsarUri, "schedule/resume-mission");

            if (!response.IsSuccessStatusCode)
            {
                var (message, statusCode) = GetErrorDescriptionFoFailedIsarRequest(response);
                string errorResponse = await response.Content.ReadAsStringAsync();
                _logger.LogError("{Message}: {ErrorResponse}", message, errorResponse);
                throw new MissionException(message, statusCode);
            }
            if (response.Content is null)
            {
                _logger.LogError("Could not read content from mission");
                throw new MissionException("Could not read content from mission");
            }

            var isarMissionResponse =
                await response.Content.ReadFromJsonAsync<IsarControlMissionResponse>();
            if (isarMissionResponse is null)
            {
                _logger.LogError("Failed to deserialize mission from ISAR");
                throw new JsonException("Failed to deserialize mission from ISAR");
            }

            return isarMissionResponse;
        }

        public async Task<IsarMission> StartLocalizationMission(Robot robot, Pose localizationPose)
        {
            var response = await CallApi(
                HttpMethod.Post,
                robot.IsarUri,
                "schedule/start-localization-mission",
                new { localization_pose = new IsarPose(localizationPose) }
            );

            if (!response.IsSuccessStatusCode)
            {
                var (message, statusCode) = GetErrorDescriptionFoFailedIsarRequest(response);
                string errorResponse = await response.Content.ReadAsStringAsync();
                _logger.LogError("{Message}: {ErrorResponse}", message, errorResponse);
                throw new MissionException(message, statusCode);
            }
            if (response.Content is null)
            {
                _logger.LogError("Could not read content from localization mission");
                throw new MissionException("Could not read content from localization mission");
            }

            var isarMissionResponse =
                await response.Content.ReadFromJsonAsync<IsarStartMissionResponse>();
            if (isarMissionResponse is null)
            {
                _logger.LogError("Failed to deserialize localization mission from ISAR");
                throw new JsonException("Failed to deserialize localization mission from ISAR");
            }

            var isarMission = new IsarMission(isarMissionResponse);

            _logger.LogInformation(
                "ISAR Localization Mission '{MissionId}' started on robot '{RobotId}'",
                isarMission.IsarMissionId,
                robot.Id
            );
            return isarMission;
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
                var (message, statusCode) = GetErrorDescriptionFoFailedIsarRequest(response);
                string errorResponse = await response.Content.ReadAsStringAsync();
                _logger.LogError("{Message}: {ErrorResponse}", message, errorResponse);
                throw new MissionException(message, statusCode);
            }
            if (response.Content is null)
            {
                string errorMessage = "Could not read content from start move arm";
                _logger.LogError("{ErrorMessage}", errorMessage);
                throw new MissionException(errorMessage);
            }

            var isarMissionResponse =
                await response.Content.ReadFromJsonAsync<IsarStartMissionResponse>();
            if (isarMissionResponse is null)
            {
                string errorMessage = $"Failed to move arm to '{armPosition}' from ISAR";
                _logger.LogError("{ErrorMessage}", errorMessage);
                throw new JsonException(errorMessage);
            }

            var isarMission = new IsarMission(isarMissionResponse);

            _logger.LogInformation(
                "ISAR move arm to '{ArmPosition}' started on robot '{RobotId}'",
                armPosition,
                robot.Id
            );
            return isarMission;
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
