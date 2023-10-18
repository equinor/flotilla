using Api.Controllers.Models;
using Api.Database.Models;

namespace Api.Database.Context;

public static class InitDb
{
    private static readonly List<Robot> robots = GetRobots();
    private static readonly List<Installation> installations = GetInstallations();
    private static readonly List<Plant> plants = GetPlants();
    private static readonly List<Deck> decks = GetDecks();
    private static readonly List<Area> areas = GetAreas();
    private static readonly List<Source> sources = GetSources();
    private static readonly List<MissionTask> tasks = GetMissionTasks();
    private static readonly List<MissionDefinition> missionDefinitions = GetMissionDefinitions();
    private static readonly List<MissionRun> missionRuns = GetMissionRuns();

    private static VideoStream VideoStream =>
        new()
        {
            Name = "Front camera",
            Url = "http://localhost:5000/stream?topic=/camera/rgb/image_raw",
            Type = "mjpeg"
        };

    private static Inspection Inspection => new() { InspectionType = InspectionType.Image };
    private static Inspection Inspection2 => new() { InspectionType = InspectionType.ThermalImage };

    private static MissionTask ExampleTask =>
        new()
        {
            Inspections = new List<Inspection>(),
            TagId = "Tagid here",
            EchoTagLink = new Uri("https://www.I-am-echo-stid-tag-url.com"),
            InspectionTarget = new Position(),
            RobotPose = new Pose()
        };

    private static List<Installation> GetInstallations()
    {
        var installation1 = new Installation
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Johan Sverdrup",
            InstallationCode = "JSV"
        };

