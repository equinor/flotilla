using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using AdaptiveCards.Templating;
using Api.Services.Events;
using Api.Utilities;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace Api.EventHandlers
{
    public class TeamsMessageEventHandler : EventHandlerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<TeamsMessageEventHandler> _logger;
        private EventAggregatorSingletonService _eventAggregatorSingletonService;

        public TeamsMessageEventHandler(
            IConfiguration configuration,
            ILogger<TeamsMessageEventHandler> logger,
            EventAggregatorSingletonService eventAggregatorSingletonService
        )
        {
            _configuration = configuration;
            _logger = logger;
            _eventAggregatorSingletonService = eventAggregatorSingletonService;

            Subscribe();
        }

        public override void Subscribe()
        {
            _eventAggregatorSingletonService.Subscribe<TeamsMessageEventArgs>(
                OnTeamsMessageReceived
            );
        }

        public override void Unsubscribe() { }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await stoppingToken;
        }

        private async void OnTeamsMessageReceived(TeamsMessageEventArgs e)
        {
            string teamsMessage = e.TeamsMessage;
            string url;

            try
            {
                url = GetWebhookURL(_configuration);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogError(ex, "Failed to get webhook URL from configuration");
                return;
            }
            var client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json")
            );

            var content = CreateTeamsMessageCard(teamsMessage);
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

            if (!response.IsSuccessStatusCode)
            {
                string errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogError(
                    $"Webhook request failed with status code {response.StatusCode}. Response body: {errorBody}"
                );
            }
        }

        private StringContent CreateTeamsMessageCard(string message)
        {
            var adaptiveCardJson = File.ReadAllText("Utilities/TeamsAdaptiveCard.json");

            AdaptiveCardTemplate template = new AdaptiveCardTemplate(adaptiveCardJson);

            string cardJson = template.Expand(
                new
                {
                    Message = message,
                    Date = DateTime.UtcNow.ToString("dd-MM-yyyy HH:mm", CultureInfo.CurrentCulture),
                }
            );

            var content = new StringContent(cardJson, Encoding.UTF8, "application/json");

            return content;
        }

        private static string GetWebhookURL(IConfiguration configuration)
        {
            string? webhookUrlFromConfig = configuration.GetSection("TeamsNotification")[
                "WebhookUrl"
            ];

            return webhookUrlFromConfig
                ?? throw new KeyNotFoundException("No webhook URL in config");
        }
    }
}
