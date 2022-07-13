using Api.Controllers.Models;
using Api.Database.Models;
using Api.Utilities;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Api.Services
{
    public class IsarService
    {
        private static readonly HttpClient httpClient = new();
        private readonly ILogger<IsarService> _logger;
        private readonly ReportService _reportService;

        public IsarService(
            ILogger<IsarService> logger,
            ReportService reportService
        )
        {
            _logger = logger;
            _reportService = reportService;
        }

        public async Task<Report> StartMission(Robot robot, IsarMissionDefinition missionDefinition)
        {
            string uri = $"{robot.IsarUri}/schedule/start-mission";

            var response = await httpClient.PostAsync(uri, JsonContent.Create(new { mission_definition = missionDefinition }));

            if (!response.IsSuccessStatusCode)
            {
                string? msg = await response.Content.ReadAsStringAsync();
                _logger.LogError("Error from ISAR: {code} {error} - {msg}", (int)response.StatusCode, response.StatusCode.ToString(), msg);
                throw new MissionException($"Could not start mission for robot '{robot.Id}': {msg}");
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
                _logger.LogError("Could not read content from mission");
                throw new JsonException("Failed to deserialize mission from Isar");
            }

            var tasks = ProcessIsarMissionResponse(isarMissionResponse);

            var report = new Report
            {
                Robot = robot,
                IsarMissionId = isarMissionResponse?.MissionId,
                Log = "",
                ReportStatus = ReportStatus.NotStarted,
                StartTime = DateTimeOffset.UtcNow,
                Tasks = tasks,
            };

            _logger.LogInformation(
                "ISAR Mission '{missionId}' started on robot '{robotId}'",
                isarMissionResponse.MissionId,
                robot.Id
            );
            return await _reportService.Create(report);
        }

        public async Task<HttpResponseMessage> StopMission(Robot robot)
        {
            string url = new UriBuilder($"{robot.IsarUri}/schedule/stop-mission").ToString();
            _logger.LogInformation("Stopping mission on robot '{id}' on ISAR at '{uri}'", robot.Id, url);
            return await httpClient.PostAsync(url, null);
        }

        public async Task<HttpResponseMessage> PauseMission(Robot robot)
        {
            string url = new UriBuilder($"{robot.IsarUri}/schedule/pause-mission").ToString();
            _logger.LogInformation("Pausing mission on robot '{id}' on ISAR at '{uri}'", robot.Id, url);
            var response = await httpClient.PostAsync(url, null);

            if (!response.IsSuccessStatusCode)
            {
                string message = GetLogMessageForFailedIsarRequest(response.StatusCode);
                _logger.LogError("{message}", message);
                throw new MissionException(message);
            }

            return response;
        }

        public async Task<HttpResponseMessage> ResumeMission(Robot robot)
        {
            string url = new UriBuilder($"{robot.IsarUri}/schedule/resume-mission").ToString();
            _logger.LogInformation("Resuming mission on robot '{id}' on ISAR at '{uri}'", robot.Id, url);
            var response = await httpClient.PostAsync(url, null);

            if (!response.IsSuccessStatusCode)
            {
                string message = GetLogMessageForFailedIsarRequest(response.StatusCode);
                _logger.LogError("{message}", message);
                throw new MissionException(message);
            }

            return response;
        }

        public IList<IsarTask> ProcessIsarMissionResponse(
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

        public IList<IsarStep> ProcessIsarTask(IsarTaskResponse taskResponse)
        {
            var steps = new List<IsarStep>();

            foreach (var stepResponse in taskResponse.Steps)
            {
                bool success = Enum.TryParse<IsarStep.StepTypeEnum>(stepResponse.Type, out var stepType);
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
