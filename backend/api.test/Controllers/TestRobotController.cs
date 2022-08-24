using System.Collections.Generic;
using Api.Controllers;
using Api.Database.Models;
using Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;
using Moq;
using Xunit;

namespace Api.Test.Controllers
{
    [Collection("Database collection")]
    public class TestRobotController
    {
        private readonly RobotController _controller;

        public TestRobotController(DatabaseFixture fixture)
        {
            // Using Moq https://github.com/moq/moq4
            var isarLogger = new Mock<ILogger<IsarService>>();
            var reportServiceLogger = new Mock<ILogger<ReportService>>();
            var echoDownstreamApi = new Mock<IDownstreamWebApi>();
            var isarDownstreamApi = new Mock<IDownstreamWebApi>();

            var context = fixture.NewContext;
            var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

            var reportService = new ReportService(context, reportServiceLogger.Object);
            var isarService = new IsarService(
                isarLogger.Object,
                reportService,
                isarDownstreamApi.Object
            );
            var echoService = new EchoService(config, echoDownstreamApi.Object);
            var service = new RobotService(context);

            var mockLoggerController = new Mock<ILogger<RobotController>>();
            _controller = new RobotController(
                mockLoggerController.Object,
                service,
                isarService,
                echoService
            );
        }

        [Fact]
        public void GetRobots()
        {
            var result = (OkObjectResult)_controller.GetRobots().Result.Result!;
            var robots = (IList<Robot>?)result.Value;

            Assert.NotNull(robots);
            if (robots is null)
                return;
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
