using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using Api.Services;
using Api.Services.Events;
using Api.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Api.EventHandlers
{
    public class TeamsMessageEventHandler : EventHandlerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<MissionEventHandler> _logger;

        public TeamsMessageEventHandler(
            IConfiguration configuration,
            ILogger<MissionEventHandler> logger
        )
        {
            _configuration = configuration;
            _logger = logger;

            Subscribe();
        }

        public override void Subscribe()
        {
            TeamsMessageService.TeamsMessage += OnTeamsMessageReceived;
        }

        public override void Unsubscribe()
        {
            TeamsMessageService.TeamsMessage -= OnTeamsMessageReceived;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await stoppingToken;
        }

        private async void OnTeamsMessageReceived(object? sender, TeamsMessageEventArgs e)
        {
            string url = InspectionFindingEventHandler.GetWebhookURL(_configuration, "TeamsSystemStatusNotification");
            var client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var content = CreateTeamsMessageCard(e.TeamsMessage);
            HttpResponseMessage? response;
            try
            {
                response = await client.PostAsync(url, content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send message to Teams");
                return;
            }
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Post request via teams incomming webhook was successful, Status Code: {response.StatusCode}", response.StatusCode);
                return;
            }

            string errorBody = await response.Content.ReadAsStringAsync();
            _logger.LogError($"Webhook request failed with status code {response.StatusCode}. Response body: {errorBody}");

        }

        private static StringContent CreateTeamsMessageCard(string message)
        {
            string jsonMessage = new JObject(
                new JProperty("title", "System Status:"),
                new JProperty("text", message),
                new JProperty("sections", new JArray(new JObject(new JProperty("activitySubtitle", $"Generated on: {DateTime.UtcNow.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture)}"))))
                ).ToString(Formatting.Indented);

            var content = new StringContent(jsonMessage, Encoding.UTF8, "application/json");

            return content;
        }
    }
}
