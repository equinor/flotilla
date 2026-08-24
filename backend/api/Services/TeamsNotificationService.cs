using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using AdaptiveCards.Templating;
using Api.Utilities;

namespace Api.Services
{
    public enum TeamsDestination
    {
        SystemAlerts,
        Feedback,
    }

    public interface ITeamsNotificationService
    {
        Task SendTeamsMessageAsync(
            string message,
            TeamsDestination destination,
            CancellationToken cancellationToken = default
        );
    }

    public class TeamsNotificationService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        IWebHostEnvironment hostEnvironment
    ) : ITeamsNotificationService
    {
        private readonly AdaptiveCardTemplate _template = new(
            File.ReadAllText(
                Path.Combine(hostEnvironment.ContentRootPath, "Utilities", "TeamsAdaptiveCard.json")
            )
        );

        public async Task SendTeamsMessageAsync(
            string message,
            TeamsDestination destination,
            CancellationToken cancellationToken = default
        )
        {
            var cardJson = RenderCard(
                new
                {
                    Title = destination.ToString(),
                    Message = message,
                    Date = DateTimeOffset.UtcNow.ToString(
                        "dd-MM-yyyy HH:mm",
                        CultureInfo.CurrentCulture
                    ),
                }
            );
            var url = GetWebhookUrl(destination);
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = new StringContent(cardJson, Encoding.UTF8, "application/json");
            using var client = httpClientFactory.CreateClient();
            using var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new TeamsNotificationException(
                    $"Failed to send Teams message to {destination}. Status code: {response.StatusCode}. Reason: {response.ReasonPhrase}"
                );
        }

        private string RenderCard(object data)
        {
            return _template.Expand(data);
        }

        private string GetWebhookUrl(TeamsDestination destination)
        {
            var destinationUrl = configuration[
                $"TeamsNotification:Destinations:{destination}:WebhookUrl"
            ];
            if (destinationUrl is not null)
                return destinationUrl;

            throw new InvalidOperationException(
                $"Missing Teams webhook configuration for {destination}"
            );
        }
    }
}
