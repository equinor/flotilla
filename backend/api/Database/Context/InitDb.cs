using Api.Database.Models;

namespace Api.Database.Context;

public static class InitDb
{
    private static readonly List<Robot> robots = GetRobots();
    private static readonly List<Asset> assets = GetAssets();
    private static readonly List<Installation> installations = GetInstallations();
    private static readonly List<Deck> decks = GetDecks();
    private static readonly List<Area> areas = GetAreas();
    private static readonly List<Source> sources = GetSources();
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
            VideoStreams = new List<VideoStream>(),
            Pose = new Pose()
        };

        return new List<Robot>(new Robot[] { robot1, robot2, robot3 });
    }

    private static List<Asset> GetAssets()
    {
        var asset1 = new Asset
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Johan Sverdrup",
            ShortName = "JSV"
        };

        return new List<Asset>(new Asset[] { asset1 });
    }

    private static List<Installation> GetInstallations()
    {
        var installation1 = new Installation
        {
            Id = Guid.NewGuid().ToString(),
            Asset = assets[0],
            Name = "Johan Sverdrup - P1",
            ShortName = "P1"
        };

        return new List<Installation>(new Installation[] { installation1 });
    }

    private static List<Deck> GetDecks()
    {
        var deck1 = new Deck
        {
            Id = Guid.NewGuid().ToString(),
            Installation = installations[0],
            Asset = installations[0].Asset,
            Name = "TestDeck"
        };

        return new List<Deck>(new Deck[] { deck1 });
    }

    private static List<Area> GetAreas()
    {
        var area1 = new Area
        {
            Id = Guid.NewGuid().ToString(),
            Deck = decks[0],
            Installation = decks[0].Installation,
            Asset = decks[0].Installation.Asset,
            Name = "AP320",
            MapMetadata = new MapMetadata(),
            DefaultLocalizationPose = new Pose {},
            SafePositions = new List<SafePosition>()
        };

        var area2 = new Area
        {
            Id = Guid.NewGuid().ToString(),
            Deck = decks[0],
            Installation = decks[0].Installation,
            Asset = decks[0].Installation.Asset,
            Name = "AP330",
            MapMetadata = new MapMetadata(),
            DefaultLocalizationPose = new Pose {},
            SafePositions = new List<SafePosition>()
        };

        var area3 = new Area
        {
            Id = "TestId",
            Deck = decks[0],
            Installation = decks[0].Installation,
            Asset = decks[0].Installation.Asset,
            Name = "testArea",
            MapMetadata = new MapMetadata(),
            DefaultLocalizationPose = new Pose { },
            SafePositions = new List<SafePosition>()
        };

        return new List<Area>(new Area[] { area1, area2, area3 });
    }

    private static List<Source> GetSources()
    {
        var source1 = new Source
        {
            Id = Guid.NewGuid().ToString(),
            URL = "https://google.com/",
            Type = MissionSourceType.Echo
        };

        var source2 = new Source
        {
            Id = Guid.NewGuid().ToString(),
            URL = "https://google.com/",
            Type = MissionSourceType.Custom
        };

        return new List<Source>(new Source[] { source1, source2 });
    }

    private static List<MissionDefinition> GetMissionDefinitions()
    {
        var mission1 = new MissionDefinition
        {
            Name = "Placeholder Mission 1",
            Id = Guid.NewGuid().ToString(),
            AssetCode = areas[0].Deck.Installation.Asset.ShortName,
            Area = areas[0],
            Source = sources[0],
            Comment = "Interesting comment",
            InspectionFrequency = new DateTime().AddDays(12) - new DateTime(),
            LastRun = null
        };

        var mission2 = new MissionDefinition
        {
            Name = "Placeholder Mission 2",
            Id = Guid.NewGuid().ToString(),
            AssetCode = areas[1].Deck.Installation.Asset.ShortName,
            Area = areas[1],
            Source = sources[1],
            InspectionFrequency = new DateTime().AddDays(7) - new DateTime(),
            LastRun = null
        };

        var mission3 = new MissionDefinition
        {
            Name = "Placeholder Mission 3",
            Id = Guid.NewGuid().ToString(),
            AssetCode = areas[1].Deck.Installation.Asset.ShortName,
            Area = areas[1],
            Source = sources[1],
            LastRun = null
        };

        return new List<MissionDefinition>(new[] { mission1, mission2, mission3 });
    }

    private static List<MissionRun> GetMissionRuns()
    {
        var mission1 = new MissionRun
        {
            Name = "Placeholder Mission 1",
            Robot = robots[0],
            AssetCode = areas[0].Deck.Installation.Asset.ShortName,
            Area = areas[0],
            MissionId = missionDefinitions[0].Id,
            Status = MissionStatus.Successful,
            DesiredStartTime = DateTimeOffset.UtcNow,
            Tasks = new List<MissionTask>(),
            MapMetadata = new MapMetadata()
        };

        var mission2 = new MissionRun
        {
            Name = "Placeholder Mission 2",
            Robot = robots[1],
            AssetCode = areas[1].Deck.Installation.Asset.ShortName,
            Area = areas[1],
            MissionId = missionDefinitions[0].Id,
            Status = MissionStatus.Successful,
            DesiredStartTime = DateTimeOffset.UtcNow,
            Tasks = new List<MissionTask>(),
            MapMetadata = new MapMetadata()
        };

        var mission3 = new MissionRun
        {
            Name = "Placeholder Mission 3",
            Robot = robots[2],
            AssetCode = areas[1].Deck.Installation.Asset.ShortName,
            Area = areas[1],
            MissionId = missionDefinitions[1].Id,
            Status = MissionStatus.Successful,
            DesiredStartTime = DateTimeOffset.UtcNow,
            Tasks = new List<MissionTask>(),
            MapMetadata = new MapMetadata()
        };

        return new List<MissionRun>(new[] { mission1, mission2, mission3 });
    }

    public static void PopulateDb(FlotillaDbContext context)
    {
        // To make sure we are not trying to initialize database more than once during tests
        if (context.Robots.Any())
        {
            return;
        }

        // Create robot models for the test database
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

        foreach (var robot in robots)
        {
            robot.VideoStreams.Add(VideoStream);
        }

        var models = context.RobotModels.AsEnumerable().ToList();
        robots[0].Model = models.Find(model => model.Type == RobotType.TaurobInspector)!;
        robots[1].Model = models.Find(model => model.Type == RobotType.ExR2)!;
        robots[2].Model = models.Find(model => model.Type == RobotType.AnymalX)!;

        foreach (var mission in missionRuns)
        {
            var task = ExampleTask;
            task.Inspections.Add(Inspection);
            task.Inspections.Add(Inspection2);
            var tasks = new List<MissionTask> { task };
            mission.Tasks = tasks;
        }
        context.AddRange(robots);
        context.AddRange(missionDefinitions);
        context.AddRange(missionRuns);
        context.AddRange(areas);
        context.SaveChanges();
    }
}
