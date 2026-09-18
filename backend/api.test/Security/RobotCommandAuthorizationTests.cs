using System.Threading;
using System.Threading.Tasks;
using Api.Controllers;
using Api.Controllers.Models;
using Api.Database.Models;
using Api.Services;
using Api.Services.Events;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Api.Test.Security
{
    public class RobotCommandAuthorizationTests
    {
        [Theory]
        [InlineData("stop")]
        [InlineData("pause")]
        [InlineData("resume")]
        [InlineData("release-intervention")]
        [InlineData("maintenance")]
        [InlineData("release-maintenance")]
        [InlineData("return-home")]
        [InlineData("return-home-timeout")]
        public async Task ReadableButNotWritableRobotCannotReceiveCommands(string command)
        {
            var robot = new Robot { Id = "robot" };
            var robots = new Mock<IRobotService>(MockBehavior.Strict);
            robots.Setup(s => s.ReadById(robot.Id, true)).ReturnsAsync(robot);
            robots.Setup(s => s.ReadByIdForWrite(robot.Id, true)).ReturnsAsync((Robot?)null);
            var isar = new Mock<IIsarService>(MockBehavior.Strict);
            var robotController = new RobotController(
                NullLogger<RobotController>.Instance,
                robots.Object,
                isar.Object,
                Mock.Of<IInspectionAreaService>(),
                Mock.Of<IErrorHandlingService>(),
                new EventAggregatorSingletonService()
            );
            var maintenance = new MaintenanceModeController(
                NullLogger<RobotController>.Instance,
                robots.Object,
                isar.Object
            );
            var returnHome = new ReturnToHomeController(
                NullLogger<RobotController>.Instance,
                robots.Object,
                isar.Object
            );

            ActionResult result = command switch
            {
                "stop" => await robotController.StopMission(robot.Id),
                "pause" => await robotController.PauseMission(robot.Id),
                "resume" => await robotController.ResumeMission(robot.Id),
                "release-intervention" => await robotController.ReleaseInterventionNeeded(robot.Id),
                "maintenance" => await maintenance.SetMaintenanceMode(robot.Id),
                "release-maintenance" => await maintenance.ReleaseMaintenanceMode(robot.Id),
                "return-home" => await returnHome.ScheduleReturnToHomeMission(robot.Id),
                _ => await returnHome.SetReturnHomeTimeout(
                    robot.Id,
                    new SetReturnHomeTimeoutQuery { Seconds = 60 },
                    CancellationToken.None
                ),
            };

            Assert.True(result is NotFoundResult or NotFoundObjectResult);
            robots.Verify(s => s.ReadByIdForWrite(robot.Id, true), Times.Once);
            robots.Verify(s => s.ReadById(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
            isar.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task EmergencyCommandsRequireInstallationWriteAccess(bool clearEmergency)
        {
            var roles = new Mock<IAccessRoleService>(MockBehavior.Strict);
            roles.Setup(s => s.GetAllowedInstallationCodes(AccessMode.Write)).ReturnsAsync(["AAA"]);
            var robots = new Mock<IRobotService>(MockBehavior.Strict);
            var events = new EventAggregatorSingletonService();
            int published = 0;
            events.Subscribe<RobotEmergencyEventArgs>(_ => published++);
            var controller = new EmergencyActionController(robots.Object, events, roles.Object);

            var response = clearEmergency
                ? await controller.ClearEmergencyStateForAllRobots("BBB")
                : await controller.AbortCurrentMissionAndSendAllRobotsToDock("BBB");

            Assert.IsType<ForbidResult>(response.Result);
            Assert.Equal(0, published);
            robots.VerifyNoOtherCalls();
        }
    }
}
