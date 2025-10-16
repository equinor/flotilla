using System.Threading;
using System.Threading.Tasks;
using Api.Database.Models;
using Api.Services;
using Api.Services.Models;

namespace Api.Test.Mocks
{
    public class MockIsarService : IIsarService
    {
        public async Task<IsarMission> StartMission(Robot robot, MissionRun mission)
        {
            await Task.Run(() => Thread.Sleep(1));
            var isarServiceMissionResponse = new IsarMission(
                new IsarStartMissionResponse
                {
                    MissionId = System.Guid.NewGuid().ToString(),
                    Tasks = [],
                }
            );
            return isarServiceMissionResponse;
        }

        public async Task ReturnHome(Robot robot)
        {
            await Task.Run(() => Thread.Sleep(1));
        }

        public async Task StopMission(Robot robot, string? missionId = null)
        {
            await Task.Run(() => Thread.Sleep(1));
        }

        public async Task PauseMission(Robot robot)
        {
            await Task.Run(() => Thread.Sleep(1));
        }

        public async Task ResumeMission(Robot robot)
        {
            await Task.Run(() => Thread.Sleep(1));
        }

        public async Task<IsarMission> StartMoveArm(Robot robot, string position)
        {
            await Task.Run(() => Thread.Sleep(1));
            var isarServiceMissionResponse = new IsarMission(
                new IsarStartMissionResponse { MissionId = "testStartMoveArm", Tasks = [] }
            );
            return isarServiceMissionResponse;
        }

        public async Task<MediaConfig?> GetMediaStreamConfig(Robot robot)
        {
            await Task.Run(() => Thread.Sleep(1));
            return new MediaConfig
            {
                Url = "mockURL",
                Token = "mockToken",
                RobotId = robot.Id,
                MediaConnectionType = MediaConnectionType.LiveKit,
            };
        }

        public async Task ReleaseInterventionNeeded(string robotIsarUri)
        {
            await Task.Run(() => Thread.Sleep(1));
        }

        public async Task SendToLockdown(string robotIsarUri)
        {
            await Task.Run(() => Thread.Sleep(1));
        }

        public async Task ReleaseFromLockdown(string robotIsarUri)
        {
            await Task.Run(() => Thread.Sleep(1));
        }
    }
}
