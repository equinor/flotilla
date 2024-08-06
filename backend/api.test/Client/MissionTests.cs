using System;
using System.Collections.Generic;
using System.Globalization;
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
using Xunit.Sdk;
namespace Api.Test
{
    [Collection("Database collection")]
    public class MissionTests : IClassFixture<TestWebApplicationFactory<Program>>
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

        public MissionTests(TestWebApplicationFactory<Program> factory)
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

        private async Task<T> PostToDb<T, TQueryType>(string postUrl, TQueryType stringContent)
        {
            var content = new StringContent(
                JsonSerializer.Serialize(stringContent),
                null,
                "application/json"
            );
            var response = await _client.PostAsync(postUrl, content);
            Assert.True(response != null, $"Failed to post to {postUrl}. Null returned");
            Assert.True(response.IsSuccessStatusCode, $"Failed to post to {postUrl}. Status code: {response.StatusCode}");
            var responseObject = await response.Content.ReadFromJsonAsync<T>(_serializerOptions);
            Assert.True(responseObject != null, $"No object returned from post to {postUrl}");
            return responseObject;
        }

        private async Task<Installation?> GetInstallation(string installationCode)
        {
            string installationUrl = "/installations";
            var installationResponse = await _client.GetAsync(installationUrl);
            Assert.True(installationResponse.IsSuccessStatusCode);
            var installationResponses = await installationResponse.Content.ReadFromJsonAsync<List<Installation>>(_serializerOptions);
            Assert.NotNull(installationResponses);
            return installationResponses.Where((i) => i.InstallationCode.Equals(installationCode, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
        }

        private async Task<Plant?> GetPlant(string installationCode, string plantCode)
        {
            string installationUrl = "/installations";
            var installationResponse = await _client.GetAsync(installationUrl);
            Assert.True(installationResponse.IsSuccessStatusCode);
            var installationResponses = await installationResponse.Content.ReadFromJsonAsync<List<Installation>>(_serializerOptions);
            Assert.NotNull(installationResponses);
            if (!installationResponses.Where((i) => i.InstallationCode.Equals(installationCode, StringComparison.OrdinalIgnoreCase)).Any()) return null;

            string plantUrl = "/plants";
            var plantResponse = await _client.GetAsync(plantUrl);
            Assert.True(plantResponse.IsSuccessStatusCode);
            var plantResponses = await plantResponse.Content.ReadFromJsonAsync<List<Plant>>(_serializerOptions);
            Assert.NotNull(plantResponses);
            return plantResponses.Where((p) => p.PlantCode.Equals(plantCode, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
        }

        private async Task<DeckResponse?> GetDeck(string installationCode, string plantCode, string deckName)
        {
            string installationUrl = "/installations";
            var installationResponse = await _client.GetAsync(installationUrl);
            Assert.True(installationResponse.IsSuccessStatusCode);
            var installationResponses = await installationResponse.Content.ReadFromJsonAsync<List<Installation>>(_serializerOptions);
            Assert.NotNull(installationResponses);
            if (!installationResponses.Where((i) => i.InstallationCode.Equals(installationCode.ToLower(CultureInfo.CurrentCulture), StringComparison.OrdinalIgnoreCase)).Any()) return null;

            string plantUrl = "/plants";
            var plantResponse = await _client.GetAsync(plantUrl);
            Assert.True(plantResponse.IsSuccessStatusCode);
            var plantResponses = await plantResponse.Content.ReadFromJsonAsync<List<Plant>>(_serializerOptions);
            Assert.NotNull(plantResponses);
            if (!plantResponses.Where((p) => p.PlantCode.Equals(plantCode.ToLower(CultureInfo.CurrentCulture), StringComparison.OrdinalIgnoreCase)).Any()) return null;

            string deckUrl = "/decks";
            var deckResponse = await _client.GetAsync(deckUrl);
            Assert.True(deckResponse.IsSuccessStatusCode);
            var deckResponses = await deckResponse.Content.ReadFromJsonAsync<List<DeckResponse>>(_serializerOptions);
            Assert.NotNull(deckResponses);
            return deckResponses.Where((d) => d.DeckName.Equals(deckName.ToLower(CultureInfo.CurrentCulture), StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
        }

        private async Task VerifyNonDuplicateAreaDbNames(string areaName)
        {
            string areaUrl = "/areas";
            var areaResponse = await _client.GetAsync(areaUrl);
            Assert.True(areaResponse.IsSuccessStatusCode);
            var areaResponses = await areaResponse.Content.ReadFromJsonAsync<List<AreaResponse>>(_serializerOptions);
            Assert.NotNull(areaResponses);
            Assert.False(areaResponses.Where((a) => a.AreaName == areaName).Any(), $"Duplicate area name detected: {areaName}");
        }

        private async Task VerifyNonDuplicateInstallationDbName(string installationCode)
        {
            string installationUrl = "/installations";
            var installationResponse = await _client.GetAsync(installationUrl);
            Assert.True(installationResponse.IsSuccessStatusCode);
            var installationResponses = await installationResponse.Content.ReadFromJsonAsync<List<Installation>>(_serializerOptions);
            Assert.NotNull(installationResponses);
            Assert.False(installationResponses.Where((i) => i.InstallationCode == installationCode).Any(), $"Duplicate installation name detected: {installationCode}");
        }

        private static (StringContent installationContent, StringContent plantContent, StringContent deckContent, StringContent areaContent) ArrangeAreaPostQueries(string installationCode, string plantCode, string deckName, string areaName)
        {
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
                Name = deckName,
                DefaultLocalizationPose = new CreateDefaultLocalizationPose()
                {
                    Pose = testPose,
                    IsDockingStation = false
                }
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

            return (installationContent, plantContent, deckContent, areaContent);
        }

        private async Task<T> PostToDb<T>(string postUrl, StringContent content)
        {
            var response = await _client.PostAsync(postUrl, content);
            Assert.True(response != null, $"Failed to post to {postUrl}. Null returned");
            Assert.True(response.IsSuccessStatusCode, $"Failed to post to {postUrl}. Status code: {response.StatusCode}");
            var responseObject = await response.Content.ReadFromJsonAsync<T>(_serializerOptions);
            Assert.True(responseObject != null, $"No object returned from post to {postUrl}");
            return responseObject;
        }

        private async Task<(Installation installation, Plant plant, DeckResponse deck, AreaResponse area)> PostAssetInformationToDb(string installationCode, string plantCode, string deckName, string areaName)
        {
            await VerifyNonDuplicateAreaDbNames(areaName);

            string installationUrl = "/installations";
            string plantUrl = "/plants";
            string deckUrl = "/decks";
            string areaUrl = "/areas";

            (var installationContent, var plantContent, var deckContent, var areaContent) = ArrangeAreaPostQueries(installationCode, plantCode, deckName, areaName);

            var installation = await GetInstallation(installationCode) ?? await PostToDb<Installation>(installationUrl, installationContent);
            var plant = await GetPlant(installation.InstallationCode, plantCode) ?? await PostToDb<Plant>(plantUrl, plantContent);
            var deck = await GetDeck(installation.InstallationCode, plant.PlantCode, deckName) ?? await PostToDb<DeckResponse>(deckUrl, deckContent);
            var area = await PostToDb<AreaResponse>(areaUrl, areaContent);

            return (installation, plant, deck, area);
        }

        private async Task<Installation> PostInstallationInformationToDb(string installationCode)
        {
            await VerifyNonDuplicateInstallationDbName(installationCode);

            string installationUrl = "/installations";

            var installationQuery = new CreateInstallationQuery
            {
                InstallationCode = installationCode,
                Name = installationCode
            };

            var installationContent = new StringContent(
                JsonSerializer.Serialize(installationQuery),
                null,
                "application/json"
            );

            var installation = await PostToDb<Installation>(installationUrl, installationContent);

            return installation;
        }

        [Fact]
        public async Task ScheduleOneMissionTest()
        {
            // Arrange - Robot
            string robotUrl = "/robots";
            string missionsUrl = "/missions";
            var response = await _client.GetAsync(robotUrl);
            Assert.True(response.IsSuccessStatusCode, $"Failed to get robot from path: {robotUrl}, with status code {response.StatusCode}");
            var robots = await response.Content.ReadFromJsonAsync<List<Robot>>(_serializerOptions);
            Assert.NotNull(robots);
            var robot = robots.Where(robot => robot.Name == "Shockwave").First();
            string robotId = robot.Id;

            // Arrange - Area
            string installationCode = "installationScheduleOneMissionTest";
            string plantCode = "plantScheduleOneMissionTest";
            string deckName = "deckScheduleOneMissionTest";
            string areaName = "areaScheduleOneMissionTest";
            (_, _, _, _) = await PostAssetInformationToDb(installationCode, plantCode, deckName, areaName);

            string missionId = "95";

            // Act
            var query = new ScheduledMissionQuery
            {
                RobotId = robotId,
                InstallationCode = installationCode,
                AreaName = areaName,
                MissionId = missionId,
                DesiredStartTime = DateTime.UtcNow
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
            Assert.NotNull(missionRun);
            Assert.NotNull(missionRun.Id);
            Assert.Equal(MissionStatus.Pending, missionRun.Status);
        }

        [Fact]
        public async Task Schedule3MissionsTest()
        {
            // Arrange - Robot
            string robotUrl = "/robots";
            string missionsUrl = "/missions";
            var robotResponse = await _client.GetAsync(robotUrl);
            Assert.True(robotResponse.IsSuccessStatusCode);
            var robots = await robotResponse.Content.ReadFromJsonAsync<List<Robot>>(_serializerOptions);
            Assert.NotNull(robots);
            var robot = robots.Where(robot => robot.Name == "Shockwave").First();
            string robotId = robot.Id;

            // Arrange - Area
            string installationCode = "installationSchedule3MissionsTest";
            string plantCode = "plantSchedule3MissionsTest";
            string deckName = "deckSchedule3MissionsTest";
            string areaName = "areaSchedule3MissionsTest";
            (_, _, _, _) = await PostAssetInformationToDb(installationCode, plantCode, deckName, areaName);

            string missionId = "97";

            // Act
            var query = new ScheduledMissionQuery
            {
                RobotId = robotId,
                InstallationCode = installationCode,
                AreaName = areaName,
                MissionId = missionId,
                DesiredStartTime = DateTime.UtcNow
            };
            var content = new StringContent(
                JsonSerializer.Serialize(query),
                null,
                "application/json"
            );

            // Increasing pageSize to 50 to ensure the missions we are looking for is included
            string urlMissionRuns = "/missions/runs?pageSize=50";
            var response = await _client.GetAsync(urlMissionRuns);
            var missionRuns = await response.Content.ReadFromJsonAsync<List<MissionRun>>(
                _serializerOptions
            );
            Assert.True(response.IsSuccessStatusCode);
            Assert.NotNull(missionRuns);
            int missionRunsBefore = missionRuns.Count;

            response = await _client.PostAsync(missionsUrl, content);

            // Assert
            Assert.True(response.IsSuccessStatusCode);

            response = await _client.PostAsync(missionsUrl, content);
            var missionRun1 = await response.Content.ReadFromJsonAsync<MissionRun>(_serializerOptions);
            Assert.True(response.IsSuccessStatusCode);
            Assert.NotNull(missionRun1);

            response = await _client.PostAsync(missionsUrl, content);
            var missionRun2 = await response.Content.ReadFromJsonAsync<MissionRun>(_serializerOptions);
            Assert.True(response.IsSuccessStatusCode);
            Assert.NotNull(missionRun2);

            response = await _client.PostAsync(missionsUrl, content);
            var missionRun3 = await response.Content.ReadFromJsonAsync<MissionRun>(_serializerOptions);
            Assert.True(response.IsSuccessStatusCode);
            Assert.NotNull(missionRun3);

            response = await _client.GetAsync(urlMissionRuns);
            missionRuns = await response.Content.ReadFromJsonAsync<List<MissionRun>>(
                _serializerOptions
            );

            // Assert
            Assert.True(response.IsSuccessStatusCode);
            Assert.NotNull(missionRuns);
            Assert.Single(missionRuns.Where((m) => m.Id == missionRun1.Id).ToList());
            Assert.Single(missionRuns.Where((m) => m.Id == missionRun2.Id).ToList());
            Assert.Single(missionRuns.Where((m) => m.Id == missionRun3.Id).ToList());
        }

        [Fact]
        public async Task AddNonDuplicateAreasToDb()
        {
            // Arrange - Area
            string installationCode = "installationAddNonDuplicateAreasToDb";
            string plantCode = "plantAddNonDuplicateAreasToDb";
            string deckName = "deckAddNonDuplicateAreasToDb";
            string areaName = "areaAddNonDuplicateAreasToDb";
            (_, _, _, _) = await PostAssetInformationToDb(installationCode, plantCode, deckName, areaName);

            string installationCode2 = "installationAddNonDuplicateAreasToDb2";
            string plantCode2 = "plantAddNonDuplicateAreasToDb2";
            string deckName2 = "deckAddNonDuplicateAreasToDb2";
            string areaName2 = "areaAddNonDuplicateAreasToDb2";
            (_, _, _, _) = await PostAssetInformationToDb(installationCode2, plantCode2, deckName2, areaName2);
        }

        [Fact]
        public async Task AddDuplicateAreasToDb_Fails()
        {
            // Arrange - Area
            string installationCode = "installationAddDuplicateAreasToDb_Fails";
            string plantCode = "plantAddDuplicateAreasToDb_Fails";
            string deckName = "deckAddDuplicateAreasToDb_Fails";
            string areaName = "areaAddDuplicateAreasToDb_Fails";
            (_, _, _, _) = await PostAssetInformationToDb(installationCode, plantCode, deckName, areaName);

            string installationCode2 = "installationAddDuplicateAreasToDb_Fails2";
            string plantCode2 = "plantAddDuplicateAreasToDb_Fails2";
            string deckName2 = "deckAddDuplicateAreasToDb_Fails";
            string areaName2 = "areaAddDuplicateAreasToDb_Fails";
            await Assert.ThrowsAsync<FalseException>(async () => await PostAssetInformationToDb(installationCode2, plantCode2, deckName2, areaName2));
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
        public async Task ScheduleDuplicateCustomMissionDefinitions()
        {
            // Arrange - Initialise area
            string installationCode = "installationScheduleDuplicateCustomMissionDefinitions";
            string plantCode = "plantScheduleDuplicateCustomMissionDefinitions";
            string deckName = "deckScheduleDuplicateCustomMissionDefinitions";
            string areaName = "areaScheduleDuplicateCustomMissionDefinitions";
            (var installation, _, _, _) = await PostAssetInformationToDb(installationCode, plantCode, deckName, areaName);

            string testMissionName = "testMissionScheduleDuplicateCustomMissionDefinitions";

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
                CurrentInstallationCode = installationCode,
                CurrentAreaName = null,
                RobotCapabilities = [],
                VideoStreams = new List<CreateVideoStreamQuery>()
            };

            string robotUrl = "/robots";
            var robot = await PostToDb<Robot, CreateRobotQuery>(robotUrl, robotQuery);
            string robotId = robot.Id;

            // Arrange - Create custom mission definition
            var query = new CustomMissionQuery
            {
                RobotId = robotId,
                InstallationCode = installationCode,
                AreaName = areaName,
                DesiredStartTime = DateTime.SpecifyKind(new DateTime(3050, 1, 1), DateTimeKind.Utc),
                InspectionFrequency = new TimeSpan(14, 0, 0, 0),
                Name = testMissionName,
                Tasks = [
                    new()
                    {
                        RobotPose = new Pose(new Position(23, 14, 4), new Orientation()),
                        Inspections = [],
                        TaskOrder = 0
                    },
                    new()
                    {
                        RobotPose = new Pose(1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f),
                        Inspections = [],
                        TaskOrder = 1
                    }
                ]
            };
            var content = new StringContent(
                JsonSerializer.Serialize(query),
                null,
                "application/json"
            );

            // Act
            string customMissionsUrl = "/missions/custom";
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
            // Increasing pageSize to 50 to ensure the missions we are looking for is included
            string missionDefinitionsUrl = "/missions/definitions?pageSize=50";
            var missionDefinitionsResponse = await _client.GetAsync(missionDefinitionsUrl);
            var missionDefinitions = await missionDefinitionsResponse.Content.ReadFromJsonAsync<List<MissionDefinition>>(_serializerOptions);
            Assert.NotNull(missionDefinitions);
            Assert.Single(missionDefinitions.Where(m => m.Id == missionId1));
        }

        [Fact]
        public async Task GetNextRun()
        {
            // Arrange - Initialise area
            string installationCode = "installationGetNextRun";
            string plantCode = "plantGetNextRun";
            string deckName = "deckGetNextRun";
            string areaName = "areaGetNextRun";
            (var installation, _, _, _) = await PostAssetInformationToDb(installationCode, plantCode, deckName, areaName);

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
                CurrentAreaName = areaName,
                RobotCapabilities = [],
                VideoStreams = new List<CreateVideoStreamQuery>()
            };

            string robotUrl = "/robots";
            var robot = await PostToDb<Robot, CreateRobotQuery>(robotUrl, robotQuery);
            string robotId = robot.Id;

            // Arrange - Schedule custom mission - create mission definition
            string testMissionName = "testMissionNextRun";
            var query = new CustomMissionQuery
            {
                RobotId = robotId,
                InstallationCode = installationCode,
                AreaName = areaName,
                DesiredStartTime = DateTime.SpecifyKind(new DateTime(3050, 1, 1), DateTimeKind.Utc),
                InspectionFrequency = new TimeSpan(14, 0, 0, 0),
                Name = testMissionName,
                Tasks = [
                    new()
                    {
                        RobotPose = new Pose(),
                        Inspections = [],
                        TaskOrder = 0
                    }
                ]
            };
            var content = new StringContent(
                JsonSerializer.Serialize(query),
                null,
                "application/json"
            );

            string customMissionsUrl = "/missions/custom";
            var response = await _client.PostAsync(customMissionsUrl, content);
            Assert.True(response.IsSuccessStatusCode);
            var missionRun = await response.Content.ReadFromJsonAsync<MissionRun>(_serializerOptions);
            Assert.NotNull(missionRun);
            Assert.NotNull(missionRun.MissionId);
            Assert.NotNull(missionRun.Id);
            Assert.Equal(MissionStatus.Pending, missionRun.Status);

            // Arrange - Schedule missions from mission definition
            var scheduleQuery1 = new ScheduleMissionQuery
            {
                RobotId = robotId,
                DesiredStartTime = DateTime.SpecifyKind(new DateTime(2050, 1, 1), DateTimeKind.Utc),
            };
            var scheduleContent1 = new StringContent(
                JsonSerializer.Serialize(scheduleQuery1),
                null,
                "application/json"
            );
            var scheduleQuery2 = new ScheduleMissionQuery
            {
                RobotId = robotId,
                DesiredStartTime = DateTime.UtcNow,
            };
            var scheduleContent2 = new StringContent(
                JsonSerializer.Serialize(scheduleQuery2),
                null,
                "application/json"
            );
            var scheduleQuery3 = new ScheduleMissionQuery
            {
                RobotId = robotId,
                DesiredStartTime = DateTime.SpecifyKind(new DateTime(2100, 1, 1), DateTimeKind.Utc),
            };
            var scheduleContent3 = new StringContent(
                JsonSerializer.Serialize(scheduleQuery3),
                null,
                "application/json"
            );
            string scheduleMissionsUrl = $"/missions/schedule/{missionRun.MissionId}";
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
        public async Task ScheduleDuplicatMissionDefinitions()
        {
            // Arrange - Initialise areas
            string installationCode = "installationScheduleDuplicatMissionDefinitions";
            string plantCode = "plantScheduleDuplicatMissionDefinitions";
            string deckName = "deckScheduleDuplicatMissionDefinitions";
            string areaName = "areaScheduleDuplicatMissionDefinitions";
            (_, _, _, _) = await PostAssetInformationToDb(installationCode, plantCode, deckName, areaName);

            // Arrange - Create mission definition
            string robotUrl = "/robots";
            var response = await _client.GetAsync(robotUrl);
            Assert.True(response.IsSuccessStatusCode);
            var robots = await response.Content.ReadFromJsonAsync<List<Robot>>(_serializerOptions);
            Assert.NotNull(robots);
            var robot = robots.Where(robot => robot.Name == "Shockwave").First();
            string robotId = robot.Id;
            string missionId = "986"; // Corresponds to mock in ServiceMock.cs

            var query = new ScheduledMissionQuery
            {
                RobotId = robotId,
                InstallationCode = installationCode,
                AreaName = areaName,
                MissionId = missionId,
                DesiredStartTime = DateTime.UtcNow
            };
            var content = new StringContent(
                JsonSerializer.Serialize(query),
                null,
                "application/json"
            );

            // Act
            string missionsUrl = "/missions";
            var response1 = await _client.PostAsync(missionsUrl, content);
            var response2 = await _client.PostAsync(missionsUrl, content);

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
            string missionDefinitionsUrl = "/missions/definitions?pageSize=50";
            var missionDefinitionsResponse = await _client.GetAsync(missionDefinitionsUrl);
            var missionDefinitions = await missionDefinitionsResponse.Content.ReadFromJsonAsync<List<MissionDefinition>>(_serializerOptions);
            Assert.NotNull(missionDefinitions);
            Assert.NotNull(missionDefinitions.Find(m => m.Id == missionId1));
        }

        [Fact]
        public async Task MissionDoesNotStartIfRobotIsNotInSameInstallationAsMission()
        {
            // Arrange - Initialise area
            string installationCode = "installationMissionDoesNotStartIfRobotIsNotInSameInstallationAsMission";
            string plantCode = "plantMissionDoesNotStartIfRobotIsNotInSameInstallationAsMission";
            string deckName = "deckMissionDoesNotStartIfRobotIsNotInSameInstallationAsMission";
            string areaName = "areaMissionDoesNotStartIfRobotIsNotInSameInstallationAsMission";
            (var installation, _, _, _) = await PostAssetInformationToDb(installationCode, plantCode, deckName, areaName);

            string testMissionName = "testMissionDoesNotStartIfRobotIsNotInSameInstallationAsMission";

            // Arrange - Get different installation
            string otherInstallationCode = "installationMissionDoesNotStartIfRobotIsNotInSameInstallationAsMission_Other";
            var otherInstallation = await PostInstallationInformationToDb(otherInstallationCode);

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
                CurrentInstallationCode = otherInstallation.InstallationCode,
                CurrentAreaName = null,
                RobotCapabilities = [],
                VideoStreams = new List<CreateVideoStreamQuery>()
            };

            string robotUrl = "/robots";
            var robot = await PostToDb<Robot, CreateRobotQuery>(robotUrl, robotQuery);
            string robotId = robot.Id;

            // Arrange - Create custom mission definition
            var query = new CustomMissionQuery
            {
                RobotId = robotId,
                InstallationCode = installation.InstallationCode,
                AreaName = areaName,
                DesiredStartTime = DateTime.SpecifyKind(new DateTime(3050, 1, 1), DateTimeKind.Utc),
                InspectionFrequency = new TimeSpan(14, 0, 0, 0),
                Name = testMissionName,
                Tasks = [
                    new()
                    {
                        RobotPose = new Pose(),
                        Inspections = [],
                        TaskOrder = 0
                    },
                    new()
                    {
                        RobotPose = new Pose(1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f),
                        Inspections = [],
                        TaskOrder = 1
                    }
                ]
            };
            var content = new StringContent(
                JsonSerializer.Serialize(query),
                null,
                "application/json"
            );

            // Act
            string customMissionsUrl = "/missions/custom";
            var response = await _client.PostAsync(customMissionsUrl, content);
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task MissionFailsIfRobotIsNotInSameDeckAsMission()
        {
            // Arrange - Initialise area
            string installationCode = "installationMissionFailsIfRobotIsNotInSameDeckAsMission";
            string plantCode = "plantMissionFailsIfRobotIsNotInSameDeckAsMission";
            string deckName1 = "deckMissionFailsIfRobotIsNotInSameDeckAsMission1";
            string areaName1 = "areaMissionFailsIfRobotIsNotInSameDeckAsMission1";
            string deckName2 = "deckMissionFailsIfRobotIsNotInSameDeckAsMission2";
            string areaName2 = "areaMissionFailsIfRobotIsNotInSameDeckAsMission2";
            (var installation, _, _, var area1) = await PostAssetInformationToDb(installationCode, plantCode, deckName1, areaName1);
            (_, _, _, var area2) = await PostAssetInformationToDb(installationCode, plantCode, deckName2, areaName2);

            string testMissionName = "testMissionFailsIfRobotIsNotInSameDeckAsMission";

            // Arrange - Create robot
            var robotQuery = new CreateRobotQuery
            {
                IsarId = Guid.NewGuid().ToString(),
                Name = "RobotMissionFailsIfRobotIsNotInSameDeckAsMission",
                SerialNumber = "GetMissionFailsIfRobotIsNotInSameDeckAsMission",
                RobotType = RobotType.Robot,
                Status = RobotStatus.Available,
                Host = "localhost",
                Port = 3000,
                CurrentInstallationCode = installation.InstallationCode,
                CurrentAreaName = area1.AreaName,
                RobotCapabilities = [],
                VideoStreams = new List<CreateVideoStreamQuery>()
            };

            string robotUrl = "/robots";
            var robot = await PostToDb<Robot, CreateRobotQuery>(robotUrl, robotQuery);
            string robotId = robot.Id;

            // Arrange - Mission Run Query
            var query = new CustomMissionQuery
            {
                RobotId = robotId,
                InstallationCode = installation.InstallationCode,
                AreaName = area2.AreaName,
                DesiredStartTime = DateTime.SpecifyKind(new DateTime(3050, 1, 1), DateTimeKind.Utc),
                InspectionFrequency = new TimeSpan(14, 0, 0, 0),
                Name = testMissionName,
                Tasks = [
                    new()
                    {
                        RobotPose = new Pose(new Position(1, 9, 4), new Orientation()),
                        Inspections = [],
                        TaskOrder = 0
                    },
                    new()
                    {
                        RobotPose = new Pose(1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f),
                        Inspections = [],
                        TaskOrder = 1
                    }
                ]
            };
            var content = new StringContent(
                JsonSerializer.Serialize(query),
                null,
                "application/json"
            );

            // Act
            string customMissionsUrl = "/missions/custom";
            var missionResponse = await _client.PostAsync(customMissionsUrl, content);
            Assert.Equal(HttpStatusCode.Conflict, missionResponse.StatusCode);
        }
    }
}
