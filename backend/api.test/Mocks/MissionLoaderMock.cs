using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Api.Controllers.Models;
using Api.Database.Models;
using Api.Services.MissionLoaders;
namespace Api.Test.Mocks
{
    public class MockMissionLoader() : IMissionLoader
    {
        private readonly List<PlantInfo> _mockPlantInfo = [
            new PlantInfo
            {
                PlantCode = "testInstallation",
                ProjectDescription = "testInstallation"
            },
            new PlantInfo
            {
                PlantCode = "JSV",
                ProjectDescription = "JSVtestInstallation"
            }
        ];

        private readonly List<MissionTask> _mockMissionTasks = [
            new MissionTask(
                inspection: new Inspection(),
                taskOrder: 0,
                tagId: "1",
                tagLink: new Uri("https://testurl.com"),
                poseId: 1,
                taskDescription: "description",
                robotPose: new Pose
                {
                    Position = new Position { X = 0, Y = 0, Z = 0 },
                    Orientation = new Orientation { X = 0, Y = 0, Z = 0, W = 1 }
                }
            ),
            new MissionTask(
                inspection: new Inspection(),
                taskOrder: 0,
                tagId: "2",
                tagLink: new Uri("https://testurl.com"),
                poseId: 1,
                taskDescription: "description",
                robotPose: new Pose
                {
                    Position = new Position { X = 0, Y = 0, Z = 0 },
                    Orientation = new Orientation { X = 0, Y = 0, Z = 0, W = 1 }
                }
            ),
        ];

        private readonly MissionDefinition _mockMissionDefinition = new()
        {
            InspectionArea = new InspectionArea(),
            Comment = "",
            Id = "",
            InstallationCode = "TTT",
            IsDeprecated = false,
            Name = "test",
            Source = new Source { Id = "", SourceId = "" }
        };

        public async Task<MissionDefinition?> GetMissionById(string sourceMissionId)
        {
            await Task.Run(() => Thread.Sleep(1));
            return _mockMissionDefinition;
        }

        public async Task<IQueryable<MissionDefinition>> GetAvailableMissions(string? installationCode)
        {
            await Task.Run(() => Thread.Sleep(1));
            return new List<MissionDefinition>([_mockMissionDefinition]).AsQueryable();
        }

        public async Task<List<MissionTask>> GetTasksForMission(string sourceMissionId)
        {
            await Task.Run(() => Thread.Sleep(1));
            return _mockMissionTasks;
        }

        public async Task<List<PlantInfo>> GetPlantInfos()
        {
            await Task.Run(() => Thread.Sleep(1));
            return _mockPlantInfo;
        }
    }
}
