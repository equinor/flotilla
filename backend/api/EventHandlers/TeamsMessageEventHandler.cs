using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using AdaptiveCards.Templating;
using Api.Services;
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

        public TeamsMessageEventHandler(
            IConfiguration configuration,
            ILogger<TeamsMessageEventHandler> logger
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
            string url = GetWebhookURL(_configuration, "TeamsSystemStatusNotification");
            var client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json")
            );

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

        private static string GetWebhookURL(IConfiguration configuration, string secretName)
        {
            string? keyVaultUri =
                configuration.GetSection("KeyVault")["VaultUri"]
                ?? throw new KeyNotFoundException("No key vault in config");

            var keyVault = new SecretClient(
                new Uri(keyVaultUri),
                new DefaultAzureCredential(new DefaultAzureCredentialOptions())
            );

            string webhookURL = keyVault.GetSecret(secretName).Value.Value;

            return webhookURL;
        }
    }
}
