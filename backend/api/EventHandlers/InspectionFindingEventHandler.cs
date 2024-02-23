using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Api.Database.Models;
using Api.Services;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using NCrontab;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Api.EventHandlers
{
    public class InspectionFindingEventHandler(IConfiguration configuration,
    IServiceScopeFactory scopeFactory,
    ILogger<InspectionFindingEventHandler> logger) : BackgroundService
    {
        private readonly string _cronExpression = "19 14 * * * ";
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

                    string adaptiveCardJson = GenerateAdaptiveCard($"Rapport {DateTime.UtcNow:yyyy-MM-dd HH}", inspectionFindings.Count, findingsList);

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
                        string errorBody = await response.Content.ReadAsStringAsync(stoppingToken);
                        logger.LogError($"Webhook request failed with status code {response.StatusCode}. Response body: {errorBody}");
                    }
                }
            }
        }

        private async Task<List<Finding>> GenerateFindingsList(List<InspectionFinding> inspectionFindings)
        {
            var findingsList = new List<Finding>();

            foreach (var inspectionFinding in inspectionFindings)
            {
                var missionRun = await InspectionFindingService.GetMissionRunByIsarStepId(inspectionFinding.IsarStepId);
                var task = await InspectionFindingService.GetMissionTaskByIsarStepId(inspectionFinding.IsarStepId);

                if (task != null && missionRun != null)
                {
                    var finding = new Finding(
                        task.TagId ?? "NA",
                        missionRun.Area?.Plant.Name ?? "NA",
                        missionRun.Area?.Name ?? "NA",
                        inspectionFinding.Finding,
                        inspectionFinding.InspectionDate
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

        public static string GenerateAdaptiveCard(string title, int numberOfFindings, List<Finding> findingsReports)
        {
            var findingsJsonArray = new JArray();

            foreach (var finding in findingsReports)
            {

                var factsArray = new JArray(
                    new JObject(new JProperty("name", "Anlegg"), new JProperty("value", finding.PlantName)),
                    new JObject(new JProperty("name", "Område"), new JProperty("value", finding.AreaName)),
                    new JObject(new JProperty("name", "Tag Number"), new JProperty("value", finding.TagId)),
                    new JObject(new JProperty("name", "Beskrivelse"), new JProperty("value", finding.FindingDescription)),
                    new JObject(new JProperty("name", "Tidspunkt"), new JProperty("value", finding.Timestamp.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)))
                );

                var findingObj = new JObject(
                    new JProperty("activityTitle", $"Finding ID: \"{finding.TagId}\""),
                    new JProperty("facts", factsArray));
                findingsJsonArray.Add(findingObj);
            }

            var sections = new JArray(
                new JObject(
                    new JProperty("activityTitle", $"Inspection report for \"{findingsReports[0].PlantName}\""),
                    new JProperty("activitySubtitle", $"Generated on: {DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)}"),
                    new JProperty("facts", new JArray(
                        new JObject(
                            new JProperty("name", "Number of findings:"),
                            new JProperty("value", numberOfFindings))))),
                new JObject(
                    new JProperty("activityTitle", "The following inspection findings were identified:")));

            foreach (var findingObj in findingsJsonArray)
            {
                sections.Add(findingObj);
            }

            var adaptiveCardObj = new JObject(
                new JProperty("summary", "Inspection Findings Report"),
                new JProperty("themeColor", "0078D7"),
                new JProperty("title", $"Inspection Findings: \"{title}\""),
                new JProperty("sections", sections));

            string adaptiveCardJson = adaptiveCardObj.ToString(Formatting.Indented);
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

    public class Finding(string tagId, string plantName, string areaName, string findingDescription, DateTime timestamp)
    {
        public string TagId { get; set; } = tagId ?? throw new ArgumentNullException(nameof(tagId));
        public string PlantName { get; set; } = plantName ?? throw new ArgumentNullException(nameof(plantName));
        public string AreaName { get; set; } = areaName ?? throw new ArgumentNullException(nameof(areaName));
        public string FindingDescription { get; set; } = findingDescription ?? throw new ArgumentNullException(nameof(findingDescription));
        public DateTime Timestamp { get; set; } = timestamp;
    }
}
