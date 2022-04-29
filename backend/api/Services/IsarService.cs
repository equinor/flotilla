using System.Text.Json.Serialization;
using Api.Models;
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

        public IsarService(IConfiguration configuration, ILogger<IsarService> logger, ReportService reportService)
        {
            _logger = logger;
            _reportService = reportService;
            _isarUri = configuration.GetSection("Isar")["Url"];
        }

        public async Task<Report> StartMission(Robot robot, string missionId)
        {
            string uri = QueryHelpers.AddQueryString($"{_isarUri}/schedule/start-mission", "ID", missionId);
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
            var responseContent = await response.Content.ReadFromJsonAsync<IsarStartMissionResponse>();
            var report = new Report
            {
                EchoMissionId = "1",
                IsarMissionId = responseContent?.MissionId,
                StartTime = DateTimeOffset.UtcNow,
                EndTime = DateTimeOffset.UtcNow,
                ReportStatus = ReportStatus.InProgress,
                Robot = robot,
                Log = "log"
            };
            _logger.LogInformation("Mission {missionId} started on robot {robotId}", missionId, robot.Id);
            return await _reportService.Create(report);
        }

        public async Task<HttpResponseMessage> StopMission()
        {
            var builder = new UriBuilder($"{_isarUri}/schedule/stop-mission");
            return await httpClient.PostAsync(builder.ToString(), null);
        }

    }

    public class IsarStartMissionResponse
    {
        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("started")]
        public bool Started { get; set; }

        [JsonPropertyName("mission_id")]
        public string? MissionId { get; set; }
    }

    public class IsarStopMissionResponse
    {
        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("stopped")]
        public bool Stopped { get; set; }
    }
}
