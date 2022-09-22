using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Api.Controllers.Models;
using Api.Database.Models;
using Api.Services;
using Api.Services.Models;

namespace Api.Test.Mocks
{
    public class MockIsarService : IIsarService
    {
        public async Task<IsarServiceStartMissionResponse> StartMission(
            Robot robot,
            int echoMissionId,
            IsarMissionDefinition missionDefinition
        )
        {
            await Task.Run(() => Thread.Sleep(1));
            var isarServiceStartMissionResponse = new IsarServiceStartMissionResponse(
                isarMissionId: "test",
                startTime: DateTimeOffset.UtcNow,
                tasks: new List<IsarTask>()
            );
            return isarServiceStartMissionResponse;
        }

        public async Task<IsarStopMissionResponse> StopMission(Robot robot)
        {
            await Task.Run(() => Thread.Sleep(1));
            return new IsarStopMissionResponse();
        }

        public async Task<HttpResponseMessage> PauseMission(Robot robot)
        {
            await Task.Run(() => Thread.Sleep(1));
            return new HttpResponseMessage();
        }

        public async Task<HttpResponseMessage> ResumeMission(Robot robot)
        {
            await Task.Run(() => Thread.Sleep(1));
            return new HttpResponseMessage();
        }

        public async Task<IsarMissionDefinition> GetIsarMissionDefinition(EchoMission echoMission)
        {
            await Task.Run(() => Thread.Sleep(1));
            return new IsarMissionDefinition(new List<IsarTaskDefinition>());
        }
    }
}
