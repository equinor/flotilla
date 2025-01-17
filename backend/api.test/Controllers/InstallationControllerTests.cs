using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Api.Controllers.Models;
using Api.Services;
using Api.Test.Database;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Api.Test.Controllers
{
    public class InstallationControllerTests : IAsyncLifetime
    {
        public required HttpClient Client;

        public required IInstallationService InstallationService;

        public async Task InitializeAsync()
        {
            string databaseName = Guid.NewGuid().ToString();
            (string connectionString, var connection) =
                await TestSetupHelpers.ConfigureSqLiteDatabase(databaseName);
            var factory = TestSetupHelpers.ConfigureWebApplicationFactory(databaseName);
            var serviceProvider = TestSetupHelpers.ConfigureServiceProvider(factory);

            Client = TestSetupHelpers.ConfigureHttpClient(factory);
            InstallationService = serviceProvider.GetRequiredService<IInstallationService>();
        }

        public Task DisposeAsync() => Task.CompletedTask;

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
            var response = await Client.PostAsync(Url, content);

            // Assert
            var installation = await InstallationService.ReadByInstallationCode(
                query.InstallationCode
            );

            Assert.True(response.IsSuccessStatusCode);
            Assert.Equal(query.Name, installation!.Name);
        }
    }
}
