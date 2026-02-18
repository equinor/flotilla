using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Api.Database.Models;
using Api.Services;
using Api.Services.Models;
using Api.Utilities;

namespace Api.Test.Mocks
{
    public class MockIsarService : IIsarService
    {
        public bool isStartCalled = false;
        public bool isStarted = false;

        public async Task<IsarMission> StartMission(Robot robot, MissionRun mission)
        {
            isStartCalled = true;
            var startMissionStatuses = new List<RobotStatus>
            {
                RobotStatus.Available,
                RobotStatus.Home,
                RobotStatus.ReturnHomePaused,
                RobotStatus.ReturningHome,
            };
            if (!startMissionStatuses.Contains(robot.Status))
            {
                if (robot.Status == RobotStatus.Busy)
                    throw new RobotBusyException("Robot was not available when starting mission");
                else
                    throw new MissionException(
                        $"Robot had the wrong status when starting mission. Robot status: ${robot.Status}"
                    );
            }

            await Task.Run(() => Thread.Sleep(1));
            isStarted = true;

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

        public async Task SetMaintenanceMode(string robotIsarUri)
        {
            await Task.Run(() => Thread.Sleep(1));
        }

        public async Task ReleaseMaintenanceMode(string robotIsarUri)
        {
            await Task.Run(() => Thread.Sleep(1));
        }
    }
}
