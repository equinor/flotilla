using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Api.Controllers.Models;
using Api.Database.Models;
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

            var mockEchoMission = new EchoMission
            {
                Id = missionId,
                Name = "test",
                InstallationCode = "testInstallation",
                URL = new Uri("https://testurl.com"),
                Tags = new List<EchoTag>{new() {
                    Id = 1,
                    TagId = "testTag",
                    Pose = new Pose(),
                    Inspections = new List<EchoInspection>{new() {
                        InspectionType = InspectionType.Image,
                        InspectionPoint = new Position{X=1, Y=1, Z=1}
                    }}
                }}
            };

            return mockEchoMission;
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
