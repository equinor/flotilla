using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Api.Controllers.Models;
using Api.Database.Models;
using Api.Services;
using Api.Test.Database;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace Api.Test.Controllers
{
    public class InspectionGroupControllerTests : IAsyncLifetime
    {
        public required DatabaseUtilities DatabaseUtilities;
        public required PostgreSqlContainer Container;
        public required HttpClient Client;
        public required JsonSerializerOptions SerializerOptions;

        public required IInspectionGroupService InspectionGroupService;
        public required IMissionRunService MissionRunService;
        public required IMissionDefinitionService MissionDefinitionService;

        public async Task InitializeAsync()
        {
            (Container, string connectionString, var connection) =
                await TestSetupHelpers.ConfigurePostgreSqlDatabase();
            var factory = TestSetupHelpers.ConfigureWebApplicationFactory(
                postgreSqlConnectionString: connectionString
            );
            var serviceProvider = TestSetupHelpers.ConfigureServiceProvider(factory);

            Client = TestSetupHelpers.ConfigureHttpClient(factory);
            SerializerOptions = TestSetupHelpers.ConfigureJsonSerializerOptions();

            DatabaseUtilities = new DatabaseUtilities(
                TestSetupHelpers.ConfigurePostgreSqlContext(connectionString)
            );

            InspectionGroupService = serviceProvider.GetRequiredService<IInspectionGroupService>();
            MissionRunService = serviceProvider.GetRequiredService<IMissionRunService>();
            MissionDefinitionService =
                serviceProvider.GetRequiredService<IMissionDefinitionService>();
        }

        public Task DisposeAsync() => Task.CompletedTask;

        [Fact]
        public async Task CheckThatInspectionGroupIsCorrectlyCreatedThroughEndpoint()
        {
            // Arrange
            var installation = await DatabaseUtilities.NewInstallation();

            var query = new CreateInspectionGroupQuery()
            {
                InstallationCode = installation.InstallationCode,
                Name = "inspectionGroup",
            };

            var content = new StringContent(
                JsonSerializer.Serialize(query),
                null,
                "application/json"
            );

            // Act
            const string Url = "/inspectionGroups";
            var response = await Client.PostAsync(Url, content);

            // Assert
            var inspectionGroup = await InspectionGroupService.ReadByInstallationAndName(
                installation.InstallationCode,
                query.Name
            );

            Assert.True(response.IsSuccessStatusCode);
            Assert.Equal(query.Name, inspectionGroup!.Name);
        }

        [Fact]
        public async Task CheckThatMissionDefinitionIsCreatedInInspectionGroupWhenSchedulingACustomMissionRun()
        {
            // Arrange
            var installation = await DatabaseUtilities.NewInstallation();
            var inspectionGroup = await DatabaseUtilities.NewInspectionGroup(
                installation.InstallationCode
            );
            var robot = await DatabaseUtilities.NewRobot(RobotStatus.Available, installation);

            var inspection = new CustomInspectionQuery
            {
                AnalysisType = AnalysisType.CarSeal,
                InspectionTarget = new Position(),
                InspectionType = InspectionType.Image,
            };
            var tasks = new List<CustomTaskQuery>
            {
                new()
                {
                    Inspection = inspection,
                    TagId = "test",
                    RobotPose = new Pose(),
                    TaskOrder = 0,
                },
            };
            var missionQuery = new CustomMissionQuery
            {
                RobotId = robot.Id,
                DesiredStartTime = DateTime.UtcNow,
                InstallationCode = installation.InstallationCode,
                InspectionGroupName = inspectionGroup.Name,
                Name = "TestMission",
                Tasks = tasks,
            };

            var missionContent = new StringContent(
                JsonSerializer.Serialize(missionQuery),
                null,
                "application/json"
            );

            // Act
            var missionResponse = await Client.PostAsync("/missions/custom", missionContent);
            var userMissionResponse = await missionResponse.Content.ReadFromJsonAsync<MissionRun>(
                SerializerOptions
            );
            var inspectionGroupMissionResponse = await Client.GetAsync(
                $"/inspectionGroups/{inspectionGroup.Id}/mission-definitions"
            );

            // Assert
            var mission = await MissionRunService.ReadById(userMissionResponse!.Id);
            var missionDefinitions = await MissionDefinitionService.ReadByInspectionGroupId(
                inspectionGroup.Id
            );

            Assert.True(missionResponse.IsSuccessStatusCode);
            Assert.True(inspectionGroupMissionResponse.IsSuccessStatusCode);
            Assert.Single(
                missionDefinitions.Where(m =>
                    m.Id.Equals(mission!.MissionId, StringComparison.Ordinal)
                )
            );
        }

        // [Fact] TODO REMOVE
        // public async Task CheckThatDefaultLocalizationPoseIsUpdatedOnInspectionGroup()
        // {
        //     // Arrange
        //     var installation = await DatabaseUtilities.NewInstallation();
        //     var inspectionGroup = await DatabaseUtilities.NewInspectionGroup(
        //         installation.InstallationCode
        //     );

        //     string inspectionGroupId = inspectionGroup.Id;

        //     string url = $"/inspectionGroups/{inspectionGroupId}/update-default-localization-pose";
        //     var query = new CreateDefaultLocalizationPose
        //     {
        //         Pose = new Pose
        //         {
        //             Position = new Position
        //             {
        //                 X = 1,
        //                 Y = 2,
        //                 Z = 3,
        //             },
        //             Orientation = new Orientation
        //             {
        //                 X = 0,
        //                 Y = 0,
        //                 Z = 0,
        //                 W = 1,
        //             },
        //         },
        //     };
        //     var content = new StringContent(
        //         JsonSerializer.Serialize(query),
        //         null,
        //         "application/json"
        //     );

        //     // Act
        //     var response = await Client.PutAsync(url, content);
        //     var updatedInspectionGroup =
        //         await response.Content.ReadFromJsonAsync<InspectionGroupResponse>(
        //             SerializerOptions
        //         );

        //     // Assert
        //     Assert.Equal(
        //         updatedInspectionGroup!.DefaultLocalizationPose!.Position,
        //         query.Pose.Position
        //     );
        //     Assert.Equal(
        //         updatedInspectionGroup!.DefaultLocalizationPose.Orientation,
        //         query.Pose.Orientation
        //     );
        // }
    }
}
