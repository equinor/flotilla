using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Api.Controllers.Models;
using Api.Database.Models;
using Api.Test.Database;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Api.Test.Controllers
{
    public class RobotControllerTests : IAsyncLifetime
    {
        public required DatabaseUtilities DatabaseUtilities;
        public required HttpClient Client;
        public required JsonSerializerOptions SerializerOptions;

        public async Task InitializeAsync()
        {
            string databaseName = Guid.NewGuid().ToString();
            (string connectionString, var connection) = await TestSetupHelpers.ConfigureDatabase(
                databaseName
            );
            var factory = TestSetupHelpers.ConfigureWebApplicationFactory(databaseName);

            Client = TestSetupHelpers.ConfigureHttpClient(factory);
            SerializerOptions = TestSetupHelpers.ConfigureJsonSerializerOptions();

            DatabaseUtilities = new DatabaseUtilities(
                TestSetupHelpers.ConfigureFlotillaDbContext(connectionString)
            );
        }

        public Task DisposeAsync() => Task.CompletedTask;

        [Fact]
        public async Task CheckThatReadAllRobotsEndpointIsSuccessful()
        {
            // Arrange
            var installation = await DatabaseUtilities.NewInstallation();

            _ = await DatabaseUtilities.NewRobot(RobotStatus.Busy, installation);
            _ = await DatabaseUtilities.NewRobot(RobotStatus.Available, installation);
            _ = await DatabaseUtilities.NewRobot(RobotStatus.Offline, installation);

            // Act
            var response = await Client.GetAsync("/robots");
            var robots = await response.Content.ReadFromJsonAsync<List<RobotResponse>>(
                SerializerOptions
            );

            // Assert
            Assert.True(response.IsSuccessStatusCode);
            Assert.Equal(3, robots!.Count);
        }

        [Fact]
        public async Task CheckThatReadRobotWithUnknownIdReturnsNotFound()
        {
            const string RobotId = "IAmAnUnknownRobot";
            const string Url = "/robots/" + RobotId;
            var response = await Client.GetAsync(Url);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task CheckThatReadRobotByIdEndpointIsSuccessful()
        {
            // Arrange
            var installation = await DatabaseUtilities.NewInstallation();
            var robot = await DatabaseUtilities.NewRobot(RobotStatus.Available, installation);

            // Act
            var robotResponse = await Client.GetAsync("/robots/" + robot.Id);
            var receivedRobot = await robotResponse.Content.ReadFromJsonAsync<RobotResponse>(
                SerializerOptions
            );

            // Assert
            Assert.Equal(receivedRobot!.Id, robot.Id);
        }

        [Fact]
        public async Task CheckThatRobotIsNotCreatedWhenInspectionAreaIsNotInInstallation()
        {
            // Arrange
            var installation = await DatabaseUtilities.NewInstallation();

            var wrongInstallation = await DatabaseUtilities.NewInstallation("wrongInstallation");
            var wrongPlant = await DatabaseUtilities.NewPlant(wrongInstallation.InstallationCode);
            var wrongInspectionArea = await DatabaseUtilities.NewInspectionArea(
                wrongInstallation.InstallationCode,
                wrongPlant.PlantCode
            );

            var robotQuery = new CreateRobotQuery
            {
                IsarId = Guid.NewGuid().ToString(),
                Name = "TestRobot",
                SerialNumber = "TestRobotSN",
                RobotType = RobotType.Robot,
                Status = RobotStatus.Available,
                Host = "localhost",
                Port = 3000,
                CurrentInstallationCode = installation.InstallationCode,
                CurrentInspectionAreaName = wrongInspectionArea.Name,
            };

            // Act
            const string RobotUrl = "/robots";
            var content = new StringContent(
                JsonSerializer.Serialize(robotQuery),
                null,
                "application/json"
            );

            // Assert
            var response = await Client.PostAsync(RobotUrl, content);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
