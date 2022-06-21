﻿using System.Net;
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
                _logger.LogError("Error in ISAR: {msg}", msg);
                throw new MissionException($"Could not start mission with id: {missionId}");
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
                RobotId = robot.Id,
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

        public async Task<HttpResponseMessage> PauseMission()
        {
            var builder = new UriBuilder($"{_isarUri}/schedule/pause-mission");
            var response = await httpClient.PostAsync(builder.ToString(), null);

            if (!response.IsSuccessStatusCode)
            {
                string message = GetLogMessageForFailedIsarRequest(response.StatusCode);
                _logger.LogError("{message}", message);
                throw new MissionException(message);
            }

            return response;
        }

        public async Task<HttpResponseMessage> ResumeMission()
        {
            var builder = new UriBuilder($"{_isarUri}/schedule/resume-mission");
            var response = await httpClient.PostAsync(builder.ToString(), null);

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

        private static string GetLogMessageForFailedIsarRequest(HttpStatusCode statusCode) =>
            (int)statusCode switch
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

    public class IsarStopMissionResponse
    {
        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("stopped")]
        public bool Stopped { get; set; }
    }
}
