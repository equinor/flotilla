using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Api.Services;
using Api.Services.Events;
using Api.Utilities;

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
            string url = InspectionFindingEventHandler.GetWebhookURL(_configuration, "TeamsInspectionFindingsWebhook");
            var client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var content = new StringContent(e.TeamsMessage, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(url, content);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                _logger.LogInformation("Post request via teams incomming webhook was successful, Status Code: {response.StatusCode}", response.StatusCode);
            }
            else
            {
                string errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogError($"Webhook request failed with status code {response.StatusCode}. Response body: {errorBody}");
            }
        }
    }
}

