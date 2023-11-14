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
using Xunit;
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


        [Fact]
        public async Task MissionsTest()
        {
            // Arrange - Robots
            string robotUrl = "/robots";
            string missionsUrl = "/missions";
            var robotResponse = await _client.GetAsync(robotUrl);
            Assert.True(robotResponse.IsSuccessStatusCode);
            var robots = await robotResponse.Content.ReadFromJsonAsync<List<Robot>>(_serializerOptions);
            Assert.True(robots != null);
            var robot = robots[0];
            string robotId = robot.Id;

            // Arrange - Areas
            string areaUrl = "/areas";
            var areaResponse = await _client.GetAsync(areaUrl);
            Assert.True(areaResponse.IsSuccessStatusCode);
            var areaResponses = await areaResponse.Content.ReadFromJsonAsync<List<AreaResponse>>(_serializerOptions);
            Assert.True(areaResponses != null);
            var area = areaResponses[0];
            string areaName = area.AreaName;
            string installationCode = area.InstallationCode;

            int echoMissionId = 97;

            // Act
            var query = new ScheduledMissionQuery
            {
                RobotId = robotId,
                InstallationCode = installationCode,
                AreaName = areaName,
                EchoMissionId = echoMissionId,
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
            Assert.True(missionRuns != null);
            int missionRunsBefore = missionRuns.Count;

            await _client.PostAsync(missionsUrl, content);
            await _client.PostAsync(missionsUrl, content);
            await _client.PostAsync(missionsUrl, content);

            response = await _client.GetAsync(urlMissionRuns);
            missionRuns = await response.Content.ReadFromJsonAsync<List<MissionRun>>(
                _serializerOptions
            );

            // Assert
            Assert.True(response.IsSuccessStatusCode);
            Assert.True(missionRuns != null);
            Assert.True(missionRuns.Count == (missionRunsBefore + 3));
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
            // Arrange - Robots
            string robotUrl = "/robots";
            string missionsUrl = "/missions";
            var response = await _client.GetAsync(robotUrl);
            Assert.True(response.IsSuccessStatusCode);
            var robots = await response.Content.ReadFromJsonAsync<List<Robot>>(_serializerOptions);
            Assert.True(robots != null);
            var robot = robots[0];
            string robotId = robot.Id;

            // Arrange - Areas
            string areaUrl = "/areas";
            var areaResponse = await _client.GetAsync(areaUrl);
            Assert.True(areaResponse.IsSuccessStatusCode);
            var areaResponses = await areaResponse.Content.ReadFromJsonAsync<List<AreaResponse>>(_serializerOptions);
            Assert.True(areaResponses != null);
            var area = areaResponses[0];
            string areaName = area.AreaName;
            string installationCode = area.InstallationCode;

            int echoMissionId = 95;

            // Act
            var query = new ScheduledMissionQuery
            {
                RobotId = robotId,
                InstallationCode = installationCode,
                AreaName = areaName,
                EchoMissionId = echoMissionId,
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
            Assert.True(missionRun != null);
            Assert.True(missionRun.Id != null);
            Assert.True(missionRun.Status == MissionStatus.Pending);
        }

        [Fact]
        public async Task ScheduleDuplicateCustomMissionDefinitions()
        {
            // Arrange - Initialise areas
            string areaUrl = "/areas";
            var areaResponse = await _client.GetAsync(areaUrl);
            Assert.True(areaResponse.IsSuccessStatusCode);
            var areaResponses = await areaResponse.Content.ReadFromJsonAsync<List<AreaResponse>>(_serializerOptions);
            Assert.True(areaResponses != null);
            var area = areaResponses[0];
            string areaName = area.AreaName;
            string installationCode = area.InstallationCode;

            string testMissionName = "testMissionScheduleDuplicateCustomMissionDefinitions";

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
                InstallationCode = installationCode,
                AreaName = areaName,
                DesiredStartTime = DateTime.SpecifyKind(new DateTime(3050, 1, 1), DateTimeKind.Utc),
                InspectionFrequency = new TimeSpan(14, 0, 0, 0),
                Name = testMissionName,
                Tasks = new List<CustomTaskQuery>
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
                }
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
            Assert.NotNull(missionDefinitions.Find(m => m.Id == missionId1));
        }

        [Fact]
        public async Task GetNextRun()
        {
            // Arrange - Initialise areas
            string areaUrl = "/areas";
            var areaResponse = await _client.GetAsync(areaUrl);
            Assert.True(areaResponse.IsSuccessStatusCode);
            var areaResponses = await areaResponse.Content.ReadFromJsonAsync<List<AreaResponse>>(_serializerOptions);
            Assert.True(areaResponses != null);
            var area = areaResponses[0];
            string areaName = area.AreaName;
            string installationCode = area.InstallationCode;

            // Arrange - Create custom mission definition
            string robotUrl = "/robots";
            var response = await _client.GetAsync(robotUrl);
            Assert.True(response.IsSuccessStatusCode);
            var robots = await response.Content.ReadFromJsonAsync<List<Robot>>(_serializerOptions);
            Assert.True(robots != null);
            var robot = robots[0];
            string robotId = robot.Id;

            string testMissionName = "testMissionNextRun";
            var query = new CustomMissionQuery
            {
                RobotId = robotId,
                InstallationCode = installationCode,
                AreaName = areaName,
                DesiredStartTime = DateTime.SpecifyKind(new DateTime(3050, 1, 1), DateTimeKind.Utc),
                InspectionFrequency = new TimeSpan(14, 0, 0, 0),
                Name = testMissionName,
                Tasks = new List<CustomTaskQuery>
                {
                    new()
                    {
                        RobotPose = new Pose(),
                        Inspections = new List<CustomInspectionQuery>(),
                        InspectionTarget = new Position(),
                        TaskOrder = 0
                    }
                }
            };
            var content = new StringContent(
                JsonSerializer.Serialize(query),
                null,
                "application/json"
            );

            string customMissionsUrl = "/missions/custom";
            response = await _client.PostAsync(customMissionsUrl, content);
            Assert.True(response.IsSuccessStatusCode);
            var missionRun = await response.Content.ReadFromJsonAsync<MissionRun>(_serializerOptions);
            Assert.True(missionRun != null);
            Assert.True(missionRun.MissionId != null);
            Assert.True(missionRun.Id != null);
            Assert.True(missionRun.Status == MissionStatus.Pending);

            // Arrange - Schedule missions from mission definition
            var scheduleQuery1 = new ScheduleMissionQuery
            {
                RobotId = robotId,
                DesiredStartTime = DateTime.SpecifyKind(new DateTime(2050, 1, 1), DateTimeKind.Utc),
                MissionDefinitionId = missionRun.MissionId
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
                MissionDefinitionId = missionRun.MissionId
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
                MissionDefinitionId = missionRun.MissionId
            };
            var scheduleContent3 = new StringContent(
                JsonSerializer.Serialize(scheduleQuery3),
                null,
                "application/json"
            );
            string scheduleMissionsUrl = "/missions/schedule";
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
        public async Task ScheduleDuplicateEchoMissionDefinitions()
        {
            // Arrange - Initialise areas
            string areaUrl = "/areas";
            var areaResponse = await _client.GetAsync(areaUrl);
            Assert.True(areaResponse.IsSuccessStatusCode);
            var areaResponses = await areaResponse.Content.ReadFromJsonAsync<List<AreaResponse>>(_serializerOptions);
            Assert.True(areaResponses != null);
            var area = areaResponses[0];
            string areaName = area.AreaName;
            string installationCode = area.InstallationCode;


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
                InstallationCode = installationCode,
                AreaName = areaName,
                EchoMissionId = echoMissionId,
                DesiredStartTime = DateTime.UtcNow
            };
            var content = new StringContent(
                JsonSerializer.Serialize(query),
                null,
                "application/json"
            );

            // Act
            string echoMissionsUrl = "/missions";
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

            string missionDefinitionsUrl = "/missions/definitions?pageSize=50";
            var missionDefinitionsResponse = await _client.GetAsync(missionDefinitionsUrl);
            var missionDefinitions = await missionDefinitionsResponse.Content.ReadFromJsonAsync<List<MissionDefinition>>(_serializerOptions);
            Assert.NotNull(missionDefinitions);
            Assert.NotNull(missionDefinitions.Find(m => m.Id == missionId1));
        }
    }
}
