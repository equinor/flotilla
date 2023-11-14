using System;
using System.Collections.Generic;
using System.Linq;
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
using Xunit;
namespace Api.Test
{
    [Collection("Database collection")]
    public class AreaTests : IClassFixture<TestWebApplicationFactory<Program>>
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

        public AreaTests(TestWebApplicationFactory<Program> factory)
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
        public async Task AreaTest()
        {
            // Arrange
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

            string testInstallation = "TestInstallationAreaTest";
            var installationQuery = new CreateInstallationQuery
            {
                InstallationCode = testInstallation,
                Name = testInstallation
            };

            string testPlant = "TestPlantAreaTest";
            var plantQuery = new CreatePlantQuery
            {
                InstallationCode = testInstallation,
                PlantCode = testPlant,
                Name = testPlant
            };

            string testDeck = "testDeckAreaTest";
            var deckQuery = new CreateDeckQuery
            {
                InstallationCode = testInstallation,
                PlantCode = testPlant,
                Name = testDeck
            };

            string testArea = "testAreaAreaTest";
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
            string installationUrl = "/installations";
            var installationResponse = await _client.PostAsync(installationUrl, installationContent);
            string plantUrl = "/plants";
            var plantResponse = await _client.PostAsync(plantUrl, plantContent);
            string deckUrl = "/decks";
            var deckResponse = await _client.PostAsync(deckUrl, deckContent);
            string areaUrl = "/areas";
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
            // Robot
            string robotUrl = "/robots";
            var robotResponse = await _client.GetAsync(robotUrl);
            Assert.True(robotResponse.IsSuccessStatusCode);
            var robots = await robotResponse.Content.ReadFromJsonAsync<List<Robot>>(_serializerOptions);
            Assert.True(robots != null);
            var robot = robots[0];
            string robotId = robot.Id;

            // Installation
            string installationUrl = "/installations";
            var installationResponse = await _client.GetAsync(installationUrl);
            Assert.True(installationResponse.IsSuccessStatusCode);
            var installations = await installationResponse.Content.ReadFromJsonAsync<List<Installation>>(_serializerOptions);
            Assert.True(installations != null);
            var installation = installations[0];

            // Area
            string areaUrl = "/areas";
            var areaResponse = await _client.GetAsync(areaUrl);
            Assert.True(areaResponse.IsSuccessStatusCode);
            var areas = await areaResponse.Content.ReadFromJsonAsync<List<AreaResponse>>(_serializerOptions);
            Assert.True(areas != null);
            var area = areas[0];
            string areaId = area.Id;

            string testMissionName = "testMissionInAreaTest";

            var inspections = new List<CustomInspectionQuery>
            {
                new()
                {
                    AnalysisType = AnalysisType.CarSeal,
                    InspectionTarget = new Position(),
                    InspectionType = InspectionType.Image
                }
            };
            var tasks = new List<CustomTaskQuery>
            {
                new()
                {
                    Inspections = inspections,
                    InspectionTarget = new Position(),
                    TagId = "test",
                    RobotPose = new Pose(),
                    TaskOrder = 0
                }
            };
            var missionQuery = new CustomMissionQuery
            {
                RobotId = robotId,
                DesiredStartTime = DateTime.UtcNow,
                InstallationCode = installation.InstallationCode,
                AreaName = area.AreaName,
                Name = testMissionName,
                Tasks = tasks
            };

            var missionContent = new StringContent(
                JsonSerializer.Serialize(missionQuery),
                null,
                "application/json"
            );

            // Act
            string missionUrl = "/missions/custom";
            var missionResponse = await _client.PostAsync(missionUrl, missionContent);

            Assert.True(missionResponse.IsSuccessStatusCode);
            var mission = await missionResponse.Content.ReadFromJsonAsync<MissionRun>(_serializerOptions);
            Assert.NotNull(mission);
            Assert.NotNull(mission.MissionId);

            var areaMissionsResponse = await _client.GetAsync(areaUrl + $"/{areaId}/mission-definitions");

            // Assert
            Assert.True(areaMissionsResponse.IsSuccessStatusCode);
            var missions = await areaMissionsResponse.Content.ReadFromJsonAsync<IList<CondensedMissionDefinitionResponse>>(_serializerOptions);
            Assert.NotNull(missions);
            Assert.Single(missions.Where(m => m.Id.Equals(mission.MissionId, StringComparison.Ordinal)));
        }

        [Fact]
        public async Task SafePositionTest()
        {
            // Arrange
            string areaUrl = "/areas";
            var areaResponse = await _client.GetAsync(areaUrl);
            Assert.True(areaResponse.IsSuccessStatusCode);
            var areaResponses = await areaResponse.Content.ReadFromJsonAsync<List<AreaResponse>>(_serializerOptions);
            Assert.True(areaResponses != null);
            var area = areaResponses[0];
            string areaName = area.AreaName;
            string installationCode = area.InstallationCode;

            string addSafePositionUrl = $"/areas/{installationCode}/{areaName}/safe-position";
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

            areaResponse = await _client.PostAsync(addSafePositionUrl, content);
            Assert.True(areaResponse.IsSuccessStatusCode);
            var areaContent = await areaResponse.Content.ReadFromJsonAsync<Area>(_serializerOptions);
            Assert.True(areaContent != null);

            // Act
            string goToSafePositionUrl = $"/emergency-action/{installationCode}/abort-current-missions-and-send-all-robots-to-safe-zone";
            var missionResponse = await _client.PostAsync(goToSafePositionUrl, null);

            // Assert
            Assert.True(missionResponse.IsSuccessStatusCode);

        }

        [Fact]
        public async Task GetMapMetadataNotFound()
        {
            // Arrange
            string areaUrl = "/areas";
            var response = await _client.GetAsync(areaUrl);
            Assert.True(response.IsSuccessStatusCode);
            var areas = await response.Content.ReadFromJsonAsync<List<AreaResponse>>(_serializerOptions);
            Assert.True(areas != null);
            var areaResponse = areas[0];
            string areaId = areaResponse.Id;
            string invalidAreaId = "InvalidId";

            // Act
            string url = $"/areas/{areaId}/map-metadata";
            response = await _client.GetAsync(url);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Assert
            string invalidUrl = $"/areas/{invalidAreaId}/map-metadata";
            var responseInvalid = await _client.GetAsync(invalidUrl);
            Assert.Equal(HttpStatusCode.NotFound, responseInvalid.StatusCode);
        }

        [Fact]
        public async Task UpdateDefaultLocalizationPoseOnDeck()
        {
            string deckUrl = "/decks";
            var deckResponse = await _client.GetAsync(deckUrl);
            Assert.True(deckResponse.IsSuccessStatusCode);
            var decks = await deckResponse.Content.ReadFromJsonAsync<List<DeckResponse>>(_serializerOptions);
            Assert.True(decks != null);
            var deck = decks[0];
            string deckId = deck.Id;

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
            var putResponse = await _client.PutAsync(url, content);
            Assert.True(putResponse.IsSuccessStatusCode);
            var putDeck = await putResponse.Content.ReadFromJsonAsync<DeckResponse>(_serializerOptions);
            Assert.True(putDeck != null);
            Assert.True(putDeck.DefaultLocalizationPose != null);
            Assert.True(putDeck.DefaultLocalizationPose.Position.Z.Equals(query.Position.Z));
            Assert.True(putDeck.DefaultLocalizationPose.Orientation.W.Equals(query.Orientation.W));
        }
    }
}
