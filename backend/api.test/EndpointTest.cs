using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
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
        private readonly JsonSerializerOptions _serializerOptions =
            new()
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

        #region MissionsController
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
        public async Task GetMissionById_ShouldReturnNotFound()
        {
            string missionId = "RandomString";
            string url = "/missions/" + missionId;
            var response = await _client.GetAsync(url);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DeleteMission_ShouldReturnNotFound()
        {
            string missionId = "RandomString";
            string url = "/missions/" + missionId;
            var response = await _client.DeleteAsync(url);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        #endregion MissionsController

        #region RobotController
        [Fact]
        public async Task RobotsTest()
        {
            string url = "/robots";
            var response = await _client.GetAsync(url);
            var robots = await response.Content.ReadFromJsonAsync<List<Robot>>(_serializerOptions);
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
            var robots = await response.Content.ReadFromJsonAsync<List<Robot>>(_serializerOptions);
            Assert.NotNull(robots);

            string robotId = robots[0].Id;

            var robotResponse = await _client.GetAsync("/robots/" + robotId);
            var robot = await robotResponse.Content.ReadFromJsonAsync<Robot>(_serializerOptions);
            Assert.Equal(HttpStatusCode.OK, robotResponse.StatusCode);
            Assert.NotNull(robot);
            Assert.Equal(robot.Id, robotId);
        }
        #endregion RobotController

        [Fact]
        public async Task StartMissionTest()
        {
            // Arrange
            string url = "/robots";
            var response = await _client.GetAsync(url);
            Assert.True(response.IsSuccessStatusCode);
            var robots = await response.Content.ReadFromJsonAsync<List<Robot>>(_serializerOptions);
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
                DesiredStartTime = DateTimeOffset.UtcNow
            };
            var content = new StringContent(
                JsonSerializer.Serialize(query),
                null,
                "application/json"
            );
            response = await _client.PostAsync(url, content);

            // Assert
            Assert.True(response.IsSuccessStatusCode);
            var mission = await response.Content.ReadFromJsonAsync<Mission>(_serializerOptions);
            Assert.True(mission != null);
            Assert.True(mission.Id != null);
            Assert.True(mission.Status == MissionStatus.Pending);
        }

        [Fact]
        public async Task AssetDeckTest()
        {
            // Arrange
            string testAsset = "testAsset";
            string testDeck = "testDeck";
            string assetDeckUrl = $"/asset-decks";
            var testPose = new Pose
            {
                Position = new Position
                {
                    X = 1,
                    Y = 2,
                    Z = 2
                },
                Orientation = new Orientation
                {
                    X = 0,
                    Y = 0,
                    Z = 0,
                    W = 1
                }
            };

            var query = new CreateAssetDeckQuery
            {
                AssetCode = testAsset,
                DeckName = testDeck,
                DefaultLocalizationPose = testPose
            };

            var content = new StringContent(
                JsonSerializer.Serialize(query),
                null,
                "application/json"
            );

            // Act
            var assetDeckResponse = await _client.PostAsync(assetDeckUrl, content);

            // Assert
            Assert.True(assetDeckResponse.IsSuccessStatusCode);
            var assetDeck = await assetDeckResponse.Content.ReadFromJsonAsync<AssetDeck>(_serializerOptions);
            Assert.True(assetDeck != null);
        }

        [Fact]
        public async Task SafePositionTest()
        {
            // Arrange - Add Safe Position
            string testAsset = "jsv";
            string testDeck = "P1 - AP320";
            string addSafePositionUrl = $"/asset-decks/{testAsset}/{testDeck}/safe-position";
            var testPosition = new Position
            {
                X = 311.097F,
                Y = 85.818F,
                Z = 526.85F
            };
            var query = new Pose
            {
                Position = testPosition,
                Orientation = new Orientation
                {
                    X = 0,
                    Y = 0,
                    Z = 0.7071F,
                    W = 0.7071F
                }
            };
            var content = new StringContent(
                JsonSerializer.Serialize(query),
                null,
                "application/json"
            );
            var assetDeckResponse = await _client.PostAsync(addSafePositionUrl, content);
            Assert.True(assetDeckResponse.IsSuccessStatusCode);
            var assetDeck = await assetDeckResponse.Content.ReadFromJsonAsync<AssetDeck>(_serializerOptions);
            Assert.True(assetDeck != null);

            // Arrange - Get a Robot
            string url = "/robots";
            var robotResponse = await _client.GetAsync(url);
            Assert.True(robotResponse.IsSuccessStatusCode);
            var robots = await robotResponse.Content.ReadFromJsonAsync<List<Robot>>(_serializerOptions);
            Assert.True(robots != null);
            var robot = robots[0];
            string robotId = robot.Id;

            // Arrange - Add asset deck to the robot
            robot.CurrentAssetDeck = assetDeck;
            string urlAssetDeck = $"/robots/{robotId}";
            var contentAssetDeck = new StringContent(
                JsonSerializer.Serialize(robot),
                null,
                "application/json"
            );
            await _client.PutAsync(urlAssetDeck, contentAssetDeck);

            string urlSafePosition = $"/robots/{robotId}/go-to-safe-position";
            var contentSafePosition = new StringContent(
                JsonSerializer.Serialize(robotId),
                null,
                "application/json"
            );
            var missionResponse = await _client.PostAsync(urlSafePosition, contentSafePosition);

            // Assert
            Assert.True(missionResponse != null);
            var mission = await missionResponse.Content.ReadFromJsonAsync<Mission>(_serializerOptions);
            Assert.True(mission != null);
            Assert.True(
                JsonSerializer.Serialize(mission.Tasks[0].RobotPose.Position) ==
                JsonSerializer.Serialize(testPosition)
            );
        }

        [Fact]
        public async Task GetMapMetadata()
        {
            var inputOutputPairs = new Dictionary<string, HttpStatusCode>(){
                {"TestId", HttpStatusCode.OK},
                {"InvalidId", HttpStatusCode.NotFound}
            };

            foreach (string input in inputOutputPairs.Keys)
            {
                string assetDeckId = input;
                string url = $"/asset-decks/{assetDeckId}/map-metadata";
                var response = await _client.GetAsync(url);
                Assert.Equal(inputOutputPairs[input], response.StatusCode);

            }
        }
    }
}
