using System.Threading.Tasks;
using Api.Database.Models;
using Api.Services;
using Api.Services.Helpers;
using Api.Test.Utilities;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Api.Test.Services.Helpers;

public class MissionSchedulingHelpersTests : IAsyncLifetime
{
    public async Task InitializeAsync() => await Task.CompletedTask;

    public Task DisposeAsync() => Task.CompletedTask;

    [Theory]
    [InlineData(false, MissionRunType.Normal, RobotStatus.Available, true, false, true)]
    [InlineData(false, MissionRunType.Normal, RobotStatus.Busy, true, false, false)]
    [InlineData(false, MissionRunType.Normal, RobotStatus.Offline, true, false, false)]
    [InlineData(false, MissionRunType.Normal, RobotStatus.Blocked, true, false, false)]
    [InlineData(
        false,
        MissionRunType.Normal,
        RobotStatus.BlockedProtectiveStop,
        true,
        false,
        false
    )]
    [InlineData(true, MissionRunType.Normal, RobotStatus.Available, true, false, false)]
    [InlineData(true, MissionRunType.Emergency, RobotStatus.Available, true, false, true)]
    [InlineData(true, MissionRunType.ReturnHome, RobotStatus.Available, true, false, true)]
    [InlineData(false, MissionRunType.Normal, RobotStatus.Available, false, false, false)]
    [InlineData(false, MissionRunType.Normal, RobotStatus.Available, true, true, false)]
    public void CheckLogicOfSystemIsAvailableToRunAMissionFunction(
        bool missionQueueFrozen,
        MissionRunType missionRunType,
        RobotStatus robotStatus,
        bool isIsarConnected,
        bool isRobotDeprecated,
        bool expectedResult
    )
    {
        // Arrange
        var installation = TestObjectGenerator.NewInstallation();
        var plant = TestObjectGenerator.NewPlant(installation);
        var inspectionArea = TestObjectGenerator.NewInspectionArea(installation, plant);

        var robot = TestObjectGenerator.NewRobot(
            currentInstallation: installation,
            currentInspectionAreaId: inspectionArea.Id,
            robotStatus: robotStatus,
            isarConnected: isIsarConnected,
            deprecated: isRobotDeprecated,
            missionQueueFrozen: missionQueueFrozen
        );

        var missionRun = TestObjectGenerator.NewMissionRun(
            installationCode: installation.InstallationCode,
            robot: robot,
            inspectionArea: inspectionArea,
            missionRunType: missionRunType
        );

        // Act
        var isSystemAvailable = MissionSchedulingHelpers.TheSystemIsAvailableToRunAMission(
            robot,
            missionRun,
            new Mock<ILogger<MissionSchedulingService>>().Object
        );

        // Assert
        Assert.Equal(expectedResult, isSystemAvailable);
    }
}
