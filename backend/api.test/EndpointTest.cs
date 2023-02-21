using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Api.Controllers.Models;
using Api.Database.Models;
using Api.Services;
using Api.Test.Mocks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Api.Test
{
    [Collection("Database collection")]
    public class EndpointTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
    {
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _serializerOptions = new()
        {
            Converters = { new JsonStringEnumConverter() },
            PropertyNameCaseInsensitive = true
        };

        public EndpointTests(WebApplicationFactory<Program> factory)
        {
            string projectDir = Directory.GetCurrentDirectory();
            string configPath = Path.Combine(projectDir, "appsettings.Test.json");
            var client = factory
                .WithWebHostBuilder(
                    builder =>
                    {
                        var configuration = new ConfigurationBuilder()
                            .AddJsonFile(configPath)
                            .Build();
                        builder.UseEnvironment("Test");
                        builder.ConfigureAppConfiguration(
                            (context, config) =>
                            {
                                config.AddJsonFile(configPath).AddEnvironmentVariables();
                            }
                        );
                        builder.ConfigureTestServices(
                            services =>
                            {
                                services.AddScoped<IIsarService, MockIsarService>();
                                services.AddScoped<IEchoService, MockEchoService>();
                                services.AddScoped<IMapService, MockMapService>();
                                services.AddAuthorization(
                                    options =>
                                    {
                                        options.FallbackPolicy = new AuthorizationPolicyBuilder(
                                            TestAuthHandler.AuthenticationScheme
                                        )
                                            .RequireAuthenticatedUser()
                                            .RequireRole(
                                                configuration.GetSection("Authorization")["Roles"]
                                            )
                                            .Build();
                                    }
                                );
                                services
                                    .AddAuthentication(
                                        options =>
                                        {
                                            options.DefaultAuthenticateScheme =
                                                TestAuthHandler.AuthenticationScheme;
                                            options.DefaultChallengeScheme =
                                                TestAuthHandler.AuthenticationScheme;
                                        }
                                    )
                                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                                        TestAuthHandler.AuthenticationScheme,
                                        options => { }
                                    );
                            }
                        );
                    }
                )
                .CreateClient(
                    new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, }
                );
            client.BaseAddress = new Uri("https://localhost:8000");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                TestAuthHandler.AuthenticationScheme
            );
            _client = client;
        }

        public void Dispose()
        {
            _client.Dispose();
            GC.SuppressFinalize(this);
        }

        [Fact]
        public async Task MissionsTest()
        {
            string url = "/missions";
            var response = await _client.GetAsync(url);
            var missions = await response.Content.ReadFromJsonAsync<List<Mission>>(
                _serializerOptions
            );
            Assert.True(response.IsSuccessStatusCode);
            Assert.True(missions != null && missions.Count == 3);
        }

        [Fact]
        public async Task RobotsTest()
        {
            string url = "/robots";
            var response = await _client.GetAsync(url);
            var robots = await response.Content.ReadFromJsonAsync<List<Robot>>(
                _serializerOptions
            );
            Assert.True(response.IsSuccessStatusCode);
            Assert.True(robots != null && robots.Count == 3);
        }

        [Fact]
        public async Task StartMissionTest()
        {
            // Arrange
            string url = "/robots";
            var response = await _client.GetAsync(url);
            Assert.True(response.IsSuccessStatusCode);
            var robots = await response.Content.ReadFromJsonAsync<List<Robot>>(
                _serializerOptions
            );
            Assert.True(robots != null);
            var robot = robots[0];
            string robotId = robot.Id;

            // Act
            url = "/missions";
            var query = new ScheduledMissionQuery
            {
                RobotId = robotId,
                AssetCode = "test",
                EchoMissionId = 95,
                StartTime = DateTimeOffset.UtcNow
            };
            var content = new StringContent(
                      JsonSerializer.Serialize(query),
                      null,
                      "application/json"
                  );
            response = await _client.PostAsync(url, content);

            // Assert
            Assert.True(response.IsSuccessStatusCode);
            var mission = await response.Content.ReadFromJsonAsync<Mission>(
                _serializerOptions
            );
            Assert.True(mission != null);
            Assert.True(mission.Id != null);
            Assert.True(mission.MissionStatus == MissionStatus.Pending);
        }
    }
}
