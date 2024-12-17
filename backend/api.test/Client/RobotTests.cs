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

namespace Api.Test.Client
{
    public class RobotTests : IAsyncLifetime
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
        public async Task RobotsTest()
        {
            string url = "/robots";
            var response = await Client.GetAsync(url);
            var robots = await response.Content.ReadFromJsonAsync<List<RobotResponse>>(
                SerializerOptions
            );
            Assert.True(response.IsSuccessStatusCode);
            Assert.NotNull(robots);
        }

        [Fact]
        public async Task GetRobotById_ShouldReturnNotFound()
        {
            string robotId = "RandomString";
            string url = "/robots/" + robotId;
            var response = await Client.GetAsync(url);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetRobotById_ShouldReturnRobot()
        {
            var installation = await DatabaseUtilities.ReadOrNewInstallation();
            _ = await DatabaseUtilities.NewRobot(RobotStatus.Available, installation);

            string url = "/robots";
            var response = await Client.GetAsync(url);
            var robots = await response.Content.ReadFromJsonAsync<List<RobotResponse>>(
                SerializerOptions
            );
            Assert.NotNull(robots);

            string robotId = robots[0].Id;

            var robotResponse = await Client.GetAsync("/robots/" + robotId);
            var robot = await robotResponse.Content.ReadFromJsonAsync<RobotResponse>(
                SerializerOptions
            );
            Assert.Equal(HttpStatusCode.OK, robotResponse.StatusCode);
            Assert.NotNull(robot);
            Assert.Equal(robot.Id, robotId);
        }

#pragma warning disable xUnit1004
        [Fact(
            Skip = "Runs inconcistently as it is tied to the database interactions of other tests"
        )]
#pragma warning restore xUnit1004
        public async Task RobotIsNotCreatedWithAreaNotInInstallation()
        {
            // Arrange - Area
            var installation = await DatabaseUtilities.ReadOrNewInstallation();

            var wrongInstallation = await DatabaseUtilities.NewInstallation("wrongInstallation");
            var wrongPlant = await DatabaseUtilities.ReadOrNewPlant(
                wrongInstallation.InstallationCode
            );
            var wrongInspectionArea = await DatabaseUtilities.ReadOrNewInspectionArea(
                wrongInstallation.InstallationCode,
                wrongPlant.PlantCode
            );

            // Arrange - Create robot
            var robotQuery = new CreateRobotQuery
            {
                IsarId = Guid.NewGuid().ToString(),
                Name = "RobotGetNextRun",
                SerialNumber = "GetNextRun",
                RobotType = RobotType.Robot,
                Status = RobotStatus.Available,
                Host = "localhost",
                Port = 3000,
                CurrentInstallationCode = installation.InstallationCode,
                CurrentInspectionAreaName = wrongInspectionArea.Name,
            };

            string robotUrl = "/robots";
            var content = new StringContent(
                JsonSerializer.Serialize(robotQuery),
                null,
                "application/json"
            );

            try
            {
                var response = await Client.PostAsync(robotUrl, content);
            }
            catch (DbUpdateException ex)
            {
                Assert.True(
                    ex.Message
                        == $"Could not create new robot in database as inspection area '{wrongInspectionArea.Name}' does not exist in installation {installation.InstallationCode}"
                );
            }
        }
    }
}
