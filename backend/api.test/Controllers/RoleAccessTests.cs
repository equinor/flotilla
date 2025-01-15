using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Api.Controllers.Models;
using Api.Database.Models;
using Api.Test.Database;
using Api.Test.Mocks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Api.Test.Controllers
{
    public class RoleAccessTests : IAsyncLifetime
    {
        public required DatabaseUtilities DatabaseUtilities;
        public required HttpClient Client;
        public required JsonSerializerOptions SerializerOptions;
        public required MockHttpContextAccessor HttpContextAccessor;

        public async Task InitializeAsync()
        {
            string databaseName = Guid.NewGuid().ToString();
            (string connectionString, var connection) = await TestSetupHelpers.ConfigureDatabase(
                databaseName
            );
            var factory = TestSetupHelpers.ConfigureWebApplicationFactory(databaseName);
            var serviceProvider = TestSetupHelpers.ConfigureServiceProvider(factory);

            Client = TestSetupHelpers.ConfigureHttpClient(factory);
            SerializerOptions = TestSetupHelpers.ConfigureJsonSerializerOptions();
            HttpContextAccessor = (MockHttpContextAccessor)
                serviceProvider.GetService<IHttpContextAccessor>()!;

            DatabaseUtilities = new DatabaseUtilities(
                TestSetupHelpers.ConfigureFlotillaDbContext(connectionString)
            );
        }

        public Task DisposeAsync() => Task.CompletedTask;

        [Fact]
        public async Task CheckThatRequestingPlantsWithUnauthorizedUserFails()
        {
            // Arrange
            var installation = await DatabaseUtilities.NewInstallation("TestInstallationCode");
            var plant = await DatabaseUtilities.NewPlant(installation.InstallationCode);

            var accessRoleQuery = new CreateAccessRoleQuery
            {
                InstallationCode = installation.InstallationCode,
                RoleName = "User.TestRole",
                AccessLevel = RoleAccessLevel.USER,
            };
            var accessRoleContent = new StringContent(
                JsonSerializer.Serialize(accessRoleQuery),
                null,
                "application/json"
            );

            var accessRoleResponse = await Client.PostAsync("/access-roles", accessRoleContent);

            // Restrict ourselves to a user without access
            HttpContextAccessor.SetHttpContextRoles(["User.TestInstallationAreaTest_Wrong"]);

            // Act
            string getPlantUrl = $"/plants/{plant.Id}";
            var samePlantResponse = await Client.GetAsync(getPlantUrl);

            // Assert
            Assert.True(accessRoleResponse.IsSuccessStatusCode);
            Assert.False(samePlantResponse.IsSuccessStatusCode);
            Assert.Equal("NotFound", samePlantResponse.StatusCode.ToString());
        }

        [Fact]
        public async Task CheckThatAnAuthorizedUserRoleCanAccessAreaEndpoint()
        {
            // Arrange
            var installation = await DatabaseUtilities.NewInstallation();
            var plant = await DatabaseUtilities.NewPlant(installation.InstallationCode);
            var inspectionArea = await DatabaseUtilities.NewInspectionArea(
                installation.InstallationCode,
                plant.PlantCode
            );
            var area = await DatabaseUtilities.NewArea(
                installation.InstallationCode,
                plant.PlantCode,
                inspectionArea.Name
            );

            var accessRoleQuery = new CreateAccessRoleQuery
            {
                InstallationCode = installation.InstallationCode,
                RoleName = "User.TestRole",
                AccessLevel = RoleAccessLevel.USER,
            };
            var accessRoleContent = new StringContent(
                JsonSerializer.Serialize(accessRoleQuery),
                null,
                "application/json"
            );
            _ = await Client.PostAsync("/access-roles", accessRoleContent);

            // Act
            // Restrict ourselves to RoleAccessLevel.USER
            HttpContextAccessor.SetHttpContextRoles(["User.TestRole"]);
            var response = await Client.GetAsync($"/areas/{area.Id}");

            // Assert
            var areaFromResponse = await response.Content.ReadFromJsonAsync<AreaResponse>(
                SerializerOptions
            );

            Assert.True(response.IsSuccessStatusCode);
            Assert.Equal(areaFromResponse!.Id, area.Id);
        }
    }
}
