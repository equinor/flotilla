using System.Collections.Generic;
using Api.Context;
using Api.Controllers;
using Api.Models;
using Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Api.Test
{
    [Collection("Database collection")]
    public class TestRobotController
    {
        private readonly RobotController _controller;

        public TestRobotController()
        {
            var options = new DbContextOptionsBuilder().UseInMemoryDatabase("flotilla").Options;
            var context = new FlotillaDbContext(options);
            var service = new RobotService(context);
            var logger = new LoggerFactory().CreateLogger<RobotController>();
            _controller = new RobotController(logger, service);
        }

        [Fact]
        public void GetRobots()
        {
            var result = (OkObjectResult)_controller.GetRobots().Result.Result!;
            var robots = (IList<Robot>?)result.Value;

            Assert.NotNull(robots);
            if (robots is null)
                return;

            Assert.Equal(InitDb.Robots.Count, robots.Count);
        }

        [Fact]
        public void GetRobotById_ShouldReturnNotFound()
        {
            var actionResultType = typeof(NotFoundResult);
            string robotId = "RandomString";

            IActionResult? result = _controller.GetRobotById(robotId).Result.Result;

            // Check if the result is, or inherits from, the expected result type
            Assert.IsAssignableFrom(actionResultType, result);
        }

        [Fact]
        public void GetRobotById_ShouldReturnOkObject()
        {
            var actionResultType = typeof(OkObjectResult);
            var result = (OkObjectResult)_controller.GetRobots().Result.Result!;
            var robots = (IList<Robot>?)result.Value;
            Assert.NotNull(robots);

            if (robots is null)
                return;

            string robotId = robots[0].Id;

            IActionResult? actionResult = _controller.GetRobotById(robotId).Result.Result;

            // Check if the result is, or inherits from, the expected result type
            Assert.IsAssignableFrom(actionResultType, actionResult);
        }
    }
}
