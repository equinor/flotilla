using System.Collections.Generic;
using Api.Controllers;
using Api.Database.Models;
using Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Api.Test.Controllers
{
    [Collection("Database collection")]
    public class TestMissionController
    {
        private readonly MissionController _controller;

        public TestMissionController(DatabaseFixture fixture)
        {
            var missionLogger = new Mock<ILogger<MissionService>>().Object;
            var missionControllerLogger = new Mock<ILogger<MissionController>>().Object;
            var context = fixture.NewContext;
            var robotService = new RobotService(context);
            var echoService = new Mock<IEchoService>().Object;
            var mapService = new Mock<IMapService>().Object;
            var missionService = new MissionService(context, missionLogger);

            _controller = new MissionController(
                missionService,
                robotService,
                echoService,
                missionControllerLogger,
                mapService
            );
        }

        [Fact]
        public void GetMissions()
        {
            var result = (OkObjectResult)_controller.GetMissions(null, null).Result.Result!;
            var scheduledMissions = (IList<Mission>?)result.Value;

            Assert.NotNull(scheduledMissions);
            if (scheduledMissions is null)
                return;
        }

        [Fact]
        public void GetMissionById_ShouldReturnNotFound()
        {
            var actionResultType = typeof(NotFoundObjectResult);
            string missionId = "RandomString";

            IActionResult? result = _controller.GetMissionById(missionId).Result.Result;

            // Check if the result is, or inherits from, the expected result type
            Assert.IsAssignableFrom(actionResultType, result);
        }

        [Fact]
        public void DeleteMission_ShouldReturnNotFound()
        {
            var actionResultType = typeof(NotFoundObjectResult);
            string missionId = "RandomString";

            IActionResult? result = _controller.DeleteMission(missionId).Result.Result;

            // Check if the result is, or inherits from, the expected result type
            Assert.IsAssignableFrom(actionResultType, result);
        }
    }
}
