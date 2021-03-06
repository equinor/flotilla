using System.Collections.Generic;
using Api.Controllers;
using Api.Database.Models;
using Api.Services;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Api.Test.Controllers
{
    [Collection("Database collection")]
    public class TestScheduledMissionController
    {
        private readonly ScheduledMissionController _controller;

        public TestScheduledMissionController(DatabaseFixture fixture)
        {
            var context = fixture.NewContext;
            var scheduledMissionService = new ScheduledMissionService(context);
            var robotService = new RobotService(context);
            _controller = new ScheduledMissionController(
                scheduledMissionService: scheduledMissionService,
                robotService: robotService
            );
        }

        [Fact]
        public void GetScheduledMissions()
        {
            var result = (OkObjectResult)_controller.GetScheduledMissions().Result.Result!;
            var scheduledMissions = (IList<ScheduledMission>?)result.Value;

            Assert.NotNull(scheduledMissions);
            if (scheduledMissions is null)
                return;
        }

        [Fact]
        public void GetScheduledMissionById_ShouldReturnNotFound()
        {
            var actionResultType = typeof(NotFoundObjectResult);
            string scheduledMissionId = "RandomString";

            IActionResult? result = _controller
                .GetScheduledMissionById(scheduledMissionId)
                .Result.Result;

            // Check if the result is, or inherits from, the expected result type
            Assert.IsAssignableFrom(actionResultType, result);
        }

        [Fact]
        public void GetScheduledMissionById_ShouldReturnOkObject()
        {
            var actionResultType = typeof(OkObjectResult);
            var result = (OkObjectResult)_controller.GetScheduledMissions().Result.Result!;
            var scheduledMissions = (IList<ScheduledMission>?)result.Value;
            Assert.NotNull(scheduledMissions);

            if (scheduledMissions is null)
                return;

            string scheduledMissionId = scheduledMissions[0].Id;

            IActionResult? actionResult = _controller
                .GetScheduledMissionById(scheduledMissionId)
                .Result.Result;

            // Check if the result is, or inherits from, the expected result type
            Assert.IsAssignableFrom(actionResultType, actionResult);
        }

        [Fact]
        public void DeleteScheduledMission_ShouldReturnNotFound()
        {
            var actionResultType = typeof(NotFoundObjectResult);
            string scheduledMissionId = "RandomString";

            IActionResult? result = _controller
                .DeleteScheduledMission(scheduledMissionId)
                .Result.Result;

            // Check if the result is, or inherits from, the expected result type
            Assert.IsAssignableFrom(actionResultType, result);
        }

        [Fact]
        public void DeleteScheduledMission_ShouldReturnOkObject()
        {
            var actionResultType = typeof(OkObjectResult);
            var result = (OkObjectResult)_controller.GetScheduledMissions().Result.Result!;
            var scheduledMissions = (IList<ScheduledMission>?)result.Value;
            Assert.NotNull(scheduledMissions);

            if (scheduledMissions is null)
                return;

            string scheduledMissionsId = scheduledMissions[0].Id;
            IActionResult? actionResult = _controller
                .DeleteScheduledMission(scheduledMissionsId)
                .Result.Result;

            // Check if the result is, or inherits from, the expected result type
            Assert.IsAssignableFrom(actionResultType, actionResult);
        }
    }
}
