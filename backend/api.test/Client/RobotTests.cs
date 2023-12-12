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
using Api.Database.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Xunit;
namespace Api.Test
{
    [Collection("Database collection")]
    public class RobotTests : IClassFixture<TestWebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;
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
        }

        [Fact]
        public async Task RobotsTest()
        {
            string url = "/robots";
            var response = await _client.GetAsync(url);
            var robots = await response.Content.ReadFromJsonAsync<List<RobotResponse>>(_serializerOptions);
            Assert.True(response.IsSuccessStatusCode);
            Assert.True(robots != null);
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


        [Fact]
        public async Task RobotIsNotCreatedWithAreaNotInInstallation()
        {
            // Area
            string areaUrl = "/areas";
            var areaResponse = await _client.GetAsync(areaUrl);
            Assert.True(areaResponse.IsSuccessStatusCode);
            var areas = await areaResponse.Content.ReadFromJsonAsync<List<AreaResponse>>(_serializerOptions);
            Assert.True(areas != null);
            var area = areas[0];

            // Installation
            string testInstallation = "InstallationRobotIsNotCreatedWithAreaNotInInstallation";
            var installationQuery = new CreateInstallationQuery
            {
                InstallationCode = testInstallation,
                Name = testInstallation
            };

            var installationContent = new StringContent(
                JsonSerializer.Serialize(installationQuery),
                null,
                "application/json"
            );

            string installationUrl = "/installations";
            var installationResponse = await _client.PostAsync(installationUrl, installationContent);
            Assert.True(installationResponse.IsSuccessStatusCode);
            var wrongInstallation = await installationResponse.Content.ReadFromJsonAsync<Installation>(_serializerOptions);
            Assert.True(wrongInstallation != null);

            // Arrange - Create robot
            var robotQuery = new CreateRobotQuery
            {
                IsarId = Guid.NewGuid().ToString(),
                Name = "RobotGetNextRun",
                SerialNumber = "GetNextRun",
                RobotType = RobotType.Robot,
                Status = RobotStatus.Available,
                Enabled = true,
                Host = "localhost",
                Port = 3000,
                CurrentInstallationCode = wrongInstallation.InstallationCode,
                VideoStreams = new List<CreateVideoStreamQuery>()
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
                Assert.True(ex.Message == $"Could not create new robot in database as area '{area.AreaName}' does not exist in installation {wrongInstallation.InstallationCode}");
            }
        }

    }
}
