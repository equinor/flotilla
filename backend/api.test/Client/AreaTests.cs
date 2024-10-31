using System;
using System.Collections.Generic;
using System.Linq;
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
using Xunit;
namespace Api.Test.Client
{
    [Collection("Database collection")]
    public class AreaTests : IClassFixture<TestWebApplicationFactory<Program>>
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

            object? context = factory.Services.GetService(typeof(FlotillaDbContext)) as FlotillaDbContext ?? throw new ArgumentNullException(nameof(factory));
            _databaseUtilities = new DatabaseUtilities((FlotillaDbContext)context);

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
            var area = await areaResponse.Content.ReadFromJsonAsync<AreaResponse>(_serializerOptions);
            Assert.NotNull(area);
        }

        [Fact]
        public async Task MissionIsCreatedInArea()
        {
            // Arrange - Initialise area
            var installation = await _databaseUtilities.ReadOrNewInstallation();
            var plant = await _databaseUtilities.ReadOrNewPlant(installation.InstallationCode);
            var deck = await _databaseUtilities.ReadOrNewDeck(installation.InstallationCode, plant.PlantCode);
            var area = await _databaseUtilities.ReadOrNewArea(installation.InstallationCode, plant.PlantCode, deck.Name);

            // Arrange - Robot
            var robot = await _databaseUtilities.NewRobot(RobotStatus.Available, installation);
            string robotId = robot.Id;

            string testMissionName = "testMissionInAreaTest";

            var inspection = new CustomInspectionQuery
            {
                AnalysisType = AnalysisType.CarSeal,
                InspectionTarget = new Position(),
                InspectionType = InspectionType.Image
            };
            var tasks = new List<CustomTaskQuery>
            {
                new()
                {
                    Inspection = inspection,
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
                AreaName = area.Name,
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
            string areaUrl = "/areas";
            var areaMissionsResponse = await _client.GetAsync(areaUrl + $"/{area.Id}/mission-definitions");

            // Assert
            Assert.True(areaMissionsResponse.IsSuccessStatusCode);
            var missions = await areaMissionsResponse.Content.ReadFromJsonAsync<IList<MissionDefinitionResponse>>(_serializerOptions);
            Assert.NotNull(missions);
            Assert.Single(missions.Where(m => m.Id.Equals(mission.MissionId, StringComparison.Ordinal)));
        }

        [Fact]
        public async Task EmergencyDockTest()
        {
            // Arrange
            var installation = await _databaseUtilities.ReadOrNewInstallation();
            var plant = await _databaseUtilities.ReadOrNewPlant(installation.InstallationCode);
            var deck = await _databaseUtilities.ReadOrNewDeck(installation.InstallationCode, plant.PlantCode);
            var area = await _databaseUtilities.ReadOrNewArea(installation.InstallationCode, plant.PlantCode, deck.Name);
            string installationCode = installation.InstallationCode;


            // Act
            string goToDockingPositionUrl = $"/emergency-action/{installationCode}/abort-current-missions-and-send-all-robots-to-safe-zone";
            var missionResponse = await _client.PostAsync(goToDockingPositionUrl, null);

            // Assert
            Assert.True(missionResponse.IsSuccessStatusCode);

            // The endpoint posted to above triggers an event and returns a successful response.
            // The test finishes and disposes of objects, but the operations of that event handler are still running, leading to a crash.
            await Task.Delay(5000);
        }

        [Fact]
        public async Task UpdateDefaultLocalizationPoseOnDeck()
        {
            // Arrange
            var installation = await _databaseUtilities.ReadOrNewInstallation();
            var plant = await _databaseUtilities.ReadOrNewPlant(installation.InstallationCode);
            var deck = await _databaseUtilities.ReadOrNewDeck(installation.InstallationCode, plant.PlantCode);

            string deckId = deck.Id;

            string url = $"/decks/{deckId}/update-default-localization-pose";
            var query = new CreateDefaultLocalizationPose
            {
                Pose = new Pose
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
                }
            };
            var content = new StringContent(
                JsonSerializer.Serialize(query),
                null,
                "application/json"
            );

            // Act
            var putResponse = await _client.PutAsync(url, content);
            Assert.True(putResponse.IsSuccessStatusCode);
            var putDeck = await putResponse.Content.ReadFromJsonAsync<DeckResponse>(_serializerOptions);

            // Assert
            Assert.NotNull(putDeck);
            Assert.NotNull(putDeck.DefaultLocalizationPose);
            Assert.True(putDeck.DefaultLocalizationPose.Position.Z.Equals(query.Pose.Position.Z));
            Assert.True(putDeck.DefaultLocalizationPose.Orientation.W.Equals(query.Pose.Orientation.W));
        }
    }
}
