using Api.Database.Models;
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
        private static readonly List<InspectionArea> inspectionAreas = GetInspectionAreas();
        private static readonly List<Area> areas = GetAreas();
        private static readonly List<Source> sources = GetSources();
        private static readonly List<MissionTask> tasks = GetMissionTasks();
        private static readonly List<MissionDefinition> missionDefinitions = GetMissionDefinitions();
        private static readonly List<MissionRun> missionRuns = GetMissionRuns();
        private static readonly List<AccessRole> accessRoles = GetAccessRoles();

        private static List<Inspection> GetInspections()
        {
            var inspection1 = new Inspection
            {
                InspectionType = InspectionType.Image
            };

            var inspection2 = new Inspection
            {
                InspectionType = InspectionType.ThermalImage
            };

            return new List<Inspection>(
            [
                inspection1,
                inspection2
            ]);
        }

        private static List<AccessRole> GetAccessRoles()
        {
            var accessRole1 = new AccessRole
            {
                Installation = installations[0],
                AccessLevel = RoleAccessLevel.ADMIN,
                RoleName = "Role.User.HUA"
            };

            return new List<AccessRole>(
            [
                accessRole1
            ]);
        }

        private static List<Installation> GetInstallations()
        {
            var installation1 = new Installation
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Huldra",
                InstallationCode = "HUA"
            };

            var installation2 = new Installation
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Kårstø",
                InstallationCode = "KAA"
            };

            return new List<Installation>(
            [
                installation1,
                installation2
            ]);
        }

        private static List<Plant> GetPlants()
        {
            var plant1 = new Plant
            {
                Id = Guid.NewGuid().ToString(),
                Installation = installations[0],
                Name = "HULDRA",
                PlantCode = "HUA"
            };

            var plant2 = new Plant
            {
                Id = Guid.NewGuid().ToString(),
                Installation = installations[1],
                Name = "Kårstø",
                PlantCode = "Kårstø"
            };

            return new List<Plant>(
            [
                plant1,
                plant2
            ]);
        }

        private static List<InspectionArea> GetInspectionAreas()
        {
            var inspectionArea1 = new InspectionArea
            {
                Id = Guid.NewGuid().ToString(),
                Plant = plants[0],
                Installation = plants[0].Installation,
                DefaultLocalizationPose = new DefaultLocalizationPose(),
                Name = "TestInspectionArea"
            };

            var inspectionArea2 = new InspectionArea
            {
                Id = Guid.NewGuid().ToString(),
                Plant = plants[0],
                Installation = plants[0].Installation,
                DefaultLocalizationPose = new DefaultLocalizationPose(),
                Name = "TestInspectionArea2"
            };

            var inspectionArea3 = new InspectionArea
            {
                Id = Guid.NewGuid().ToString(),
                Plant = plants[0],
                Installation = plants[0].Installation,
                DefaultLocalizationPose = new DefaultLocalizationPose(),
                Name = "TestInspectionArea3"
            };

            var inspectionArea4 = new InspectionArea
            {
                Id = Guid.NewGuid().ToString(),
                Plant = plants[0],
                Installation = plants[0].Installation,
                DefaultLocalizationPose = new DefaultLocalizationPose(),
                Name = "TestInspectionArea4"
            };

            var inspectionAreaHuldraMezzanine = new InspectionArea
            {
                Id = Guid.NewGuid().ToString(),
                Plant = plants[0],
                Installation = plants[0].Installation,
                DefaultLocalizationPose = new DefaultLocalizationPose(),
                Name = "Huldra Mezzanine InspectionArea"
            };

            var inspectionAreaKLab = new InspectionArea
            {
                Id = Guid.NewGuid().ToString(),
                Plant = plants[1],
                Installation = plants[1].Installation,
                DefaultLocalizationPose = new DefaultLocalizationPose(),
                Name = "K-Lab"
            };

            return new List<InspectionArea>(
            [
                inspectionArea1,
                inspectionArea2,
                inspectionArea3,
                inspectionArea4,
                inspectionAreaHuldraMezzanine,
                inspectionAreaKLab
            ]);
        }

        private static List<Area> GetAreas()
        {
            var area1 = new Area
            {
                Id = Guid.NewGuid().ToString(),
                InspectionArea = inspectionAreas[0],
                Plant = inspectionAreas[0].Plant,
                Installation = inspectionAreas[0].Installation,
                Name = "testArea",
                MapMetadata = new MapMetadata(),
                DefaultLocalizationPose = new DefaultLocalizationPose(),
            };

            var area2 = new Area
            {
                Id = Guid.NewGuid().ToString(),
                InspectionArea = inspectionAreas[0],
                Plant = inspectionAreas[0].Plant,
                Installation = inspectionAreas[0].Installation,
                Name = "testArea2",
                MapMetadata = new MapMetadata(),
                DefaultLocalizationPose = new DefaultLocalizationPose(),
            };

            var area3 = new Area
            {
                Id = Guid.NewGuid().ToString(),
                InspectionArea = inspectionAreas[0],
                Plant = inspectionAreas[0].Plant,
                Installation = inspectionAreas[0].Installation,
                Name = "testArea3",
                MapMetadata = new MapMetadata(),
                DefaultLocalizationPose = new DefaultLocalizationPose(),
            };

            var area4 = new Area
            {
                Id = Guid.NewGuid().ToString(),
                InspectionArea = inspectionAreas[1],
                Plant = inspectionAreas[1].Plant,
                Installation = inspectionAreas[1].Installation,
                Name = "testArea4",
                MapMetadata = new MapMetadata(),
                DefaultLocalizationPose = new DefaultLocalizationPose(),
            };

            var area5 = new Area
            {
                Id = Guid.NewGuid().ToString(),
                InspectionArea = inspectionAreas[2],
                Plant = inspectionAreas[2].Plant,
                Installation = inspectionAreas[2].Installation,
                Name = "testArea5",
                MapMetadata = new MapMetadata(),
                DefaultLocalizationPose = new DefaultLocalizationPose(),
            };

            var area6 = new Area
            {
                Id = Guid.NewGuid().ToString(),
                InspectionArea = inspectionAreas[3],
                Plant = inspectionAreas[3].Plant,
                Installation = inspectionAreas[3].Installation,
                Name = "testArea6",
                MapMetadata = new MapMetadata(),
                DefaultLocalizationPose = new DefaultLocalizationPose(),
            };

            var areaHuldraHB = new Area
            {
                Id = Guid.NewGuid().ToString(),
                InspectionArea = inspectionAreas[4],
                Plant = inspectionAreas[4].Plant,
                Installation = inspectionAreas[4].Installation,
                Name = "HB",
                MapMetadata = new MapMetadata(),
                DefaultLocalizationPose = new DefaultLocalizationPose(),
            };

            var areaKLab = new Area
            {
                Id = Guid.NewGuid().ToString(),
                InspectionArea = inspectionAreas[5],
                Plant = inspectionAreas[5].Plant,
                Installation = inspectionAreas[5].Installation,
                Name = "K-lab",
                MapMetadata = new MapMetadata(),
                DefaultLocalizationPose = new DefaultLocalizationPose(),
            };

            return new List<Area>(
            [
                area1,
                area2,
                area3,
                area4,
                area5,
                area6,
                areaHuldraHB,
                areaKLab
            ]);
        }

        private static List<Source> GetSources()
        {
            var source1 = new Source
            {
                SourceId = "986",
            };

            var source2 = new Source
            {
                SourceId = "990",
            };

            var source3 = new Source
            {
                SourceId = "991",
            };

            return new List<Source>(
            [
                source1,
                source2,
                source3
            ]);
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
                Pose = new Pose()
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
                Pose = new Pose()
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
                Pose = new Pose()
            };

            return new List<Robot>(
            [
                robot1,
                robot2,
                robot3
            ]);
        }

        private static List<MissionDefinition> GetMissionDefinitions()
        {
            var missionDefinition1 = new MissionDefinition
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Placeholder Mission 1",
                InstallationCode = inspectionAreas[0].Installation!.InstallationCode,
                InspectionArea = inspectionAreas[0],
                Source = sources[0],
                Comment = "Interesting comment",
                InspectionFrequency = new DateTime().AddDays(12) - new DateTime(),
                LastSuccessfulRun = null
            };

            var missionDefinition2 = new MissionDefinition
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Placeholder Mission 2",
                InstallationCode = inspectionAreas[1].Installation!.InstallationCode,
                InspectionArea = inspectionAreas[1],
                Source = sources[1],
                InspectionFrequency = new DateTime().AddDays(7) - new DateTime(),
                LastSuccessfulRun = null
            };

            var missionDefinition3 = new MissionDefinition
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Placeholder Mission 3",
                InstallationCode = inspectionAreas[1].Installation!.InstallationCode,
                InspectionArea = inspectionAreas[1],
                Source = sources[2],
                LastSuccessfulRun = null
            };

            var missionDefinition4 = new MissionDefinition
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Placeholder Mission 4",
                InstallationCode = inspectionAreas[2].Installation.InstallationCode,
                InspectionFrequency = new DateTime().AddDays(90) - new DateTime(),
                InspectionArea = inspectionAreas[2],
                Source = sources[2],
                LastSuccessfulRun = null
            };

            var missionDefinition5 = new MissionDefinition
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Placeholder Mission 5",
                InstallationCode = inspectionAreas[2].Installation.InstallationCode,
                InspectionFrequency = new DateTime().AddDays(35) - new DateTime(),
                InspectionArea = inspectionAreas[2],
                Source = sources[2],
                LastSuccessfulRun = null
            };

            var missionDefinition6 = new MissionDefinition
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Placeholder Mission 6",
                InstallationCode = inspectionAreas[3].Installation.InstallationCode,
                InspectionFrequency = new DateTime().AddDays(4) - new DateTime(),
                InspectionArea = inspectionAreas[3],
                Source = sources[2],
                LastSuccessfulRun = null
            };
            _ = new MissionDefinition
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Placeholder Mission 7",
                InstallationCode = inspectionAreas[3].Installation.InstallationCode,
                InspectionArea = inspectionAreas[4],
                Source = sources[2],
                LastSuccessfulRun = null
            };

            return new List<MissionDefinition>(
            [
                missionDefinition1,
                missionDefinition2,
                missionDefinition3,
                missionDefinition4,
                missionDefinition5,
                missionDefinition6
            ]);
        }

        private static List<MissionTask> GetMissionTasks()
        {
            var url = new Uri(
                    "https://stid.equinor.com/hua/tag?tagNo=ABCD"
                );
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

            return [
                task1,
                task2,
                task3,
                task4,
                task5,
                task6,
                task7
            ];
        }

        private static List<MissionRun> GetMissionRuns()
        {
            var missionRun1 = new MissionRun
            {
                Name = "Placeholder Mission 1",
                Robot = robots[0],
                InstallationCode = inspectionAreas[0].Installation!.InstallationCode,
                InspectionArea = inspectionAreas[0],
                MissionId = missionDefinitions[0].Id,
                Status = MissionStatus.Successful,
                DesiredStartTime = DateTime.UtcNow,
                Tasks = []
            };

            var missionRun2 = new MissionRun
            {
                Name = "Placeholder Mission 2",
                Robot = robots[1],
                InstallationCode = inspectionAreas[1].Installation!.InstallationCode,
                InspectionArea = inspectionAreas[1],
                MissionId = missionDefinitions[0].Id,
                Status = MissionStatus.Successful,
                DesiredStartTime = DateTime.UtcNow,
                Tasks = []
            };
            missionDefinitions[0].LastSuccessfulRun = missionRun2;

            var missionRun3 = new MissionRun
            {
                Name = "Placeholder Mission 3",
                Robot = robots[2],
                InstallationCode = inspectionAreas[1].Installation!.InstallationCode,
                InspectionArea = inspectionAreas[1],
                MissionId = missionDefinitions[1].Id,
                Status = MissionStatus.Successful,
                DesiredStartTime = DateTime.UtcNow,
                Tasks = []
            };

            var missionRun4 = new MissionRun
            {
                Name = "Placeholder Mission 4",
                Robot = robots[2],
                InstallationCode = inspectionAreas[1].Installation.InstallationCode,
                InspectionArea = inspectionAreas[1],
                MissionId = missionDefinitions[1].Id,
                Status = MissionStatus.Failed,
                DesiredStartTime = DateTime.UtcNow,
                Tasks =
                [
                    tasks[0],
                    tasks[1]
                ]
            };

            var missionRun5 = new MissionRun
            {
                Name = "Placeholder Mission 5",
                Robot = robots[2],
                InstallationCode = inspectionAreas[1].Installation.InstallationCode,
                InspectionArea = inspectionAreas[1],
                MissionId = missionDefinitions[1].Id,
                Status = MissionStatus.PartiallySuccessful,
                DesiredStartTime = DateTime.UtcNow,
                Tasks =
                [
                    tasks[0],
                    tasks[2]
                ]
            };

            var missionRun6 = new MissionRun
            {
                Name = "Placeholder Mission 6",
                Robot = robots[2],
                InstallationCode = inspectionAreas[1].Installation.InstallationCode,
                InspectionArea = inspectionAreas[1],
                MissionId = missionDefinitions[1].Id,
                Status = MissionStatus.Cancelled,
                DesiredStartTime = DateTime.UtcNow,
                Tasks =
                [
                    tasks[0],
                    tasks[3]
                ]
            };

            var missionRun7 = new MissionRun
            {
                Name = "Some failed tasks",
                Robot = robots[2],
                InstallationCode = inspectionAreas[1].Installation.InstallationCode,
                InspectionArea = inspectionAreas[1],
                MissionId = missionDefinitions[1].Id,
                Status = MissionStatus.Failed,
                DesiredStartTime = DateTime.UtcNow,
                Tasks =
                [
                    tasks[0],
                    tasks[1],
                    tasks[2],
                    tasks[3],
                    tasks[4],
                    tasks[5],
                    tasks[6]
                ]
            };

            missionDefinitions[1].LastSuccessfulRun = missionRun3;

            return new List<MissionRun>(
            [
                missionRun1,
                missionRun2,
                missionRun3,
                missionRun4,
                missionRun5,
                missionRun6,
                missionRun7
            ]);
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
                        UpperPressureWarningThreshold = null
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
            context.AddRange(inspectionAreas);
            context.AddRange(areas);
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
