using NCrontab;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Api.Database.Models;
using Api.Services;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace Api.EventHandlers
{
    public class InspectionFindingEventHandler(IConfiguration configuration,
    IServiceScopeFactory scopeFactory,
    ILogger<InspectionFindingEventHandler> logger) : BackgroundService
    {
        private readonly string _cronExpression = "30 16 * * * ";
        private readonly TimeSpan _interval = configuration.GetValue<TimeSpan>("InspectionFindingEventHandler:Interval");
        private InspectionFindingService InspectionFindingService => scopeFactory.CreateScope().ServiceProvider.GetRequiredService<InspectionFindingService>();
        private readonly TimeSpan _timeSpan = configuration.GetValue<TimeSpan>("InspectionFindingEventHandler:TimeSpan");

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("InspectionFinding EventHandler service started at {time}", DateTime.UtcNow);

            while (!stoppingToken.IsCancellationRequested)
            {

                var now = DateTime.UtcNow;

                var nextExecutionTime = CrontabSchedule.Parse(_cronExpression).GetNextOccurrence(now);

                var delay = nextExecutionTime - now;

                if (delay.TotalMilliseconds > 0)
                {
                    await Task.Delay(delay, stoppingToken);
                }

                var lastReportingTime = DateTime.UtcNow - _timeSpan;

                var inspectionFindings = await InspectionFindingService.RetrieveInspectionFindings(lastReportingTime);

                logger.LogInformation("Found {count} inspection findings in last {interval}", inspectionFindings.Count, _timeSpan);

                if (inspectionFindings.Count > 0)
                {
                    var findingsList = await GenerateFindingsList(inspectionFindings);

                    string messageString = GenerateReportFromFindingsReportsList(findingsList);

                    string adaptiveCardJson = GenerateAdaptiveCard(messageString);

                    string url = GetWebhookURL(configuration, "TeamsInspectionFindingsWebhook");

                    var client = new HttpClient();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    var content = new StringContent(adaptiveCardJson, Encoding.UTF8, "application/json");
                    var response = await client.PostAsync(url, content, stoppingToken);
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        logger.LogInformation("Post request via teams incomming webhook was successful, Status Code: {response.StatusCode}", response.StatusCode);
                    }
                    else
                    {
                        logger.LogInformation("Post request via teams incomming webhook was not successful, Status Code: {response.StatusCode}", response.StatusCode);
                    }
                }
            }
        }

        private async Task<List<Finding>> GenerateFindingsList(List<InspectionFinding> inspectionFindings)
        {
            var findingsList = new List<Finding>();

            foreach (var inspectionFinding in inspectionFindings)
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
            logger.LogInformation("Findings List sucessfully generated, adaptive Card will be generated next");
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

        public static string GetWebhookURL(IConfiguration configuration, string secretName)
        {

            string? keyVaultUri = configuration.GetSection("KeyVault")["VaultUri"] ?? throw new KeyNotFoundException("No key vault in config");

            var keyVault = new SecretClient(
                new Uri(keyVaultUri),
                new DefaultAzureCredential(
                    new DefaultAzureCredentialOptions { ExcludeSharedTokenCacheCredential = true }
                )
            );

            string webhookURL = keyVault
                .GetSecret(secretName)
                .Value.Value;

            return webhookURL;
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
