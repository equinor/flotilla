using System.Linq;
using System.Threading.Tasks;
using Api.Database.Models;
using Api.Services;
using Xunit;

namespace Api.Test
{
    [Collection("Database collection")]
    public class RobotServiceTest
    {
        private readonly DatabaseFixture _fixture;

        public RobotServiceTest(DatabaseFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task ReadAll()
        {
            var robotService = new RobotService(_fixture.Context);
            var robots = await robotService.ReadAll();

            Assert.True(robots.Any());
        }

        [Fact]
        public async Task Read()
        {
            var robotService = new RobotService(_fixture.Context);
            var robots = await robotService.ReadAll();
            var firstRobot = robots.First();
            var robotById = await robotService.ReadById(firstRobot.Id);

            Assert.Equal(firstRobot, robotById);
        }

        [Fact]
        public async Task ReadIdDoesNotExist()
        {
            var robotService = new RobotService(_fixture.Context);
            var robot = await robotService.ReadById("some_id_that_does_not_exist");
            Assert.Null(robot);
        }

        [Fact]
        public async Task Create()
        {
            var robotService = new RobotService(_fixture.Context);
            int nRobotsBefore = robotService.ReadAll().Result.Count();
            var robot = new Robot()
            {
                Name = "",
                Model = "",
                SerialNumber = "",
                Host = "",
                Port = 1,
                Enabled = true,
                Status = RobotStatus.Available
            };
            await robotService.Create(robot);
            int nRobotsAfter = robotService.ReadAll().Result.Count();

            Assert.Equal(nRobotsBefore + 1, nRobotsAfter);
        }
    }
}
