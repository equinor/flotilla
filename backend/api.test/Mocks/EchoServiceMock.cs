using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Api.Controllers.Models;
using Api.Services;
namespace Api.Test.Mocks
{
    public class MockEchoService : IEchoService
    {
        private readonly List<EchoPlantInfo> _mockEchoPlantInfo = [
            new EchoPlantInfo
            {
                PlantCode = "testInstallation",
                ProjectDescription = "testInstallation"
            },
            new EchoPlantInfo
            {
                PlantCode = "JSV",
                ProjectDescription = "JSVtestInstallation"
            }
        ];

        public EchoMission MockEchoMission =
            new()
            {
                Id = 1,
                Name = "test",
                InstallationCode = "testInstallation",
                URL = new Uri("https://www.I-am-echo-stid-tag-url.com"),
                Tags = new List<EchoTag>()
            };

        public CondensedEchoMissionDefinition MockMissionDefinition =
            new()
            {
                EchoMissionId = 1,
                Name = "test"
            };

        public async Task<IList<CondensedEchoMissionDefinition>> GetAvailableMissions(string? installationCode)
        {
            await Task.Run(() => Thread.Sleep(1));
            return new List<CondensedEchoMissionDefinition>(new[] { MockMissionDefinition });
        }

        public async Task<EchoMission> GetMissionById(int missionId)
        {
            await Task.Run(() => Thread.Sleep(1));
            return MockEchoMission;
        }

        public async Task<IList<EchoPlantInfo>> GetEchoPlantInfos()
        {
            await Task.Run(() => Thread.Sleep(1));
            return _mockEchoPlantInfo;
        }

        public Task<EchoMission> GetMissionByPath(string relativePath)
        {
            throw new NotImplementedException();
        }
    }
}
