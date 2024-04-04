using Api.Controllers.Models;
using Api.Database.Models;
using TaskStatus = Api.Database.Models.TaskStatus;
namespace Api.Database.Context
{
    public static class InitDb
    {
        private static readonly List<Inspection> inspections = GetInspections();
        private static readonly List<Installation> installations = GetInstallations();
        private static readonly List<Robot> robots = GetRobots();
        private static readonly List<Plant> plants = GetPlants();
        private static readonly List<Deck> decks = GetDecks();
        private static readonly List<Area> areas = GetAreas();
        private static readonly List<Source> sources = GetSources();
        private static readonly List<MissionTask> tasks = GetMissionTasks();
        private static readonly List<MissionDefinition> missionDefinitions = GetMissionDefinitions();
        private static readonly List<MissionRun> missionRuns = GetMissionRuns();
        private static readonly List<AccessRole> accessRoles = GetAccessRoles();

        private static VideoStream VideoStream =>
            new()
            {
                Name = "Front camera",
                Url = "http://localhost:5000/stream?topic=/camera/rgb/image_raw",
                Type = "mjpeg"
            };

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

            return new List<Inspection>(new[]
            {
                inspection1, inspection2
            });
        }

        private static List<AccessRole> GetAccessRoles()
        {
            var accessRole1 = new AccessRole
            {
                Installation = installations[0],
                AccessLevel = RoleAccessLevel.ADMIN,
                RoleName = "Role.User.HUA"
            };

            return new List<AccessRole>(new[]
            {
                accessRole1
            });
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

            return new List<Installation>(new[]
            {
                installation1, installation2
            });
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
                Installation = installations[0],
                Name = "Kårstø",
                PlantCode = "Kårstø"
            };

