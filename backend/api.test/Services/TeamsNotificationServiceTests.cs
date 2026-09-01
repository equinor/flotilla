using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Api.Services;
using Api.Utilities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace Api.Test.Services
{
    public class TeamsNotificationServiceTests
    {
        [Fact]
        public async Task SendsFeedbackCardToFeedbackDestination()
        {
            Uri? requestUri = null;
            string? requestBody = null;
            var handler = new StubHttpMessageHandler(async request =>
            {
                requestUri = request.RequestUri;
                requestBody = await request.Content!.ReadAsStringAsync();
                return new HttpResponseMessage(HttpStatusCode.Accepted);
            });
            var service = CreateService(handler);

            await service.SendTeamsMessageAsync(
                "Email: user@example.com",
                TeamsDestination.Feedback,
                TestContext.Current.CancellationToken
            );

            Assert.Equal("https://feedback.example/webhook", requestUri?.ToString());
            Assert.Contains("user@example.com", requestBody);
        }

        [Fact]
        public async Task ThrowsWhenTeamsRejectsMessage()
        {
            var handler = new StubHttpMessageHandler(_ =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest))
            );
            var service = CreateService(handler);

            await Assert.ThrowsAsync<TeamsNotificationException>(() =>
                service.SendTeamsMessageAsync(
                    "Feedback",
                    TeamsDestination.Feedback,
                    TestContext.Current.CancellationToken
                )
            );
        }

        private static TeamsNotificationService CreateService(HttpMessageHandler handler)
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["TeamsNotification:Destinations:SystemAlerts:WebhookUrl"] =
                            "https://alerts.example/webhook",
                        ["TeamsNotification:Destinations:Feedback:WebhookUrl"] =
                            "https://feedback.example/webhook",
                    }
                )
                .Build();

            return CreateService(handler, configuration);
        }

        private static TeamsNotificationService CreateService(
            HttpMessageHandler handler,
            IConfiguration configuration
        )
        {
            var hostEnvironment = new Mock<IWebHostEnvironment>();
            hostEnvironment
                .SetupGet(environment => environment.ContentRootPath)
                .Returns(
                    Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../api"))
                );

            var httpClientFactory = new Mock<IHttpClientFactory>();
            httpClientFactory
                .Setup(factory => factory.CreateClient(It.IsAny<string>()))
                .Returns(() => new HttpClient(handler, disposeHandler: false));

            return new TeamsNotificationService(
                httpClientFactory.Object,
                configuration,
                hostEnvironment.Object
            );
        }

        private sealed class StubHttpMessageHandler(
            Func<HttpRequestMessage, Task<HttpResponseMessage>> send
        ) : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken
            )
            {
                return send(request);
            }
        }
    }
}