        return new List<Installation>(new Installation[] { installation1 });
    }

    private static List<Plant> GetPlants()
    {
        var plant1 = new Plant
        {
            Id = Guid.NewGuid().ToString(),
            Installation = installations[0],
            Name = "Johan Sverdrup - P1",
            PlantCode = "P1"
        };

        return new List<Plant>(new Plant[] { plant1 });
    }

    private static List<Deck> GetDecks()
    {
        var deck1 = new Deck
        {
            Id = Guid.NewGuid().ToString(),
            Plant = plants[0],
            Installation = plants[0].Installation,
            DefaultLocalizationPose = null,
            Name = "TestDeck"
        };

        var deck2 = new Deck
        {
            Id = Guid.NewGuid().ToString(),
            Plant = plants[0],
            Installation = plants[0].Installation,
            DefaultLocalizationPose = null,
            Name = "TestDeck2"
        };

        var deck3 = new Deck
        {
            Id = Guid.NewGuid().ToString(),
            Plant = plants[0],
            Installation = plants[0].Installation,
            DefaultLocalizationPose = null,
            Name = "TestDeck3"
        };

        var deck4 = new Deck
        {
            Id = Guid.NewGuid().ToString(),
            Plant = plants[0],
            Installation = plants[0].Installation,
            DefaultLocalizationPose = null,
            Name = "TestDeck4"
        };

        return new List<Deck>(new Deck[] { deck1, deck2, deck3, deck4 });
    }

    private static List<Area> GetAreas()
    {
        var area1 = new Area
        {
            Id = Guid.NewGuid().ToString(),
            Deck = decks[0],
            Plant = decks[0].Plant,
            Installation = decks[0].Plant!.Installation,
            Name = "AP320",
            MapMetadata = new MapMetadata(),
            DefaultLocalizationPose = null,
            SafePositions = new List<SafePosition>()
        };

        var area2 = new Area
        {
            Id = Guid.NewGuid().ToString(),
            Deck = decks[0],
            Plant = decks[0].Plant,
            Installation = decks[0].Plant!.Installation,
            Name = "AP330",
            MapMetadata = new MapMetadata(),
            DefaultLocalizationPose = null,
            SafePositions = new List<SafePosition>()
        };

        var area3 = new Area
        {
            Id = Guid.NewGuid().ToString(),
            Deck = decks[0],
            Plant = decks[0].Plant,
            Installation = decks[0].Plant!.Installation,
            Name = "testArea",
            MapMetadata = new MapMetadata(),
            DefaultLocalizationPose = new DefaultLocalizationPose(),
            SafePositions = new List<SafePosition>()
            {
                new()
            }
        };

        var area4 = new Area
        {
            Id = Guid.NewGuid().ToString(),
            Deck = decks[1],
            Plant = decks[1].Plant,
            Installation = decks[1].Plant.Installation,
            Name = "testArea2",
            MapMetadata = new MapMetadata(),
            DefaultLocalizationPose = null,
            SafePositions = new List<SafePosition>()
        };

        var area5 = new Area
        {
            Id = Guid.NewGuid().ToString(),
            Deck = decks[2],
            Plant = decks[2].Plant,
            Installation = decks[2].Plant.Installation,
            Name = "testArea3",
            MapMetadata = new MapMetadata(),
            DefaultLocalizationPose = null,
            SafePositions = new List<SafePosition>()
        };

        var area6 = new Area
        {
            Id = Guid.NewGuid().ToString(),
            Deck = decks[3],
            Plant = decks[3].Plant,
            Installation = decks[3].Plant.Installation,
            Name = "testArea4",
            MapMetadata = new MapMetadata(),
            DefaultLocalizationPose = null,
            SafePositions = new List<SafePosition>()
        };

        return new List<Area>(new Area[] { area1, area2, area3, area4, area5, area6 });
    }

    private static List<Source> GetSources()
    {
        var source1 = new Source
        {
            SourceId = "791",
            Type = MissionSourceType.Echo
        };

        var source2 = new Source
        {
            SourceId = "792",
            Type = MissionSourceType.Echo
        };

        var source3 = new Source
        {
            SourceId = "793",
            Type = MissionSourceType.Echo
        };

        return new List<Source>(new Source[] { source1, source2, source3 });
    }

    private static List<Robot> GetRobots()
    {
        var robot1 = new Robot
        {
            IsarId = "c68b679d-308b-460f-9fe0-87eaadbd8a6e",
            Name = "R2-D2",
            SerialNumber = "D2",
            Status = RobotStatus.Available,
            Enabled = true,
            Host = "localhost",
            Port = 3000,
            CurrentInstallation = "JSV",
            VideoStreams = new List<VideoStream>(),
            Pose = new Pose()
        };

        var robot2 = new Robot
        {
            Name = "Shockwave",
            IsarId = "c68b679d-308b-460f-9fe0-87eaadbd1234",
            SerialNumber = "SS79",
            Status = RobotStatus.Busy,
            Enabled = true,
            Host = "localhost",
            Port = 3000,
            CurrentInstallation = "JSV",
            VideoStreams = new List<VideoStream>(),
            Pose = new Pose()
        };

        var robot3 = new Robot
        {
            Name = "Ultron",
            IsarId = "c68b679d-308b-460f-9fe0-87eaadbd5678",
            SerialNumber = "Earth616",
            Status = RobotStatus.Available,
            Enabled = false,
            Host = "localhost",
            Port = 3000,
            CurrentInstallation = "JSV",
            VideoStreams = new List<VideoStream>(),
            Pose = new Pose()
        };

        return new List<Robot>(new Robot[] { robot1, robot2, robot3 });
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
            LastRun = null
        };

        var missionDefinition2 = new MissionDefinition
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Placeholder Mission 2",
            InstallationCode = areas[1].Installation!.InstallationCode,
            Area = areas[1],
            Source = sources[1],
            InspectionFrequency = new DateTime().AddDays(7) - new DateTime(),
            LastRun = null
        };

        var missionDefinition3 = new MissionDefinition
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Placeholder Mission 3",
            InstallationCode = areas[1].Installation!.InstallationCode,
            Area = areas[1],
            Source = sources[2],
            LastRun = null
        };

        var missionDefinition4 = new MissionDefinition
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Placeholder Mission 4",
            InstallationCode = areas[2].Installation.InstallationCode,
            InspectionFrequency = new DateTime().AddDays(90) - new DateTime(),
            Area = areas[2],
            Source = sources[2],
            LastRun = null
        };

        var missionDefinition5 = new MissionDefinition
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Placeholder Mission 5",
            InstallationCode = areas[2].Installation.InstallationCode,
            InspectionFrequency = new DateTime().AddDays(35) - new DateTime(),
            Area = areas[2],
            Source = sources[2],
            LastRun = null
        };

        var missionDefinition6 = new MissionDefinition
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Placeholder Mission 6",
            InstallationCode = areas[3].Installation.InstallationCode,
            InspectionFrequency = new DateTime().AddDays(4) - new DateTime(),
            Area = areas[3],
            Source = sources[2],
            LastRun = null
        };
        _ = new MissionDefinition
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Placeholder Mission 7",
            InstallationCode = areas[3].Installation.InstallationCode,
            Area = areas[4],
            Source = sources[2],
            LastRun = null
        };

        return new List<MissionDefinition>(new[] {
            missionDefinition1,
            missionDefinition2,
            missionDefinition3,
            missionDefinition4,
            missionDefinition5,
            missionDefinition6
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
                    $"https://stid.equinor.com/jsv/tag?tagNo=ABCD"
                ),
                Inspections = new List<EchoInspection>
                {
                    new()
                }
            }, new Position(1.0f, 1.0f, 1.0f));
        var task2 = new MissionTask(
            new EchoTag
            {
                Id = 2,
                TagId = "ABCD",
                PoseId = 2,
                PlanOrder = 0,
                Pose = new Pose(300.0f, 50.0f, 200.0f, 0.0f, 0.0f, 0.0f, 1.0f),
                URL = new Uri(
                    $"https://stid.equinor.com/jsv/tag?tagNo=ABCD"
                ),
                Inspections = new List<EchoInspection>
                {
                    new()
                }
            }, new Position(1.0f, 1.0f, 1.0f))
        {
            Status = Models.TaskStatus.Failed
        };

        var task3 = new MissionTask(
            new EchoTag
            {
                Id = 2,
                TagId = "ABCD",
                PoseId = 2,
                PlanOrder = 0,
                Pose = new Pose(300.0f, 50.0f, 200.0f, 0.0f, 0.0f, 0.0f, 1.0f),
                URL = new Uri(
                    $"https://stid.equinor.com/jsv/tag?tagNo=ABCD"
                ),
                Inspections = new List<EchoInspection>
                {
                    new()
                }
            }, new Position(1.0f, 1.0f, 1.0f))
        {
            Status = Models.TaskStatus.PartiallySuccessful
        };

        var task4 = new MissionTask(
            new EchoTag
            {
                Id = 2,
                TagId = "ABCD",
                PoseId = 2,
                PlanOrder = 0,
                Pose = new Pose(300.0f, 50.0f, 200.0f, 0.0f, 0.0f, 0.0f, 1.0f),
                URL = new Uri(
                    $"https://stid.equinor.com/jsv/tag?tagNo=ABCD"
                ),
                Inspections = new List<EchoInspection>
                {
                    new()
                }
            }, new Position(1.0f, 1.0f, 1.0f))
        {
            Status = Models.TaskStatus.Cancelled
        };

        return new List<MissionTask> { task1, task2, task3, task4 };
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
            DesiredStartTime = DateTimeOffset.UtcNow,
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
            DesiredStartTime = DateTimeOffset.UtcNow,
            Tasks = new List<MissionTask>(),
            Map = new MapMetadata()
        };
        missionDefinitions[0].LastRun = missionRun2;

        var missionRun3 = new MissionRun
        {
            Name = "Placeholder Mission 3",
            Robot = robots[2],
            InstallationCode = areas[1].Installation!.InstallationCode,
            Area = areas[1],
            MissionId = missionDefinitions[1].Id,
            Status = MissionStatus.Successful,
            DesiredStartTime = DateTimeOffset.UtcNow,
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
            DesiredStartTime = DateTimeOffset.UtcNow,
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
            DesiredStartTime = DateTimeOffset.UtcNow,
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
            DesiredStartTime = DateTimeOffset.UtcNow,
            Tasks = new List<MissionTask>
            {
                tasks[0],
                tasks[3]
            },
            Map = new MapMetadata()
        };

        var missionRun7 = new MissionRun
        {
            Name = "Says failed but all tasks succeeded",
            Robot = robots[2],
            InstallationCode = areas[1].Installation.InstallationCode,
            Area = areas[1],
            MissionId = missionDefinitions[1].Id,
            Status = MissionStatus.Failed,
            DesiredStartTime = DateTimeOffset.UtcNow,
            Tasks = new List<MissionTask>
            {
                tasks[0],
                tasks[0]
            },
            Map = new MapMetadata()
        };

        missionDefinitions[1].LastRun = missionRun3;

        return new List<MissionRun>(new[] { missionRun1, missionRun2, missionRun3, missionRun4, missionRun5, missionRun6, missionRun7 });
    }

    public static void AddRobotModelsToDatabase(FlotillaDbContext context)
    {
        foreach (var type in Enum.GetValues<RobotType>())
        {
            RobotModel model =
                new()
                {
                    Type = type,
                    BatteryWarningThreshold = 20f,
                    LowerPressureWarningThreshold = 40f,
                    UpperPressureWarningThreshold = 80f
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

        AddRobotModelsToDatabase(context);

        foreach (var robot in robots)
        {
            robot.VideoStreams.Add(VideoStream);
        }

        var models = context.RobotModels.AsEnumerable().ToList();
        robots[0].Model = models.Find(model => model.Type == RobotType.TaurobInspector)!;
        robots[1].Model = models.Find(model => model.Type == RobotType.ExR2)!;
        robots[2].Model = models.Find(model => model.Type == RobotType.AnymalX)!;

        foreach (var missionRun in missionRuns)
        {
            var task = ExampleTask;
            task.Inspections.Add(Inspection);
            task.Inspections.Add(Inspection2);
            var tasks = new List<MissionTask> { task };
            missionRun.Tasks = tasks;
        }
        context.AddRange(robots);
        context.AddRange(missionDefinitions);
        context.AddRange(missionRuns);
        context.AddRange(installations);
        context.AddRange(plants);
        context.AddRange(decks);
        context.AddRange(areas);
        context.SaveChanges();
    }
}
