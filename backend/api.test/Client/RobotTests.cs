using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Api.Controllers.Models;
using Api.Database.Context;
using Api.Database.Models;
using Api.Test.Database;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Xunit;
namespace Api.Test.Client
{
    [Collection("Database collection")]
    public class RobotTests : IClassFixture<TestWebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;
        private readonly DatabaseUtilities _databaseUtilities;
        private readonly JsonSerializerOptions _serializerOptions =
            new()
            {
                Converters =
                {
                    new JsonStringEnumConverter()
                },
                PropertyNameCaseInsensitive = true
            };

        public RobotTests(TestWebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                BaseAddress = new Uri("https://localhost:8000")
            });
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                TestAuthHandler.AuthenticationScheme
            );
            object? context = factory.Services.GetService(typeof(FlotillaDbContext)) as FlotillaDbContext ?? throw new ArgumentNullException(nameof(factory));
            _databaseUtilities = new DatabaseUtilities((FlotillaDbContext)context);
        }

        [Fact]
        public async Task RobotsTest()
        {
            string url = "/robots";
            var response = await _client.GetAsync(url);
            var robots = await response.Content.ReadFromJsonAsync<List<RobotResponse>>(_serializerOptions);
            Assert.True(response.IsSuccessStatusCode);
            Assert.NotNull(robots);
        }

        [Fact]
        public async Task GetRobotById_ShouldReturnNotFound()
        {
            string robotId = "RandomString";
            string url = "/robots/" + robotId;
            var response = await _client.GetAsync(url);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetRobotById_ShouldReturnRobot()
        {
            var installation = await _databaseUtilities.ReadOrNewInstallation();
            _ = await _databaseUtilities.NewRobot(RobotStatus.Available, installation);

            string url = "/robots";
            var response = await _client.GetAsync(url);
            var robots = await response.Content.ReadFromJsonAsync<List<RobotResponse>>(_serializerOptions);
            Assert.NotNull(robots);

            string robotId = robots[0].Id;

            var robotResponse = await _client.GetAsync("/robots/" + robotId);
            var robot = await robotResponse.Content.ReadFromJsonAsync<RobotResponse>(_serializerOptions);
            Assert.Equal(HttpStatusCode.OK, robotResponse.StatusCode);
            Assert.NotNull(robot);
            Assert.Equal(robot.Id, robotId);
        }


#pragma warning disable xUnit1004
        [Fact(Skip = "Runs inconcistently as it is tied to the database interactions of other tests")]
#pragma warning restore xUnit1004
        public async Task RobotIsNotCreatedWithAreaNotInInstallation()
        {
            // Arrange - Area
            var installation = await _databaseUtilities.ReadOrNewInstallation();

            var wrongInstallation = await _databaseUtilities.NewInstallation("wrongInstallation");
            var wrongPlant = await _databaseUtilities.ReadOrNewPlant(wrongInstallation.InstallationCode);
            var wrongInspectionArea = await _databaseUtilities.ReadOrNewInspectionArea(wrongInstallation.InstallationCode, wrongPlant.PlantCode);

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
                var response = await _client.PostAsync(robotUrl, content);
            }
            catch (DbUpdateException ex)
            {
                Assert.True(ex.Message == $"Could not create new robot in database as inspection area '{wrongInspectionArea.Name}' does not exist in installation {installation.InstallationCode}");
            }
        }
    }
}