            return new List<Plant>(new[]
            {
                plant1, plant2
            });
        }

        private static List<Deck> GetDecks()
        {
            var deck1 = new Deck
            {
                Id = Guid.NewGuid().ToString(),
                Plant = plants[0],
                Installation = plants[0].Installation,
                DefaultLocalizationPose = new DefaultLocalizationPose(),
                Name = "TestDeck"
            };

            var deck2 = new Deck
            {
                Id = Guid.NewGuid().ToString(),
                Plant = plants[0],
                Installation = plants[0].Installation,
                DefaultLocalizationPose = new DefaultLocalizationPose(),
                Name = "TestDeck2"
            };

            var deck3 = new Deck
            {
                Id = Guid.NewGuid().ToString(),
                Plant = plants[0],
                Installation = plants[0].Installation,
                DefaultLocalizationPose = new DefaultLocalizationPose(),
                Name = "TestDeck3"
            };

            var deck4 = new Deck
            {
                Id = Guid.NewGuid().ToString(),
                Plant = plants[0],
                Installation = plants[0].Installation,
                DefaultLocalizationPose = new DefaultLocalizationPose(),
                Name = "TestDeck4"
            };

            var deckHuldraMezzanine = new Deck
            {
                Id = Guid.NewGuid().ToString(),
                Plant = plants[0],
                Installation = plants[0].Installation,
                DefaultLocalizationPose = new DefaultLocalizationPose(),
                Name = "Huldra Mezzanine Deck"
            };

            return new List<Deck>(new[]
            {
                deck1, deck2, deck3, deck4, deckHuldraMezzanine
            });
        }

        private static List<Area> GetAreas()
        {
            var area1 = new Area
            {
                Id = Guid.NewGuid().ToString(),
                Deck = decks[0],
                Plant = decks[0].Plant,
                Installation = decks[0].Plant!.Installation,
                Name = "testArea",
                MapMetadata = new MapMetadata(),
                DefaultLocalizationPose = new DefaultLocalizationPose(),
                SafePositions = new List<SafePosition>(new[] { new SafePosition() })
            };

            var area2 = new Area
            {
                Id = Guid.NewGuid().ToString(),
                Deck = decks[0],
                Plant = decks[0].Plant,
                Installation = decks[0].Plant!.Installation,
                Name = "testArea2",
                MapMetadata = new MapMetadata(),
                DefaultLocalizationPose = new DefaultLocalizationPose(),
                SafePositions = new List<SafePosition>(new[] { new SafePosition() })
            };

            var area3 = new Area
            {
                Id = Guid.NewGuid().ToString(),
                Deck = decks[0],
                Plant = decks[0].Plant,
                Installation = decks[0].Plant!.Installation,
                Name = "testArea3",
                MapMetadata = new MapMetadata(),
                DefaultLocalizationPose = new DefaultLocalizationPose(),
                SafePositions = new List<SafePosition>(new[] { new SafePosition() })
            };

            var area4 = new Area
            {
                Id = Guid.NewGuid().ToString(),
                Deck = decks[1],
                Plant = decks[1].Plant,
                Installation = decks[1].Plant.Installation,
                Name = "testArea4",
                MapMetadata = new MapMetadata(),
                DefaultLocalizationPose = new DefaultLocalizationPose(),
                SafePositions = new List<SafePosition>(new[] { new SafePosition() })
            };

            var area5 = new Area
            {
                Id = Guid.NewGuid().ToString(),
                Deck = decks[2],
                Plant = decks[2].Plant,
                Installation = decks[2].Plant.Installation,
                Name = "testArea5",
                MapMetadata = new MapMetadata(),
                DefaultLocalizationPose = new DefaultLocalizationPose(),
                SafePositions = new List<SafePosition>(new[] { new SafePosition() })
            };

            var area6 = new Area
            {
                Id = Guid.NewGuid().ToString(),
                Deck = decks[3],
                Plant = decks[3].Plant,
                Installation = decks[3].Plant.Installation,
                Name = "testArea6",
                MapMetadata = new MapMetadata(),
                DefaultLocalizationPose = new DefaultLocalizationPose(),
                SafePositions = new List<SafePosition>(new[] { new SafePosition() })
            };

            var areaHuldraHB = new Area
            {
                Id = Guid.NewGuid().ToString(),
                Deck = decks[4],
                Plant = decks[4].Plant,
                Installation = decks[4].Plant.Installation,
                Name = "HB",
                MapMetadata = new MapMetadata(),
                DefaultLocalizationPose = new DefaultLocalizationPose(),
                SafePositions = new List<SafePosition>(new[] { new SafePosition() })
            };

            return new List<Area>(new[]
            {
                area1, area2, area3, area4, area5, area6,areaHuldraHB
            });
        }

        private static List<Source> GetSources()
        {
            var source1 = new Source
            {
                SourceId = "986",
                Type = MissionSourceType.Echo
            };

            var source2 = new Source
            {
                SourceId = "990",
                Type = MissionSourceType.Echo
            };

            var source3 = new Source
            {
                SourceId = "991",
                Type = MissionSourceType.Echo
            };

            return new List<Source>(new[]
            {
                source1, source2, source3
            });
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
                VideoStreams = new List<VideoStream>(),
                Pose = new Pose(),
                PressureLevel = 0.001f
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
                VideoStreams = new List<VideoStream>(),
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
                VideoStreams = new List<VideoStream>(),
                Pose = new Pose()
            };

            return new List<Robot>(new[]
            {
                robot1, robot2, robot3
            });
        }

        private static List<MissionDefinition> GetMissionDefinitions()
        {
            var missionDefinition1 = new MissionDefinition
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Placeholder Mission 1",
                InstallationCode = areas[0].Installation!.InstallationCode,
                Area = areas[0],
                Source = sources[0],
                Comment = "Interesting comment",
                InspectionFrequency = new DateTime().AddDays(12) - new DateTime(),
                LastSuccessfulRun = null
            };

            var missionDefinition2 = new MissionDefinition
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Placeholder Mission 2",
                InstallationCode = areas[1].Installation!.InstallationCode,
                Area = areas[1],
                Source = sources[1],
                InspectionFrequency = new DateTime().AddDays(7) - new DateTime(),
                LastSuccessfulRun = null
            };

            var missionDefinition3 = new MissionDefinition
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Placeholder Mission 3",
                InstallationCode = areas[1].Installation!.InstallationCode,
                Area = areas[1],
                Source = sources[2],
                LastSuccessfulRun = null
            };

            var missionDefinition4 = new MissionDefinition
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Placeholder Mission 4",
                InstallationCode = areas[2].Installation.InstallationCode,
                InspectionFrequency = new DateTime().AddDays(90) - new DateTime(),
                Area = areas[2],
                Source = sources[2],
                LastSuccessfulRun = null
            };

            var missionDefinition5 = new MissionDefinition
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Placeholder Mission 5",
                InstallationCode = areas[2].Installation.InstallationCode,
                InspectionFrequency = new DateTime().AddDays(35) - new DateTime(),
                Area = areas[2],
                Source = sources[2],
                LastSuccessfulRun = null
            };

            var missionDefinition6 = new MissionDefinition
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Placeholder Mission 6",
                InstallationCode = areas[3].Installation.InstallationCode,
                InspectionFrequency = new DateTime().AddDays(4) - new DateTime(),
                Area = areas[3],
                Source = sources[2],
                LastSuccessfulRun = null
            };
            _ = new MissionDefinition
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Placeholder Mission 7",
                InstallationCode = areas[3].Installation.InstallationCode,
                Area = areas[4],
                Source = sources[2],
                LastSuccessfulRun = null
            };

            return new List<MissionDefinition>(new[]
            {
                missionDefinition1, missionDefinition2, missionDefinition3, missionDefinition4, missionDefinition5, missionDefinition6
            });
        }

        private static List<MissionTask> GetMissionTasks()
        {
            var task1 = new MissionTask(
                new EchoTag
                {
                    Id = 2,
                    TagId = "ABCD",
                    PoseId = 2,
                    PlanOrder = 0,
                    Pose = new Pose(300.0f, 50.0f, 200.0f, 0.0f, 0.0f, 0.0f, 1.0f),
                    URL = new Uri(
                        "https://stid.equinor.com/hua/tag?tagNo=ABCD"
                    ),
                    Inspections = new List<EchoInspection>
                    {
                        new()
                    }
                })
            {
                Status = TaskStatus.Successful
            };

            var task2 = new MissionTask(
                new EchoTag
                {
                    Id = 3,
                    TagId = "ABCDE",
                    PoseId = 2,
                    PlanOrder = 0,
                    Pose = new Pose(300.0f, 50.0f, 200.0f, 0.0f, 0.0f, 0.0f, 1.0f),
                    URL = new Uri(
                        "https://stid.equinor.com/hua/tag?tagNo=ABCD"
                    ),
                    Inspections = new List<EchoInspection>
                    {
                        new()
                    }
                })
            {
                Status = TaskStatus.Failed
            };

            var task3 = new MissionTask(
                new EchoTag
                {
                    Id = 4,
                    TagId = "ABCDEF",
                    PoseId = 2,
                    PlanOrder = 0,
                    Pose = new Pose(300.0f, 50.0f, 200.0f, 0.0f, 0.0f, 0.0f, 1.0f),
                    URL = new Uri(
                        "https://stid.equinor.com/hua/tag?tagNo=ABCD"
                    ),
                    Inspections = new List<EchoInspection>
                    {
                        new()
                    }
                })
            {
                Status = TaskStatus.PartiallySuccessful
            };

            var task4 = new MissionTask(
                new EchoTag
                {
                    Id = 5,
                    TagId = "ABCDEFG",
                    PoseId = 2,
                    PlanOrder = 0,
                    Pose = new Pose(300.0f, 50.0f, 200.0f, 0.0f, 0.0f, 0.0f, 1.0f),
                    URL = new Uri(
                        "https://stid.equinor.com/hua/tag?tagNo=ABCD"
                    ),
                    Inspections = new List<EchoInspection>
                    {
                        new()
                    }
                })
            {
                Status = TaskStatus.Cancelled
            };

            var task5 = new MissionTask(
                new EchoTag
                {
                    Id = 6,
                    TagId = "ABCDEFGH",
                    PoseId = 2,
                    PlanOrder = 0,
                    Pose = new Pose(300.0f, 50.0f, 200.0f, 0.0f, 0.0f, 0.0f, 1.0f),
                    URL = new Uri(
                        "https://stid.equinor.com/hua/tag?tagNo=ABCD"
                    ),
                    Inspections = new List<EchoInspection>
                    {
                        new()
                    }
                })
            {
                Status = TaskStatus.Failed
            };

            var task6 = new MissionTask(
                new EchoTag
                {
                    Id = 7,
                    TagId = "ABCDEFGHI",
                    PoseId = 2,
                    PlanOrder = 0,
                    Pose = new Pose(300.0f, 50.0f, 200.0f, 0.0f, 0.0f, 0.0f, 1.0f),
                    URL = new Uri(
                        "https://stid.equinor.com/hua/tag?tagNo=ABCD"
                    ),
                    Inspections = new List<EchoInspection>
                    {
                        new()
                    }
                })
            {
                Status = TaskStatus.Failed
            };

            var task7 = new MissionTask(
                new EchoTag
                {
                    Id = 8,
                    TagId = "ABCDEFGHIJ",
                    PoseId = 2,
                    PlanOrder = 0,
                    Pose = new Pose(300.0f, 50.0f, 200.0f, 0.0f, 0.0f, 0.0f, 1.0f),
                    URL = new Uri(
                        "https://stid.equinor.com/hua/tag?tagNo=ABCD"
                    ),
                    Inspections = new List<EchoInspection>
                    {
                        new()
                    }
                })
            {
                Status = TaskStatus.Failed
            };

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
                InstallationCode = areas[0].Installation!.InstallationCode,
                Area = areas[0],
                MissionId = missionDefinitions[0].Id,
                Status = MissionStatus.Successful,
                DesiredStartTime = DateTime.UtcNow,
                Tasks = new List<MissionTask>(),
                Map = new MapMetadata()
            };

            var missionRun2 = new MissionRun
            {
                Name = "Placeholder Mission 2",
                Robot = robots[1],
                InstallationCode = areas[1].Installation!.InstallationCode,
                Area = areas[1],
                MissionId = missionDefinitions[0].Id,
                Status = MissionStatus.Successful,
                DesiredStartTime = DateTime.UtcNow,
                Tasks = new List<MissionTask>(),
                Map = new MapMetadata()
            };
            missionDefinitions[0].LastSuccessfulRun = missionRun2;

            var missionRun3 = new MissionRun
            {
                Name = "Placeholder Mission 3",
                Robot = robots[2],
                InstallationCode = areas[1].Installation!.InstallationCode,
                Area = areas[1],
                MissionId = missionDefinitions[1].Id,
                Status = MissionStatus.Successful,
                DesiredStartTime = DateTime.UtcNow,
                Tasks = new List<MissionTask>(),
                Map = new MapMetadata()
            };

            var missionRun4 = new MissionRun
            {
                Name = "Placeholder Mission 4",
                Robot = robots[2],
                InstallationCode = areas[1].Installation.InstallationCode,
                Area = areas[1],
                MissionId = missionDefinitions[1].Id,
                Status = MissionStatus.Failed,
                DesiredStartTime = DateTime.UtcNow,
                Tasks = new List<MissionTask>
                {
                    tasks[0],
                    tasks[1]
                },
                Map = new MapMetadata()
            };

            var missionRun5 = new MissionRun
            {
                Name = "Placeholder Mission 5",
                Robot = robots[2],
                InstallationCode = areas[1].Installation.InstallationCode,
                Area = areas[1],
                MissionId = missionDefinitions[1].Id,
                Status = MissionStatus.PartiallySuccessful,
                DesiredStartTime = DateTime.UtcNow,
                Tasks = new List<MissionTask>
                {
                    tasks[0],
                    tasks[2]
                },
                Map = new MapMetadata()
            };

            var missionRun6 = new MissionRun
            {
                Name = "Placeholder Mission 6",
                Robot = robots[2],
                InstallationCode = areas[1].Installation.InstallationCode,
                Area = areas[1],
                MissionId = missionDefinitions[1].Id,
                Status = MissionStatus.Cancelled,
                DesiredStartTime = DateTime.UtcNow,
                Tasks = new List<MissionTask>
                {
                    tasks[0],
                    tasks[3]
                },
                Map = new MapMetadata()
            };

            var missionRun7 = new MissionRun
            {
                Name = "Some failed tasks",
                Robot = robots[2],
                InstallationCode = areas[1].Installation.InstallationCode,
                Area = areas[1],
                MissionId = missionDefinitions[1].Id,
                Status = MissionStatus.Failed,
                DesiredStartTime = DateTime.UtcNow,
                Tasks = new List<MissionTask>
                {
                    tasks[0],
                    tasks[1],
                    tasks[2],
                    tasks[3],
                    tasks[4],
                    tasks[5],
                    tasks[6]
                },
                Map = new MapMetadata()
            };

            missionDefinitions[1].LastSuccessfulRun = missionRun3;

            return new List<MissionRun>(new[]
            {
                missionRun1, missionRun2, missionRun3, missionRun4, missionRun5, missionRun6, missionRun7
            });
        }

        public static void AddRobotModelsToDatabase(FlotillaDbContext context)
        {
            foreach (var type in Enum.GetValues<RobotType>())
            {
                RobotModel model =
                    new()
                    {
                        Type = type,
                        BatteryWarningThreshold = 0f,
                        LowerPressureWarningThreshold = 0.01f,
                        UpperPressureWarningThreshold = 0.08f
                    };
                context.Add(model);
            }
            context.SaveChanges();
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
            AddRobotModelsToDatabase(context);
            foreach (var robot in robots)
                robot.VideoStreams.Add(VideoStream);
            var models = context.RobotModels.AsEnumerable().ToList();
            robots[0].Model = models.Find(model => model.Type == RobotType.TaurobInspector)!;
            robots[1].Model = models.Find(model => model.Type == RobotType.ExR2)!;
            robots[2].Model = models.Find(model => model.Type == RobotType.AnymalX)!;

            context.AddRange(robots);
            context.AddRange(plants);
            context.AddRange(decks);
            context.AddRange(areas);
            context.AddRange(sources);

            var tasks = GetMissionTasks();
            foreach (var task in tasks)
            {
                task.Inspections.Add(inspections[0]);
                task.Inspections.Add(inspections[1]);
            }
            context.AddRange(tasks);
            context.AddRange(missionDefinitions);
            context.AddRange(missionRuns);
            context.AddRange(accessRoles);

            context.SaveChanges();
        }
    }
}
