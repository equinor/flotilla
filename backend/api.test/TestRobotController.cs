using System.Collections.Generic;
using Api.Context;
using Api.Controllers;
using Api.Models;
using Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Api.Test
{
    [Collection("Database collection")]
    public class TestRobotController
    {
        private readonly RobotController _controller;

        public TestRobotController()
        {
            // Using Moq https://github.com/moq/moq4
            var isarLogger = new Mock<ILogger<IsarService>>();
            var reportServiceLogger = new Mock<ILogger<ReportService>>();

            var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

            var options = new DbContextOptionsBuilder().UseInMemoryDatabase("flotilla").Options;
            var context = new FlotillaDbContext(options);

            var reportService = new ReportService(context, reportServiceLogger.Object);
            var isarService = new IsarService(config, isarLogger.Object, reportService);
            var service = new RobotService(context);

            var mockLoggerController = new Mock<ILogger<RobotController>>();
            _controller = new RobotController(mockLoggerController.Object, service, isarService);
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
            var actionResultType = typeof(NotFoundObjectResult);
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
