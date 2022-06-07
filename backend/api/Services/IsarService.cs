using System.Text.Json;
using System.Text.Json.Serialization;
using Api.Controllers.Models;
using Api.Database.Models;
using Api.Utilities;
using Microsoft.AspNetCore.WebUtilities;

namespace Api.Services
{
    public class IsarService
    {
        private readonly string _isarUri;
        private static readonly HttpClient httpClient = new();
        private readonly ILogger<IsarService> _logger;
        private readonly ReportService _reportService;

        public IsarService(
            IConfiguration configuration,
            ILogger<IsarService> logger,
            ReportService reportService
        )
        {
            _logger = logger;
            _reportService = reportService;
            _isarUri = configuration.GetSection("Isar")["Url"];
        }

        public async Task<Report> StartMission(Robot robot, string missionId)
        {
            string uri = QueryHelpers.AddQueryString(
                $"{_isarUri}/schedule/start-mission",
                "ID",
                missionId
            );

            var response = await httpClient.PostAsync(uri, null);

            if (!response.IsSuccessStatusCode)
            {
                string msg = response.ToString();
                _logger.LogWarning("Error in ISAR: {msg}", msg);
                throw new MissionException($"Could not start mission with id: {missionId}");
            }
            if (response.Content is null)
            {
                throw new MissionException("Could not read content from mission");
            }

            var isarMissionResponse =
                await response.Content.ReadFromJsonAsync<IsarStartMissionResponse>();
            if (isarMissionResponse is null)
                throw new JsonException("Failed to deserialize mission from Isar");

            var tasks = ProcessIsarMissionResponse(isarMissionResponse);

            var report = new Report
            {
                Robot = robot,
                IsarMissionId = isarMissionResponse?.MissionId,
                EchoMissionId = missionId,
                Log = "",
                ReportStatus = ReportStatus.NotStarted,
                StartTime = DateTimeOffset.UtcNow,
                EndTime = DateTimeOffset.UtcNow,
                Tasks = tasks,
            };

            _logger.LogInformation(
                "Mission {missionId} started on robot {robotId}",
                missionId,
                robot.Id
            );
            return await _reportService.Create(report);
        }

        public async Task<HttpResponseMessage> StopMission()
        {
            var builder = new UriBuilder($"{_isarUri}/schedule/stop-mission");
            return await httpClient.PostAsync(builder.ToString(), null);
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
                bool success = Enum.TryParse<StepType>(stepResponse.Type, out var stepType);
                if (!success)
                    throw new JsonException(
                        $"Failed to parse step type. {stepResponse.Type} is not valid"
                    );

                if (stepType != StepType.DriveToPose)
                {
                    _ = SelectInspectionType.FromSensorTypeAsString(stepResponse.Type);
                }

                var step = new IsarStep()
                {
                    IsarStepId = stepResponse.IsarStepId,
                    TagId = taskResponse.TagId,
                    StepStatus = IsarStepStatus.NotStarted,
                    StepType = stepType,
                    Time = DateTimeOffset.UtcNow,
                    FileLocation = "",
                };

                steps.Add(step);
            }

            return steps;
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
