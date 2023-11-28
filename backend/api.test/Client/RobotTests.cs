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
using Microsoft.AspNetCore.Mvc.Testing;
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
            Assert.True(robots != null && robots.Count == 3);
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
    }
}
