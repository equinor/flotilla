using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Api.Services;
using Api.Test.Database;
using Api.Test.Mocks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace Api.Test.Controllers
{
    public class FeedbackControllerTests : IAsyncLifetime
    {
        private TestWebApplicationFactory<Program>? _factory;

        private PostgreSqlContainer Container = null!;
        private HttpClient Client = null!;
        private JsonSerializerOptions SerializerOptions = null!;
        private MockTeamsNotificationService TeamsNotificationService = null!;

        public async ValueTask InitializeAsync()
        {
            (Container, string connectionString, var _) =
                await TestSetupHelpers.ConfigurePostgreSqlDatabase();

            _factory = TestSetupHelpers.ConfigureWebApplicationFactory(
                postgreSqlConnectionString: connectionString
            );

            Client = TestSetupHelpers.ConfigureHttpClient(_factory);
            SerializerOptions = TestSetupHelpers.ConfigureJsonSerializerOptions();

            TeamsNotificationService = _factory.MockTeamsNotificationService;
        }

        public async ValueTask DisposeAsync()
        {
            if (_factory is not null)
                await _factory.DisposeAsync();
            Client.Dispose();
            if (Container is not null)
                await Container.DisposeAsync();
            GC.SuppressFinalize(this);
        }

        [Fact]
        public async Task FeedbackIsSentWhenInputIsValid()
        {
            var feedback = new JsonObject
            {
                ["Title"] = "Test title with æøå and ÆØÅ",
                ["Email"] = "testuser@example.com",
                ["ShortName"] = "testUser",
                ["Description"] = "This is a test feedback message from Blåbærgrød.",
                ["Type"] = "FeatureRequest",
                ["Timestamp"] = DateTime.UtcNow,
                ["Url"] = "https://example.com/test-feedback",
            };

            var content = new StringContent(
                feedback.ToJsonString(SerializerOptions),
                System.Text.Encoding.UTF8,
                "application/json"
            );

            var response = await Client.PostAsync(
                "submit-feedback",
                content,
                TestContext.Current.CancellationToken
            );
            response.EnsureSuccessStatusCode();

            var notification = Assert.Single(TeamsNotificationService.Notifications);
            Assert.Equal(TeamsDestination.Feedback, notification.Destination);
            Assert.Contains("Test title with æøå and ÆØÅ", notification.CardJson);
            Assert.Contains("testuser@example.com", notification.CardJson);
            Assert.Contains("testUser", notification.CardJson);
        }

        [Fact]
        public async Task NoFeedbackIsSentWhenInputIsInvalid()
        {
            var feedback = new JsonObject
            {
                ["Title"] = "Test title",
                ["Email"] = "test2userexample.com", // Invalid email
                ["ShortName"] = "testUser",
                ["Description"] = "This is a test feedback message.",
                ["Type"] = "Other",
                ["Timestamp"] = DateTime.UtcNow,
                ["Url"] = "https://example.com/test-feedback",
            };

            var content = new StringContent(
                feedback.ToJsonString(SerializerOptions),
                System.Text.Encoding.UTF8,
                "application/json"
            );

            var response = await Client.PostAsync(
                "submit-feedback",
                content,
                TestContext.Current.CancellationToken
            );
            Assert.False(response.IsSuccessStatusCode);

            Assert.Empty(TeamsNotificationService.Notifications);
        }

        [Fact]
        public async Task NoFeedbackIsSentWhenTypeIsMissing()
        {
            var feedback = new JsonObject
            {
                ["Title"] = "Test title",
                ["Email"] = "testuser@example.com",
                ["ShortName"] = "testUser",
                ["Description"] = "This is a test feedback message.",
                ["Timestamp"] = DateTime.UtcNow,
                ["Url"] = "https://example.com/test-feedback",
            };

            var content = new StringContent(
                feedback.ToJsonString(SerializerOptions),
                System.Text.Encoding.UTF8,
                "application/json"
            );

            var response = await Client.PostAsync(
                "submit-feedback",
                content,
                TestContext.Current.CancellationToken
            );

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Empty(TeamsNotificationService.Notifications);
        }
    }
}
