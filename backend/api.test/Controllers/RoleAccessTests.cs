﻿using System;
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
using Testcontainers.PostgreSql;
using Xunit;

namespace Api.Test.Controllers
{
    public class RoleAccessTests : IAsyncLifetime
    {
        public required DatabaseUtilities DatabaseUtilities;
        public required PostgreSqlContainer Container;
        public required HttpClient Client;
        public required JsonSerializerOptions SerializerOptions;
        public required MockHttpContextAccessor HttpContextAccessor;

        public async Task InitializeAsync()
        {
            (Container, string connectionString, var connection) =
                await TestSetupHelpers.ConfigurePostgreSqlDatabase();
            var factory = TestSetupHelpers.ConfigureWebApplicationFactory(
                postgreSqlConnectionString: connectionString
            );
            var serviceProvider = TestSetupHelpers.ConfigureServiceProvider(factory);

            Client = TestSetupHelpers.ConfigureHttpClient(factory);
            SerializerOptions = TestSetupHelpers.ConfigureJsonSerializerOptions();
            HttpContextAccessor = (MockHttpContextAccessor)
                serviceProvider.GetService<IHttpContextAccessor>()!;

            DatabaseUtilities = new DatabaseUtilities(
                TestSetupHelpers.ConfigurePostgreSqlContext(connectionString)
            );
        }

        public Task DisposeAsync() => Task.CompletedTask;

        [Fact]
        public async Task CheckThatRequestingPlantsWithUnauthorizedUserFails()
        {
            // Arrange
            var installation = await DatabaseUtilities.NewInstallation();
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
    }
}
