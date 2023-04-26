﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Api.Controllers.Models;
using Api.Database.Context;
using Api.Database.Models;
using Api.Services;
using Xunit;

namespace Api.Test.Services
{
    [Collection("Database collection")]
    public class RobotServiceTest : IDisposable
    {
        private readonly FlotillaDbContext _context;

        public RobotServiceTest(DatabaseFixture fixture)
        {
            _context = fixture.NewContext;
        }

        public void Dispose()
        {
            _context.Dispose();
            GC.SuppressFinalize(this);
        }

        [Fact]
        public async Task ReadAll()
        {
            var robotService = new RobotService(_context);
            var robots = await robotService.ReadAll();

            Assert.True(robots.Any());
        }

        [Fact]
        public async Task Read()
        {
            var robotService = new RobotService(_context);
            var robots = await robotService.ReadAll();
            var firstRobot = robots.First();
            var robotById = await robotService.ReadById(firstRobot.Id);

            Assert.Equal(firstRobot, robotById);
        }

        [Fact]
        public async Task ReadIdDoesNotExist()
        {
            var robotService = new RobotService(_context);
            var robot = await robotService.ReadById("some_id_that_does_not_exist");
            Assert.Null(robot);
        }

        [Fact]
        public async Task Create()
        {
            var robotService = new RobotService(_context);
            int nRobotsBefore = robotService.ReadAll().Result.Count();
            var videoStreamQuery = new CreateVideoStreamQuery()
            {
                Name = "Front Camera",
                Url = "localhost:5000",
                Type = "mjpeg"
            };
            var robotQuery = new CreateRobotQuery()
            {
                Name = "",
                IsarId = "",
                SerialNumber = "",
                VideoStreams = new List<CreateVideoStreamQuery>() { videoStreamQuery },
                RobotType = RobotType.Robot,
                Host = "",
                Port = 1,
                Enabled = true,
                Status = RobotStatus.Available
            };

            var robot = new Robot(robotQuery);
            var robotModel = _context.RobotModels.First();
            robot.Model = robotModel;

            await robotService.Create(robot);
            int nRobotsAfter = robotService.ReadAll().Result.Count();

            Assert.Equal(nRobotsBefore + 1, nRobotsAfter);
        }
    }
}
