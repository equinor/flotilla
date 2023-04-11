using Api.Controllers;
using Api.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace Api.Test.Mocks
{
    internal class RobotControllerMock
    {
        public Mock<IIsarService> IsarServiceMock;
        public Mock<IRobotService> RobotServiceMock;
        public Mock<IMissionService> MissionServiceMock;
        public Mock<RobotController> Mock;

        public RobotControllerMock()
        {
            MissionServiceMock = new Mock<IMissionService>();
            IsarServiceMock = new Mock<IIsarService>();
            RobotServiceMock = new Mock<IRobotService>();

            var mockLoggerController = new Mock<ILogger<RobotController>>();

            Mock = new Mock<RobotController>(
                mockLoggerController.Object,
                RobotServiceMock.Object,
                IsarServiceMock.Object,
                MissionServiceMock.Object
            )
            {
                CallBase = true
            };
        }
    }
}
