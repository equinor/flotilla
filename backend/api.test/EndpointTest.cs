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
                                services.AddScoped<IBlobService, MockBlobService>();
                                services.AddScoped<IStidService, MockStidService>();
                                services.AddScoped<ICustomMissionService, MockCustomMissionService>();
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

        private async Task<(string installationId, string plantId, string deckId, string areaId)> PopulateAreaDb(string installationCode, string plantCode, string deckName, string areaName)
        {
            string installationUrl = $"/installations";
            string plantUrl = $"/plants";
            string deckUrl = $"/decks";
            string areaUrl = $"/areas";
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

            var installationQuery = new CreateInstallationQuery
            {
                InstallationCode = installationCode,
                Name = installationCode
            };

            var plantQuery = new CreatePlantQuery
            {
                InstallationCode = installationCode,
                PlantCode = plantCode,
                Name = plantCode
            };

            var deckQuery = new CreateDeckQuery
            {
                InstallationCode = installationCode,
                PlantCode = plantCode,
                Name = deckName
            };

            var areaQuery = new CreateAreaQuery
            {
                InstallationCode = installationCode,
                PlantCode = plantCode,
                DeckName = deckName,
                AreaName = areaName,
                DefaultLocalizationPose = testPose
            };

            var installationContent = new StringContent(
                JsonSerializer.Serialize(installationQuery),
                null,
                "application/json"
            );

            var plantContent = new StringContent(
                JsonSerializer.Serialize(plantQuery),
                null,
                "application/json"
            );

            var deckContent = new StringContent(
                JsonSerializer.Serialize(deckQuery),
                null,
                "application/json"
            );

            var areaContent = new StringContent(
                JsonSerializer.Serialize(areaQuery),
                null,
                "application/json"
            );

            // Act
            var installationResponse = await _client.PostAsync(installationUrl, installationContent);
            Assert.NotNull(installationResponse);
            Assert.True(installationResponse.IsSuccessStatusCode);
            var installation = await installationResponse.Content.ReadFromJsonAsync<Installation>(_serializerOptions);
            Assert.NotNull(installation);

            var plantResponse = await _client.PostAsync(plantUrl, plantContent);
            Assert.NotNull(plantResponse);
            Assert.True(plantResponse.IsSuccessStatusCode);
            var plant = await plantResponse.Content.ReadFromJsonAsync<Plant>(_serializerOptions);
            Assert.NotNull(plant);

            var deckResponse = await _client.PostAsync(deckUrl, deckContent);
            Assert.NotNull(deckResponse);
            Assert.True(deckResponse.IsSuccessStatusCode);
            var deck = await deckResponse.Content.ReadFromJsonAsync<Deck>(_serializerOptions);
            Assert.NotNull(deck);

            var areaResponse = await _client.PostAsync(areaUrl, areaContent);
            Assert.NotNull(areaResponse);
            Assert.True(areaResponse.IsSuccessStatusCode);
            var area = await areaResponse.Content.ReadFromJsonAsync<Area>(_serializerOptions);
            Assert.NotNull(area);

            return (installation.Id, plant.Id, deck.Id, area.Id);
        }

        #region MissionsController
        [Fact]
        public async Task MissionsTest()
        {
            // Arrange
            string robotUrl = "/robots";
            string missionsUrl = "/missions";
            var robotResponse = await _client.GetAsync(robotUrl);
            Assert.True(robotResponse.IsSuccessStatusCode);
            var robots = await robotResponse.Content.ReadFromJsonAsync<List<Robot>>(_serializerOptions);
            Assert.True(robots != null);
            var robot = robots[0];
            string robotId = robot.Id;
            string testInstallation = "TestInstallationMissionsTest";
            string testPlant = "TestPlantMissionsTest";
            string testDeck = "TestDeckMissionsTest";
            string testArea = "testAreaMissionsTest";

            await PopulateAreaDb(testInstallation, testPlant, testDeck, testArea);

            int echoMissionId = 97;

            // Act
            var query = new ScheduledMissionQuery
            {
                RobotId = robotId,
                InstallationCode = testInstallation,
                AreaName = testArea,
                EchoMissionId = echoMissionId,
                DesiredStartTime = DateTimeOffset.UtcNow
            };
            var content = new StringContent(
                JsonSerializer.Serialize(query),
                null,
                "application/json"
            );

            await _client.PostAsync(missionsUrl, content);
            await _client.PostAsync(missionsUrl, content);
            await _client.PostAsync(missionsUrl, content);

            // Increasing pageSize to 50 to ensure the missions we are looking for is included
            string urlMissionRuns = $"/missions/runs?pageSize=50";
            var response = await _client.GetAsync(urlMissionRuns);
            var missionRuns = await response.Content.ReadFromJsonAsync<List<MissionRun>>(
                _serializerOptions
            );

            // Assert
            Assert.True(response.IsSuccessStatusCode);
            Assert.True(missionRuns != null);
            missionRuns = missionRuns.FindAll(m => m.Area!.Name == testArea);
            Assert.True(missionRuns.Count == 3);
        }

        [Fact]
        public async Task GetMissionById_ShouldReturnNotFound()
        {
            string missionId = "RandomString";
            string url = "/missions/runs/" + missionId;
            var response = await _client.GetAsync(url);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DeleteMission_ShouldReturnNotFound()
        {
            string missionId = "RandomString";
            string url = "/missions/runs/" + missionId;
            var response = await _client.DeleteAsync(url);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task StartMissionTest()
        {
            // Arrange
            string robotUrl = "/robots";
            string missionsUrl = "/missions";
            var response = await _client.GetAsync(robotUrl);
            Assert.True(response.IsSuccessStatusCode);
            var robots = await response.Content.ReadFromJsonAsync<List<Robot>>(_serializerOptions);
            Assert.True(robots != null);
            var robot = robots[0];
            string robotId = robot.Id;
            string testInstallation = "TestInstallationStartMissionTest";
            string testPlant = "TestPlantStartMissionTest";
            string testDeck = "TestDeckStartMissionTest";
            string testArea = "testAreaStartMissionTest";
            int echoMissionId = 95;

            await PopulateAreaDb(testInstallation, testPlant, testDeck, testArea);

            // Act
            var query = new ScheduledMissionQuery
            {
                RobotId = robotId,
                InstallationCode = testInstallation,
                AreaName = testArea,
                EchoMissionId = echoMissionId,
                DesiredStartTime = DateTimeOffset.UtcNow
            };
            var content = new StringContent(
                JsonSerializer.Serialize(query),
                null,
                "application/json"
            );

            response = await _client.PostAsync(missionsUrl, content);

            // Assert
            Assert.True(response.IsSuccessStatusCode);
            var missionRun = await response.Content.ReadFromJsonAsync<MissionRun>(_serializerOptions);
            Assert.True(missionRun != null);
            Assert.True(missionRun.Id != null);
            Assert.True(missionRun.Status == MissionStatus.Pending);
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
        public async Task AreaTest()
        {
            // Arrange
            string testInstallation = "TestInstallationAreaTest";
            string testPlant = "TestPlantAreaTest";
            string testDeck = "testDeckAreaTest";
            string testArea = "testAreaAreaTest";
            string installationUrl = $"/installations";
            string plantUrl = $"/plants";
            string deckUrl = $"/decks";
            string areaUrl = $"/areas";
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

            var installationQuery = new CreateInstallationQuery
            {
                InstallationCode = testInstallation,
                Name = testInstallation
            };

            var plantQuery = new CreatePlantQuery
            {
                InstallationCode = testInstallation,
                PlantCode = testPlant,
                Name = testPlant
            };

            var deckQuery = new CreateDeckQuery
            {
                InstallationCode = testInstallation,
                PlantCode = testPlant,
                Name = testDeck
            };

            var areaQuery = new CreateAreaQuery
            {
                InstallationCode = testInstallation,
                PlantCode = testPlant,
                DeckName = testDeck,
                AreaName = testArea,
                DefaultLocalizationPose = testPose
            };

            var installationContent = new StringContent(
                JsonSerializer.Serialize(installationQuery),
                null,
                "application/json"
            );

            var plantContent = new StringContent(
                JsonSerializer.Serialize(plantQuery),
                null,
                "application/json"
            );

            var deckContent = new StringContent(
                JsonSerializer.Serialize(deckQuery),
                null,
                "application/json"
            );

            var areaContent = new StringContent(
                JsonSerializer.Serialize(areaQuery),
                null,
                "application/json"
            );

            // Act
            var installationResponse = await _client.PostAsync(installationUrl, installationContent);
            var plantResponse = await _client.PostAsync(plantUrl, plantContent);
            var deckResponse = await _client.PostAsync(deckUrl, deckContent);
            var areaResponse = await _client.PostAsync(areaUrl, areaContent);

            // Assert
            Assert.True(installationResponse.IsSuccessStatusCode);
            Assert.True(plantResponse.IsSuccessStatusCode);
            Assert.True(deckResponse.IsSuccessStatusCode);
            Assert.True(areaResponse.IsSuccessStatusCode);
            var area = await areaResponse.Content.ReadFromJsonAsync<Area>(_serializerOptions);
            Assert.True(area != null);
        }

        [Fact]
        public async Task GetMissionsInAreaTest()
        {
            // Arrange
            string testInstallation = "TestInstallationMissionsInAreaTest";
            string testPlant = "TestPlantMissionsInAreaTest";
            string testDeck = "testDeckMissionsInAreaTest";
            string testArea = "testAreaMissionsInAreaTest";
            string testMissionName = "testMissionInAreaTest";
            string areaUrl = $"/areas";
            string missionUrl = $"/missions/custom";
            (_, _, _, string areaId) = await PopulateAreaDb(testInstallation, testPlant, testDeck, testArea);

            string url = "/robots";
            var robotResponse = await _client.GetAsync(url);
            Assert.True(robotResponse.IsSuccessStatusCode);
            var robots = await robotResponse.Content.ReadFromJsonAsync<List<Robot>>(_serializerOptions);
            Assert.True(robots != null);
            var robot = robots[0];
            string robotId = robot.Id;

            var missionQuery = new CustomMissionQuery
            {
                RobotId = robotId,
                DesiredStartTime = DateTimeOffset.UtcNow,
                InstallationCode = testInstallation,
                AreaName = testArea,
                Name = testMissionName,
                Tasks = new List<CustomTaskQuery>()
            };

            var missionContent = new StringContent(
                JsonSerializer.Serialize(missionQuery),
                null,
                "application/json"
            );

            // Act
            var missionResponse = await _client.PostAsync(missionUrl, missionContent);

            Assert.True(missionResponse.IsSuccessStatusCode);
            var mission = await missionResponse.Content.ReadFromJsonAsync<MissionRun>(_serializerOptions);
            Assert.NotNull(mission);
            Assert.NotNull(mission.MissionId);

            var areaMissionsResponse = await _client.GetAsync(areaUrl + $"/{areaId}/mission-definitions");

            // Assert
            Assert.True(areaMissionsResponse.IsSuccessStatusCode);
            var missions = await areaMissionsResponse.Content.ReadFromJsonAsync<IList<MissionRun>>(_serializerOptions);
            Assert.NotNull(missions);
            Assert.Single(missions);
            Assert.Equal(missions[0].Id, mission.MissionId);
        }

        [Fact]
        public async Task SafePositionTest()
        {
            // Arrange - Add Safe Position
            string testInstallation = "testInstallationSafePositionTest";
            string testPlant = "testPlantSafePositionTest";
            string testDeck = "testDeckSafePositionTest";
            string testArea = "testAreaSafePositionTest";
            string addSafePositionUrl = $"/areas/{testInstallation}/{testArea}/safe-position";
            var testPosition = new Position
            {
                X = 1,
                Y = 2,
                Z = 2
            };
            var query = new Pose
            {
                Position = testPosition,
                Orientation = new Orientation
                {
                    X = 0,
                    Y = 0,
                    Z = 0,
                    W = 1
                }
            };
            var content = new StringContent(
                JsonSerializer.Serialize(query),
                null,
                "application/json"
            );

            _ = await PopulateAreaDb(testInstallation, testPlant, testDeck, testArea);

            var areaResponse = await _client.PostAsync(addSafePositionUrl, content);
            Assert.True(areaResponse.IsSuccessStatusCode);
            var area = await areaResponse.Content.ReadFromJsonAsync<Area>(_serializerOptions);
            Assert.True(area != null);

            // Arrange - Get a Robot
            string url = "/robots";
            var robotResponse = await _client.GetAsync(url);
            Assert.True(robotResponse.IsSuccessStatusCode);
            var robots = await robotResponse.Content.ReadFromJsonAsync<List<Robot>>(_serializerOptions);
            Assert.True(robots != null);
            var robot = robots[0];
            string robotId = robot.Id;

            // Act
            string goToSafePositionUrl = $"/robots/{robotId}/{testInstallation}/{testArea}/go-to-safe-position";
            var missionResponse = await _client.PostAsync(goToSafePositionUrl, null);

            // Assert
            Assert.True(missionResponse.IsSuccessStatusCode);
            var missionRun = await missionResponse.Content.ReadFromJsonAsync<MissionRun>(_serializerOptions);
            Assert.True(missionRun != null);
            Assert.True(
                JsonSerializer.Serialize(missionRun.Tasks[0].RobotPose.Position) ==
                JsonSerializer.Serialize(testPosition)
            );
        }

        [Fact]
        public async Task GetMapMetadata()
        {
            string testInstallation = "testInstallationGetMapMetadata";
            string testPlant = "testPlantGetMapMetadata";
            string testDeck = "testDeckGetMapMetadata";
            string testArea = "testAreaGetMapMetadata";
            string invalidAreaId = "InvalidId";

            (_, _, _, string areaId) = await PopulateAreaDb(testInstallation, testPlant, testDeck, testArea);

            string url = $"/areas/{areaId}/map-metadata";
            var response = await _client.GetAsync(url);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            string invalidUrl = $"/areas/{invalidAreaId}/map-metadata";
            var responseInvalid = await _client.GetAsync(invalidUrl);
            Assert.Equal(HttpStatusCode.NotFound, responseInvalid.StatusCode);
        }

        [Fact]
        public async Task UpdateDefaultLocalizationPoseOnDeck()
        {
            string testInstallation = "testInstallationUpdateDefaultLocalizationPoseOnDeck";
            string testPlant = "testPlantUpdateDefaultLocalizationPoseOnDeck";
            string testDeck = "testDeckUpdateDefaultLocalizationPoseOnDeck";
            string testArea = "testAreaUpdateDefaultLocalizationPoseOnDeck";

            (_, _, string deckId, _) = await PopulateAreaDb(testInstallation, testPlant, testDeck, testArea);

            string url = $"/decks/{deckId}/update-default-localization-pose";
            var query = new Pose
            {
                Position = new Position
                {
                    X = 1,
                    Y = 2,
                    Z = 3
                },
                Orientation = new Orientation
                {
                    X = 0,
                    Y = 0,
                    Z = 0,
                    W = 1
                }
            };
            var content = new StringContent(
                JsonSerializer.Serialize(query),
                null,
                "application/json"
            );
            var response = await _client.PutAsync(url, content);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        }


        [Fact]
        public async Task GetNextRun()
        {
            // Arrange - Initialise areas
            string customMissionsUrl = "/missions/custom";
            string scheduleMissionsUrl = "/missions/schedule";

            string testInstallation = "testInstallationNextRun";
            string testPlant = "testPlantNextRun";
            string testDeck = "testDeckNextRun";
            string testArea = "testAreaNextRun";
            string testMissionName = "testMissionNextRun";

            await PopulateAreaDb(testInstallation, testPlant, testDeck, testArea);

            // Arrange - Create custom mission definition
            string robotUrl = "/robots";
            var response = await _client.GetAsync(robotUrl);
            Assert.True(response.IsSuccessStatusCode);
            var robots = await response.Content.ReadFromJsonAsync<List<Robot>>(_serializerOptions);
            Assert.True(robots != null);
            var robot = robots[0];
            string robotId = robot.Id;

            var query = new CustomMissionQuery
            {
                RobotId = robotId,
                InstallationCode = testInstallation,
                AreaName = testArea,
                DesiredStartTime = new DateTimeOffset(new DateTime(3050, 1, 1)),
                InspectionFrequency = new TimeSpan(14, 0, 0, 0),
                Name = testMissionName,
                Tasks = new List<CustomTaskQuery>()
                {
                    new CustomTaskQuery()
                    {
                        RobotPose = new Pose(),
                        Inspections = new List<CustomInspectionQuery>(),
                        InspectionTarget = new Position(),
                        TaskOrder = 0
                    }
                },
            };
            var content = new StringContent(
                JsonSerializer.Serialize(query),
                null,
                "application/json"
            );

            response = await _client.PostAsync(customMissionsUrl, content);

            Assert.True(response.IsSuccessStatusCode);
            var missionRun = await response.Content.ReadFromJsonAsync<MissionRun>(_serializerOptions);
            Assert.True(missionRun != null);
            Assert.True(missionRun.MissionId != null);
            Assert.True(missionRun.Id != null);
            Assert.True(missionRun.Status == MissionStatus.Pending);

            // Arrange - Schedule missions from mission definition
            var scheduleQuery1 = new ScheduleMissionQuery()
            {
                RobotId = robotId,
                DesiredStartTime = new DateTimeOffset(new DateTime(2050, 1, 1)),
                MissionDefinitionId = missionRun.MissionId
            };
            var scheduleContent1 = new StringContent(
                JsonSerializer.Serialize(scheduleQuery1),
                null,
                "application/json"
            );
            var scheduleQuery2 = new ScheduleMissionQuery()
            {
                RobotId = robotId,
                DesiredStartTime = DateTimeOffset.UtcNow,
                MissionDefinitionId = missionRun.MissionId
            };
            var scheduleContent2 = new StringContent(
                JsonSerializer.Serialize(scheduleQuery2),
                null,
                "application/json"
            );
            var scheduleQuery3 = new ScheduleMissionQuery()
            {
                RobotId = robotId,
                DesiredStartTime = new DateTimeOffset(new DateTime(2100, 1, 1)),
                MissionDefinitionId = missionRun.MissionId
            };
            var scheduleContent3 = new StringContent(
                JsonSerializer.Serialize(scheduleQuery3),
                null,
                "application/json"
            );
            var missionRun1Response = await _client.PostAsync(scheduleMissionsUrl, scheduleContent1);
            var missionRun2Response = await _client.PostAsync(scheduleMissionsUrl, scheduleContent2);
            var missionRun3Response = await _client.PostAsync(scheduleMissionsUrl, scheduleContent3);
            var missionRun1 = await missionRun1Response.Content.ReadFromJsonAsync<MissionRun>(_serializerOptions);
            var missionRun2 = await missionRun2Response.Content.ReadFromJsonAsync<MissionRun>(_serializerOptions);
            var missionRun3 = await missionRun3Response.Content.ReadFromJsonAsync<MissionRun>(_serializerOptions);

            // Act
            string nextMissionUrl = $"missions/definitions/{missionRun.MissionId}/next-run";
            var nextMissionResponse = await _client.GetAsync(nextMissionUrl);

            // Assert
            Assert.True(nextMissionResponse.IsSuccessStatusCode);
            var nextMissionRun = await nextMissionResponse.Content.ReadFromJsonAsync<MissionRun>(_serializerOptions);
            Assert.NotNull(nextMissionRun);
            Assert.NotNull(missionRun1);
            Assert.NotNull(missionRun2);
            Assert.NotNull(missionRun3);
            Assert.Equal(missionRun1.MissionId, missionRun.MissionId);
            Assert.Equal(missionRun2.MissionId, missionRun.MissionId);
            Assert.Equal(missionRun3.MissionId, missionRun.MissionId);
            Assert.True(nextMissionRun.Id == missionRun2.Id);
        }

        [Fact]
        public async Task ScheduleDuplicateCustomMissionDefinitions()
        {
            // Arrange - Initialise areas
            string customMissionsUrl = "/missions/custom";
            string missionDefinitionsUrl = "/missions/definitions";

            string testInstallation = "testInstallationScheduleDuplicateCustomMissionDefinitions";
            string testPlant = "testPlantScheduleDuplicateCustomMissionDefinitions";
            string testDeck = "testDeckScheduleDuplicateCustomMissionDefinitions";
            string testArea = "testAreaScheduleDuplicateCustomMissionDefinitions";
            string testMissionName = "testMissionScheduleDuplicateCustomMissionDefinitions";

            _ = await PopulateAreaDb(testInstallation, testPlant, testDeck, testArea);

            // Arrange - Create custom mission definition
            string robotUrl = "/robots";
            var response = await _client.GetAsync(robotUrl);
            Assert.True(response.IsSuccessStatusCode);
            var robots = await response.Content.ReadFromJsonAsync<List<Robot>>(_serializerOptions);
            Assert.True(robots != null);
            var robot = robots[0];
            string robotId = robot.Id;

            var query = new CustomMissionQuery
            {
                RobotId = robotId,
                InstallationCode = testInstallation,
                AreaName = testArea,
                DesiredStartTime = new DateTimeOffset(new DateTime(3050, 1, 1)),
                InspectionFrequency = new TimeSpan(14, 0, 0, 0),
                Name = testMissionName,
                Tasks = new List<CustomTaskQuery>()
                {
                    new()
                    {
                        RobotPose = new Pose(),
                        Inspections = new List<CustomInspectionQuery>(),
                        InspectionTarget = new Position(),
                        TaskOrder = 0
                    },
                    new()
                    {
                        RobotPose = new Pose(1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f),
                        Inspections = new List<CustomInspectionQuery>(),
                        InspectionTarget = new Position(),
                        TaskOrder = 1
                    }
                },
            };
            var content = new StringContent(
                JsonSerializer.Serialize(query),
                null,
                "application/json"
            );

            // Act
            var response1 = await _client.PostAsync(customMissionsUrl, content);
            var response2 = await _client.PostAsync(customMissionsUrl, content);

            // Assert
            Assert.True(response1.IsSuccessStatusCode);
            Assert.True(response2.IsSuccessStatusCode);
            var missionRun1 = await response1.Content.ReadFromJsonAsync<MissionRun>(_serializerOptions);
            var missionRun2 = await response2.Content.ReadFromJsonAsync<MissionRun>(_serializerOptions);
            Assert.NotNull(missionRun1);
            Assert.NotNull(missionRun2);
            string? missionId1 = missionRun1.MissionId;
            string? missionId2 = missionRun2.MissionId;
            Assert.Equal(missionId1, missionId2);

            var missionDefinitionsResponse = await _client.GetAsync(missionDefinitionsUrl);
            var missionDefinitions = await missionDefinitionsResponse.Content.ReadFromJsonAsync<List<MissionDefinition>>(_serializerOptions);
            Assert.NotNull(missionDefinitions);
            Assert.NotNull(missionDefinitions.Find((m) => m.Id == missionId1));
        }

        [Fact]
        public async Task ScheduleDuplicateEchoMissionDefinitions()
        {
            // Arrange - Initialise areas
            string echoMissionsUrl = "/missions";
            string missionDefinitionsUrl = "/missions/definitions";

            string testInstallation = "testInstallationScheduleDuplicateEchoMissionDefinitions";
            string testPlant = "testPlantScheduleDuplicateEchoMissionDefinitions";
            string testDeck = "testDeckScheduleDuplicateEchoMissionDefinitions";
            string testArea = "testAreaScheduleDuplicateEchoMissionDefinitions";

            _ = await PopulateAreaDb(testInstallation, testPlant, testDeck, testArea);

            // Arrange - Create echo mission definition
            string robotUrl = "/robots";
            var response = await _client.GetAsync(robotUrl);
            Assert.True(response.IsSuccessStatusCode);
            var robots = await response.Content.ReadFromJsonAsync<List<Robot>>(_serializerOptions);
            Assert.True(robots != null);
            var robot = robots[0];
            string robotId = robot.Id;
            int echoMissionId = 1; // Corresponds to mock in EchoServiceMock.cs

            var query = new ScheduledMissionQuery
            {
                RobotId = robotId,
                InstallationCode = testInstallation,
                AreaName = testArea,
                EchoMissionId = echoMissionId,
                DesiredStartTime = DateTimeOffset.UtcNow
            };
            var content = new StringContent(
                JsonSerializer.Serialize(query),
                null,
                "application/json"
            );

            // Act
            var response1 = await _client.PostAsync(echoMissionsUrl, content);
            var response2 = await _client.PostAsync(echoMissionsUrl, content);

            // Assert
            Assert.True(response1.IsSuccessStatusCode);
            Assert.True(response2.IsSuccessStatusCode);
            var missionRun1 = await response1.Content.ReadFromJsonAsync<MissionRun>(_serializerOptions);
            var missionRun2 = await response2.Content.ReadFromJsonAsync<MissionRun>(_serializerOptions);
            Assert.NotNull(missionRun1);
            Assert.NotNull(missionRun2);
            string? missionId1 = missionRun1.MissionId;
            string? missionId2 = missionRun2.MissionId;
            Assert.Equal(missionId1, missionId2);

            var missionDefinitionsResponse = await _client.GetAsync(missionDefinitionsUrl);
            var missionDefinitions = await missionDefinitionsResponse.Content.ReadFromJsonAsync<List<MissionDefinition>>(_serializerOptions);
            Assert.NotNull(missionDefinitions);
            Assert.NotNull(missionDefinitions.Find((m) => m.Id == missionId1));
        }
    }
}
