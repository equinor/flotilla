using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Api.Controllers.Models;
using Api.Services;
using Api.Services.Models;

namespace Api.Test.Mocks
{
    public class MockEchoService : IEchoService
    {
        public EchoMission MockEchoMission =
            new()
            {
                Id = 1,
                Name = "test",
                URL = new Uri("https://www.I-am-echo-stid-tag-url.com"),
                Tags = new List<EchoTag>()
            };

        public MissionDefinition MockMissionDefinition =
            new()
            {
                EchoMissionId = 1,
                Name = "test",
            };

        public async Task<IList<MissionDefinition>> GetAvailableMissions(string? installationCode)
        {
            await Task.Run(() => Thread.Sleep(1));
            return new List<MissionDefinition>(new MissionDefinition[] { MockMissionDefinition });
        }

        public async Task<EchoMission> GetMissionById(int missionId)
        {
            await Task.Run(() => Thread.Sleep(1));
            return MockEchoMission;
        }

        public async Task<IList<EchoPlantInfo>> GetEchoPlantInfos()
        {
            await Task.Run(() => Thread.Sleep(1));
            return new List<EchoPlantInfo>();
        }
        public async Task<EchoPoseResponse> GetRobotPoseFromPoseId(int poseId)
        {
            await Task.Run(() => Thread.Sleep(1));
            return new EchoPoseResponse();
        }
    }
}
