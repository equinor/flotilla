using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Api.Controllers.Models;
using Api.Services;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace Api.Test.Controllers
{
    public class InstallationControllerTests : IAsyncLifetime
    {
        public required PostgreSqlContainer Container;
        public required HttpClient Client;

        public required IInstallationService InstallationService;

        public async ValueTask InitializeAsync()
        {
            (Container, string connectionString, var connection) =
                await TestSetupHelpers.ConfigurePostgreSqlDatabase();
            var factory = TestSetupHelpers.ConfigureWebApplicationFactory(
                postgreSqlConnectionString: connectionString
            );
            var serviceProvider = TestSetupHelpers.ConfigureServiceProvider(factory);

            Client = TestSetupHelpers.ConfigureHttpClient(factory);
            InstallationService = serviceProvider.GetRequiredService<IInstallationService>();
        }

        public ValueTask DisposeAsync()
        {
            GC.SuppressFinalize(this);
            return ValueTask.CompletedTask;
        }

        [Fact]
        public async Task TestCreateInstallationEndpoint()
        {
            // Arrange
            var query = new CreateInstallationQuery
            {
                InstallationCode = "inst",
                Name = "Installation",
            };

            var content = new StringContent(
                JsonSerializer.Serialize(query),
                null,
                "application/json"
            );

            // Act
            const string Url = "/installations";
            var response = await Client.PostAsync(
                Url,
                content,
                TestContext.Current.CancellationToken
            );

            // Assert
            var installation = await InstallationService.ReadByInstallationCode(
                query.InstallationCode
            );

            Assert.True(response.IsSuccessStatusCode);
            Assert.Equal(query.Name, installation!.Name);
        }
    }
}
