using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Api.Controllers.Models;
using Api.Database.Models;
using Api.Utilities;
using Microsoft.Identity.Web;

namespace Api.Services
{
    public interface IIsarService
    {
        public abstract Task<Report> StartMission(
            Robot robot,
            int echoMissionId,
            IsarMissionDefinition missionDefinition
        );

        public abstract Task<IsarStopMissionResponse> StopMission(Robot robot);

        public abstract Task<HttpResponseMessage> PauseMission(Robot robot);

        public abstract Task<HttpResponseMessage> ResumeMission(Robot robot);
    }

    public class IsarService : IIsarService
    {
        public const string ServiceName = "IsarApi";
        private readonly IDownstreamWebApi _isarApi;
        private readonly ILogger<IsarService> _logger;
        private readonly IReportService _reportService;

        public IsarService(
            ILogger<IsarService> logger,
            IReportService reportService,
            IDownstreamWebApi downstreamWebApi
        )
        {
            _logger = logger;
            _reportService = reportService;
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

            return await _isarApi.CallWebApiForAppAsync(
                ServiceName,
                options =>
                {
                    options.HttpMethod = method;
                    options.BaseUrl = isarBaseUri;
                    options.RelativePath = relativeUri;
                },
                content
            );
        }

        public async Task<Report> StartMission(
            Robot robot,
            int echoMissionId,
            IsarMissionDefinition missionDefinition
        )
        {
            var response = await CallApi(
                HttpMethod.Post,
                robot.IsarUri,
                "schedule/start-mission",
                new { mission_definition = missionDefinition }
            );

            if (!response.IsSuccessStatusCode)
            {
                string? message = GetLogMessageForFailedIsarRequest(response.StatusCode);
                _logger.LogError("{message}", message);
                throw new MissionException(message);
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

            var tasks = ProcessIsarMissionResponse(isarMissionResponse);

            var report = new Report
            {
                Robot = robot,
                IsarMissionId = isarMissionResponse?.MissionId,
                EchoMissionId = echoMissionId,
                ReportStatus = ReportStatus.NotStarted,
                StartTime = DateTimeOffset.UtcNow,
                Tasks = tasks,
            };

            _logger.LogInformation(
                "ISAR Mission '{missionId}' started on robot '{robotId}'",
                isarMissionResponse?.MissionId,
                robot.Id
            );
            return await _reportService.Create(report);
        }

        public async Task<IsarStopMissionResponse> StopMission(Robot robot)
        {
            _logger.LogInformation(
                "Stopping mission on robot '{id}' on ISAR at '{uri}'",
                robot.Id,
                robot.IsarUri
            );
            var response = await CallApi(HttpMethod.Post, robot.IsarUri, "schedule/stop-mission");

            if (!response.IsSuccessStatusCode)
            {
                string? message = GetLogMessageForFailedIsarRequest(response.StatusCode);
                _logger.LogError("{message}", message);
                throw new MissionException(message);
            }
            if (response.Content is null)
            {
                _logger.LogError("Could not read content from mission");
                throw new MissionException("Could not read content from mission");
            }

            var isarMissionResponse =
                await response.Content.ReadFromJsonAsync<IsarStopMissionResponse>();
            if (isarMissionResponse is null)
            {
                _logger.LogError("Failed to deserialize mission from ISAR");
                throw new JsonException("Failed to deserialize mission from ISAR");
            }

            return isarMissionResponse;
        }

        public async Task<HttpResponseMessage> PauseMission(Robot robot)
        {
            _logger.LogInformation(
                "Pausing mission on robot '{id}' on ISAR at '{uri}'",
                robot.Id,
                robot.IsarUri
            );
            var response = await CallApi(HttpMethod.Post, robot.IsarUri, "schedule/pause-mission");

            if (!response.IsSuccessStatusCode)
            {
                string message = GetLogMessageForFailedIsarRequest(response.StatusCode);
                _logger.LogError("{message}", message);
                throw new MissionException(message);
            }
            if (response.Content is null)
            {
                _logger.LogError("Could not read content from mission");
                throw new MissionException("Could not read content from mission");
            }

            var isarMissionResponse =
                await response.Content.ReadFromJsonAsync<HttpResponseMessage>();
            if (isarMissionResponse is null)
            {
                _logger.LogError("Failed to deserialize mission from ISAR");
                throw new JsonException("Failed to deserialize mission from ISAR");
            }

            return isarMissionResponse;
        }

        public async Task<HttpResponseMessage> ResumeMission(Robot robot)
        {
            _logger.LogInformation(
                "Resuming mission on robot '{id}' on ISAR at '{uri}'",
                robot.Id,
                robot.IsarUri
            );
            var response = await CallApi(HttpMethod.Post, robot.IsarUri, "schedule/resume-mission");

            if (!response.IsSuccessStatusCode)
            {
                string message = GetLogMessageForFailedIsarRequest(response.StatusCode);
                _logger.LogError("{message}", message);
                throw new MissionException(message);
            }
            if (response.Content is null)
            {
                _logger.LogError("Could not read content from mission");
                throw new MissionException("Could not read content from mission");
            }

            var isarMissionResponse =
                await response.Content.ReadFromJsonAsync<HttpResponseMessage>();
            if (isarMissionResponse is null)
            {
                _logger.LogError("Failed to deserialize mission from ISAR");
                throw new JsonException("Failed to deserialize mission from ISAR");
            }

            return response;
        }

        private static IList<IsarTask> ProcessIsarMissionResponse(
            IsarStartMissionResponse isarMissionResponse
        )
        {
            var tasks = new List<IsarTask>();
            foreach (var taskResponse in isarMissionResponse.Tasks)
            {
                var steps = ProcessIsarTask(taskResponse);

                var task = new IsarTask()
                {
                    IsarTaskId = taskResponse.IsarTaskId,
                    TagId = taskResponse.TagId,
                    TaskStatus = IsarTaskStatus.NotStarted,
                    Time = DateTimeOffset.UtcNow,
                    Steps = steps,
                };

                tasks.Add(task);
            }

            return tasks;
        }

        private static IList<IsarStep> ProcessIsarTask(IsarTaskResponse taskResponse)
        {
            var steps = new List<IsarStep>();

            foreach (var stepResponse in taskResponse.Steps)
            {
                bool success = Enum.TryParse<IsarStep.StepTypeEnum>(
                    stepResponse.Type,
                    out var stepType
                );
                if (!success)
                    throw new JsonException(
                        $"Failed to parse step type. {stepResponse.Type} is not valid"
                    );

                if (stepType != IsarStep.StepTypeEnum.DriveToPose)
                {
                    _ = IsarStep.StepTypeFromString(stepResponse.Type);
                }

                var step = new IsarStep()
                {
                    IsarStepId = stepResponse.IsarStepId,
                    TagId = taskResponse.TagId,
                    StepStatus = IsarStep.IsarStepStatus.NotStarted,
                    StepType = stepType,
                    Time = DateTimeOffset.UtcNow,
                    FileLocation = "",
                };

                steps.Add(step);
            }

            return steps;
        }

        private static string GetLogMessageForFailedIsarRequest(HttpStatusCode statusCode)
        {
            return (int)statusCode switch
            {
                StatusCodes.Status408RequestTimeout
                  => "A timeout ocurred when communicating with the ISAR state machine",
                StatusCodes.Status409Conflict
                  => "A conflict ocurred when interacting with the ISAR state machine. This could imply the state machine is in a state that does not allow the current action you attempted.",
                StatusCodes.Status500InternalServerError
                  => "An internal server error ocurred in ISAR",
                _ => $"An unexpected status code: {statusCode} was received from ISAR"
            };
        }
    }

    public class IsarStopMissionResponse
    {
        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("stopped")]
        public bool Stopped { get; set; }
    }
}
