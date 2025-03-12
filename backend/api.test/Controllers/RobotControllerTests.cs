﻿using System;
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
using Testcontainers.PostgreSql;
using Xunit;

namespace Api.Test.Controllers
{
    public class RobotControllerTests : IAsyncLifetime
    {
        public required DatabaseUtilities DatabaseUtilities;
        public required PostgreSqlContainer Container;
        public required HttpClient Client;
        public required JsonSerializerOptions SerializerOptions;

        public async Task InitializeAsync()
        {
            (Container, string connectionString, var connection) =
                await TestSetupHelpers.ConfigurePostgreSqlDatabase();
            var factory = TestSetupHelpers.ConfigureWebApplicationFactory(
                postgreSqlConnectionString: connectionString
            );
            Client = TestSetupHelpers.ConfigureHttpClient(factory);
            SerializerOptions = TestSetupHelpers.ConfigureJsonSerializerOptions();

            DatabaseUtilities = new DatabaseUtilities(
                TestSetupHelpers.ConfigurePostgreSqlContext(connectionString)
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
    }
}
