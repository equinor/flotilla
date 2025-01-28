﻿using Api.Database.Models;
using Microsoft.EntityFrameworkCore;
using TaskStatus = Api.Database.Models.TaskStatus;

namespace Api.Database.Context
{
    public static class InitDb
    {
        private static readonly List<Inspection> inspections = GetInspections();
        private static readonly List<Installation> installations = GetInstallations();
        private static readonly List<Robot> robots = GetRobots();
        private static readonly List<Plant> plants = GetPlants();
        private static readonly List<InspectionGroup> inspectionGroups = GetInspectionGroups();
        private static readonly List<Source> sources = GetSources();
        private static readonly List<MissionTask> tasks = GetMissionTasks();
        private static readonly List<MissionDefinition> missionDefinitions =
            GetMissionDefinitions();
        private static readonly List<MissionRun> missionRuns = GetMissionRuns();
        private static readonly List<AccessRole> accessRoles = GetAccessRoles();

        private static List<Inspection> GetInspections()
        {
            var inspection1 = new Inspection { InspectionType = InspectionType.Image };

            var inspection2 = new Inspection { InspectionType = InspectionType.ThermalImage };

            return [inspection1, inspection2];
        }

        private static List<AccessRole> GetAccessRoles()
        {
            var accessRole1 = new AccessRole
            {
                Installation = installations[0],
                AccessLevel = RoleAccessLevel.ADMIN,
                RoleName = "Role.User.HUA",
            };

            return [accessRole1];
        }

        private static List<Installation> GetInstallations()
        {
            var installation1 = new Installation
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Huldra",
                InstallationCode = "HUA",
            };

            var installation2 = new Installation
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Kårstø",
                InstallationCode = "KAA",
            };

