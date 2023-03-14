using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Api.Database.Models;
using Api.Services;
using Api.Services.Models;

namespace Api.Test.Mocks
{
    public class MockIsarService : IIsarService
    {
        public async Task<IsarMission> StartMission(Robot robot, Mission mission)
        {
            await Task.Run(() => Thread.Sleep(1));
            var isarServiceMissionResponse = new IsarMission(
                new IsarStartMissionResponse
                {
                    MissionId = "test",
                    Tasks = new List<IsarTaskResponse>()
                }
            );
            return isarServiceMissionResponse;
        }

        public async Task<IsarControlMissionResponse> StopMission(Robot robot)
        {
            await Task.Run(() => Thread.Sleep(1));
            return new IsarControlMissionResponse();
        }

        public async Task<IsarControlMissionResponse> PauseMission(Robot robot)
        {
            await Task.Run(() => Thread.Sleep(1));
            return new IsarControlMissionResponse();
        }

        public async Task<IsarControlMissionResponse> ResumeMission(Robot robot)
        {
            await Task.Run(() => Thread.Sleep(1));
            return new IsarControlMissionResponse();
        }
    }
}
