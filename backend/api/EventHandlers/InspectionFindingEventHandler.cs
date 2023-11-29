using System.Globalization;
using System.Text;
using Api.Database.Models;
using Api.Services;
namespace Api.EventHandlers
{
    public class InspectionFindingEventHandler(IConfiguration configuration,
    IServiceScopeFactory scopeFactory,
    ILogger<InspectionFindingEventHandler> logger) : BackgroundService
    {
        private readonly TimeSpan _interval = configuration.GetValue<TimeSpan>("InspectionFindingEventHandler:Interval");
        private InspectionFindingService InspectionFindingService => scopeFactory.CreateScope().ServiceProvider.GetRequiredService<InspectionFindingService>();
        private readonly TimeSpan _timeSpan = configuration.GetValue<TimeSpan>("InspectionFindingEventHandler:TimeSpan");

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_interval, stoppingToken);

                    var lastReportingTime = DateTime.UtcNow - _timeSpan;

                    var inspectionFindings = await InspectionFindingService.RetrieveInspectionFindings(lastReportingTime);

                    logger.LogInformation("Found {count} inspection findings in the last {interval}.", inspectionFindings.Count, _timeSpan);

                    if (inspectionFindings.Count > 0)
                    {
                        var findingsList = await GenerateFindingsList(inspectionFindings);

                        string messageString = GenerateReportFromFindingsReportsList(findingsList);

                        string adaptiveCardJson = GenerateAdaptiveCard(messageString);
                    }
                }
                catch (OperationCanceledException) { throw; }
            }
        }
        private async Task<List<Finding>> GenerateFindingsList(List<InspectionFinding> inspectionFindings)
        {
            var findingsList = new List<Finding>();

            foreach (var inspectionFinding in inspectionFindings)
            {
                try
                {
                    var missionRun = await InspectionFindingService.GetMissionRunByIsarStepId(inspectionFinding);
                    var task = await InspectionFindingService.GetMissionTaskByIsarStepId(inspectionFinding);

                    if (task?.TagId != null && missionRun?.Area?.Plant?.Name != null && missionRun?.Area?.Name != null)
                    {
                        var finding = new Finding(
                            task.TagId,
                            missionRun.Area.Plant.Name,
                            missionRun.Area.Name,
                            inspectionFinding.Finding,
                            inspectionFinding.InspectionDate,
                            missionRun.Robot.Name
                        );

                        findingsList.Add(finding);
                    }
                    else
                    {
                        logger.LogInformation("Failed to generate a finding since TagId in missionTask or Area in MissionRun is null");
                        continue;
                    }
                }
                catch { throw; }
            }
            return findingsList;
        }

        public static string GenerateReportFromFindingsReportsList(List<Finding> findingsReports)
        {
            var reportBuilder = new StringBuilder("Findings Report:");
            reportBuilder.Append(Environment.NewLine);
            string dateFormat = "dd/MM/yyyy HH:mm:ss";
            var formatProvider = CultureInfo.InvariantCulture;
            foreach (var finding in findingsReports)
            {
                _ = reportBuilder.AppendLine(
                    formatProvider,
                    $"- TagId: {finding.TagId}, PlantName: {finding.PlantName}, AreaName: {finding.AreaName}, Description: {finding.FindingDescription}, Timestamp: {finding.Timestamp.ToString(dateFormat, formatProvider)}, RobotName: {finding.RobotName}"
                    );
            }
            return reportBuilder.ToString();
        }

        public static string GenerateAdaptiveCard(string messageContent)
        {
            string adaptiveCardJson = $@"{{
                        ""type"": ""message"",
                        ""attachments"": [
                            {{
                                ""contentType"": ""application/vnd.microsoft.card.adaptive"",
                                ""content"": {{
                                    ""type"": ""AdaptiveCard"",
                                    ""body"": [
                                        {{
                                            ""type"": ""Container"",
                                            ""height"": ""stretch"",
                                            ""items"": [
                                                {{
                                                    ""type"": ""TextBlock"",
                                                    ""text"": ""{messageContent}"",
                                                    ""wrap"": true
                                                }}
                                            ]
                                        }}
                                    ],
                                    ""$schema"": ""http://adaptivecards.io/schemas/adaptive-card.json"",
                                    ""version"": ""1.0"",
                                    ""msteams"": {{
                                        ""displayStyle"": ""full""
                                    }}
                                }}
                            }}
                        ]
                    }}";
            return adaptiveCardJson;
        }
    }

    public class Finding(string tagId, string plantName, string areaName, string findingDescription, DateTime timestamp, string robotName)
    {
        public string TagId { get; set; } = tagId ?? throw new ArgumentNullException(nameof(tagId));
        public string PlantName { get; set; } = plantName ?? throw new ArgumentNullException(nameof(plantName));
        public string AreaName { get; set; } = areaName ?? throw new ArgumentNullException(nameof(areaName));
        public string FindingDescription { get; set; } = findingDescription ?? throw new ArgumentNullException(nameof(findingDescription));
        public DateTime Timestamp { get; set; } = timestamp;
        public string RobotName { get; set; } = robotName ?? throw new ArgumentNullException(nameof(robotName));
    }
}