            return [installation1, installation2];
        }

        private static List<Plant> GetPlants()
        {
            var plant1 = new Plant
            {
                Id = Guid.NewGuid().ToString(),
                Installation = installations[0],
                Name = "HULDRA",
                PlantCode = "HUA",
            };

            var plant2 = new Plant
            {
                Id = Guid.NewGuid().ToString(),
                Installation = installations[1],
                Name = "Kårstø",
                PlantCode = "Kårstø",
            };

            return [plant1, plant2];
        }

        private static List<InspectionGroup> GetInspectionGroups()
        {
            var inspectionGroup1 = new InspectionGroup
            {
                Id = Guid.NewGuid().ToString(),
                Installation = installations[0],
                DefaultLocalizationPose = new DefaultLocalizationPose(),
                Name = "TestInspectionGroup",
            };

            var inspectionGroup2 = new InspectionGroup
            {
                Id = Guid.NewGuid().ToString(),
                Installation = installations[0],
                DefaultLocalizationPose = new DefaultLocalizationPose(),
                Name = "TestInspectionGroup2",
            };

            var inspectionGroup3 = new InspectionGroup
            {
                Id = Guid.NewGuid().ToString(),
                Installation = installations[0],
                DefaultLocalizationPose = new DefaultLocalizationPose(),
                Name = "TestInspectionGroup3",
            };

            var inspectionGroup4 = new InspectionGroup
            {
                Id = Guid.NewGuid().ToString(),
                Installation = installations[0],
                DefaultLocalizationPose = new DefaultLocalizationPose(),
                Name = "TestInspectionGroup4",
            };

            var inspectionGroupHuldraMezzanine = new InspectionGroup
            {
                Id = Guid.NewGuid().ToString(),
                Installation = installations[0],
                DefaultLocalizationPose = new DefaultLocalizationPose(),
                Name = "Huldra Mezzanine InspectionGroup",
            };

            var inspectionGroupKLab = new InspectionGroup
            {
                Id = Guid.NewGuid().ToString(),
                Installation = installations[1],
                DefaultLocalizationPose = new DefaultLocalizationPose(),
                Name = "K-Lab",
            };

            return
            [
                inspectionGroup1,
                inspectionGroup2,
                inspectionGroup3,
                inspectionGroup4,
                inspectionGroupHuldraMezzanine,
                inspectionGroupKLab,
            ];
        }

        private static List<Source> GetSources()
        {
            var source1 = new Source { SourceId = "986" };

            var source2 = new Source { SourceId = "990" };

            var source3 = new Source { SourceId = "991" };

            return [source1, source2, source3];
        }

        private static List<Robot> GetRobots()
        {
            var robot1 = new Robot
            {
                IsarId = "c68b679d-308b-460f-9fe0-87eaadbd8a6e",
                Name = "R2-D2",
                SerialNumber = "D2",
                Status = RobotStatus.Available,
                Host = "localhost",
                Port = 3000,
                CurrentInstallation = installations[0],
                Documentation = [],
                Pose = new Pose(),
            };

            var robot2 = new Robot
            {
                Name = "Shockwave",
                IsarId = "c68b679d-308b-460f-9fe0-87eaadbd1234",
                SerialNumber = "SS79",
                Status = RobotStatus.Busy,
                Host = "localhost",
                Port = 3000,
                CurrentInstallation = installations[0],
                Documentation = [],
                Pose = new Pose(),
            };

            var robot3 = new Robot
            {
                Name = "Ultron",
                IsarId = "c68b679d-308b-460f-9fe0-87eaadbd5678",
                SerialNumber = "Earth616",
                Status = RobotStatus.Available,
                Host = "localhost",
                Port = 3000,
                CurrentInstallation = installations[0],
                Documentation = [],
                Pose = new Pose(),
            };

            return [robot1, robot2, robot3];
        }

        private static List<MissionDefinition> GetMissionDefinitions()
        {
            var missionDefinition1 = new MissionDefinition
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Placeholder Mission 1",
                Installation = inspectionGroups[0].Installation!,
                InspectionGroups = [inspectionGroups[0]],
                Source = sources[0],
                Comment = "Interesting comment",
                InspectionFrequency = new DateTime().AddDays(12) - new DateTime(),
                LastSuccessfulRun = null,
            };

            var missionDefinition2 = new MissionDefinition
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Placeholder Mission 2",
                Installation = inspectionGroups[1].Installation!,
                InspectionGroups = [inspectionGroups[1]],
                Source = sources[1],
                InspectionFrequency = new DateTime().AddDays(7) - new DateTime(),
                LastSuccessfulRun = null,
            };

            var missionDefinition3 = new MissionDefinition
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Placeholder Mission 3",
                Installation = inspectionGroups[1].Installation!,
                InspectionGroups = [inspectionGroups[1]],
                Source = sources[2],
                LastSuccessfulRun = null,
            };

            var missionDefinition4 = new MissionDefinition
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Placeholder Mission 4",
                Installation = inspectionGroups[2].Installation,
                InspectionFrequency = new DateTime().AddDays(90) - new DateTime(),
                InspectionGroups = [inspectionGroups[2]],
                Source = sources[2],
                LastSuccessfulRun = null,
            };

            var missionDefinition5 = new MissionDefinition
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Placeholder Mission 5",
                Installation = inspectionGroups[2].Installation,
                InspectionFrequency = new DateTime().AddDays(35) - new DateTime(),
                InspectionGroups = [inspectionGroups[2]],
                Source = sources[2],
                LastSuccessfulRun = null,
            };

            var missionDefinition6 = new MissionDefinition
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Placeholder Mission 6",
                Installation = inspectionGroups[3].Installation,
                InspectionFrequency = new DateTime().AddDays(4) - new DateTime(),
                InspectionGroups = [inspectionGroups[3]],
                Source = sources[2],
                LastSuccessfulRun = null,
            };
            _ = new MissionDefinition
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Placeholder Mission 7",
                Installation = inspectionGroups[3].Installation,
                InspectionGroups = [inspectionGroups[4]],
                Source = sources[2],
                LastSuccessfulRun = null,
            };

            return
            [
                missionDefinition1,
                missionDefinition2,
                missionDefinition3,
                missionDefinition4,
                missionDefinition5,
                missionDefinition6,
            ];
        }

        private static List<MissionTask> GetMissionTasks()
        {
            var url = new Uri("https://testurl.test");
            var task1 = new MissionTask(
                inspection: new Inspection(),
                robotPose: new Pose(300.0f, 50.0f, 200.0f, 0.0f, 0.0f, 0.0f, 1.0f),
                taskOrder: 0,
                tagLink: url,
                tagId: "ABCD",
                taskDescription: "Task description",
                poseId: 2,
                status: TaskStatus.Successful
            );

            var task2 = new MissionTask(
                inspection: new Inspection(),
                robotPose: new Pose(300.0f, 50.0f, 200.0f, 0.0f, 0.0f, 0.0f, 1.0f),
                taskOrder: 0,
                tagLink: url,
                tagId: "ABCDE",
                taskDescription: "Task description",
                poseId: 2,
                status: TaskStatus.Failed
            );

            var task3 = new MissionTask(
                inspection: new Inspection(),
                robotPose: new Pose(300.0f, 50.0f, 200.0f, 0.0f, 0.0f, 0.0f, 1.0f),
                taskOrder: 0,
                tagLink: url,
                tagId: "ABCDEF",
                taskDescription: "Task description",
                poseId: 2,
                status: TaskStatus.PartiallySuccessful
            );

            var task4 = new MissionTask(
                inspection: new Inspection(),
                robotPose: new Pose(300.0f, 50.0f, 200.0f, 0.0f, 0.0f, 0.0f, 1.0f),
                taskOrder: 0,
                tagLink: url,
                tagId: "ABCDEFG",
                taskDescription: "Task description",
                poseId: 2,
                status: TaskStatus.Cancelled
            );

            var task5 = new MissionTask(
                inspection: new Inspection(),
                robotPose: new Pose(300.0f, 50.0f, 200.0f, 0.0f, 0.0f, 0.0f, 1.0f),
                taskOrder: 0,
                tagLink: url,
                tagId: "ABCDEFGH",
                taskDescription: "Task description",
                poseId: 2,
                status: TaskStatus.Failed
            );

            var task6 = new MissionTask(
                inspection: new Inspection(),
                robotPose: new Pose(300.0f, 50.0f, 200.0f, 0.0f, 0.0f, 0.0f, 1.0f),
                taskOrder: 0,
                tagLink: url,
                tagId: "ABCDEFGHI",
                taskDescription: "Task description",
                poseId: 2,
                status: TaskStatus.Failed
            );

            var task7 = new MissionTask(
                inspection: new Inspection(),
                robotPose: new Pose(300.0f, 50.0f, 200.0f, 0.0f, 0.0f, 0.0f, 1.0f),
                taskOrder: 0,
                tagLink: url,
                tagId: "ABCDEFGHIJ",
                taskDescription: "Task description",
                poseId: 2,
                status: TaskStatus.Failed
            );

            return [task1, task2, task3, task4, task5, task6, task7];
        }

        private static List<MissionRun> GetMissionRuns()
        {
            var missionRun1 = new MissionRun
            {
                Name = "Placeholder Mission 1",
                Robot = robots[0],
                Installation = inspectionGroups[0].Installation!,
                InspectionGroups = [inspectionGroups[0]],
                MissionId = missionDefinitions[0].Id,
                Status = MissionStatus.Successful,
                DesiredStartTime = DateTime.UtcNow,
                Tasks = [],
            };

            var missionRun2 = new MissionRun
            {
                Name = "Placeholder Mission 2",
                Robot = robots[1],
                Installation = inspectionGroups[1].Installation!,
                InspectionGroups = [inspectionGroups[1]],
                MissionId = missionDefinitions[0].Id,
                Status = MissionStatus.Successful,
                DesiredStartTime = DateTime.UtcNow,
                Tasks = [],
            };
            missionDefinitions[0].LastSuccessfulRun = missionRun2;

            var missionRun3 = new MissionRun
            {
                Name = "Placeholder Mission 3",
                Robot = robots[2],
                Installation = inspectionGroups[1].Installation!,
                InspectionGroups = [inspectionGroups[1]],
                MissionId = missionDefinitions[1].Id,
                Status = MissionStatus.Successful,
                DesiredStartTime = DateTime.UtcNow,
                Tasks = [],
            };

            var missionRun4 = new MissionRun
            {
                Name = "Placeholder Mission 4",
                Robot = robots[2],
                Installation = inspectionGroups[1].Installation,
                InspectionGroups = [inspectionGroups[1]],
                MissionId = missionDefinitions[1].Id,
                Status = MissionStatus.Failed,
                DesiredStartTime = DateTime.UtcNow,
                Tasks = [tasks[0], tasks[1]],
            };

            var missionRun5 = new MissionRun
            {
                Name = "Placeholder Mission 5",
                Robot = robots[2],
                Installation = inspectionGroups[1].Installation,
                InspectionGroups = [inspectionGroups[1]],
                MissionId = missionDefinitions[1].Id,
                Status = MissionStatus.PartiallySuccessful,
                DesiredStartTime = DateTime.UtcNow,
                Tasks = [tasks[0], tasks[2]],
            };

            var missionRun6 = new MissionRun
            {
                Name = "Placeholder Mission 6",
                Robot = robots[2],
                Installation = inspectionGroups[1].Installation,
                InspectionGroups = [inspectionGroups[1]],
                MissionId = missionDefinitions[1].Id,
                Status = MissionStatus.Cancelled,
                DesiredStartTime = DateTime.UtcNow,
                Tasks = [tasks[0], tasks[3]],
            };

            var missionRun7 = new MissionRun
            {
                Name = "Some failed tasks",
                Robot = robots[2],
                Installation = inspectionGroups[1].Installation,
                InspectionGroups = [inspectionGroups[1]],
                MissionId = missionDefinitions[1].Id,
                Status = MissionStatus.Failed,
                DesiredStartTime = DateTime.UtcNow,
                Tasks = [tasks[0], tasks[1], tasks[2], tasks[3], tasks[4], tasks[5], tasks[6]],
            };

            missionDefinitions[1].LastSuccessfulRun = missionRun3;

            return
            [
                missionRun1,
                missionRun2,
                missionRun3,
                missionRun4,
                missionRun5,
                missionRun6,
                missionRun7,
            ];
        }

        public static void AddRobotModelsToContext(FlotillaDbContext context)
        {
            foreach (var type in Enum.GetValues<RobotType>())
            {
                RobotModel model =
                    new()
                    {
                        Type = type,
                        BatteryWarningThreshold = null,
                        BatteryMissionStartThreshold = null,
                        LowerPressureWarningThreshold = null,
                        UpperPressureWarningThreshold = null,
                    };
                context.Add(model);
            }
        }

        public static void PopulateDb(FlotillaDbContext context)
        {
            // To make sure we are not trying to initialize database more than once during tests
            if (context.Robots.Any())
            {
                return;
            }

            context.AddRange(inspections);
            context.AddRange(installations);
            AddRobotModelsToContext(context);
            context.SaveChanges();
            var models = context.RobotModels.AsTracking().AsEnumerable().ToList();
            robots[0].Model = models.Find(model => model.Type == RobotType.TaurobInspector)!;
            robots[1].Model = models.Find(model => model.Type == RobotType.ExR2)!;
            robots[2].Model = models.Find(model => model.Type == RobotType.AnymalX)!;

            context.AddRange(robots);
            context.AddRange(plants);
            context.AddRange(inspectionGroups);
            context.AddRange(sources);

            var tasks = GetMissionTasks();
            foreach (var task in tasks)
            {
                task.Inspection = inspections[0];
            }
            context.AddRange(tasks);
            context.AddRange(missionDefinitions);
            context.AddRange(missionRuns);
            context.AddRange(accessRoles);

            context.SaveChanges();
            context.ChangeTracker.Clear();
        }
    }
}
