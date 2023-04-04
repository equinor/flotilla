using Api.Database.Models;

namespace Api.Database.Context;

public static class InitDb
{
    public static readonly List<Robot> Robots = GetRobots();
    public static readonly List<Mission> Missions = GetMissions();

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
            Model = RobotModel.TaurobInspector,
            SerialNumber = "D2",
            Status = RobotStatus.Available,
            Enabled = true,
            Host = "localhost",
            Logs = "",
            Port = 3000,
            VideoStreams = new List<VideoStream>(),
            Pose = new Pose()
        };

        var robot2 = new Robot
        {
            Name = "Shockwave",
            IsarId = "c68b679d-308b-460f-9fe0-87eaadbd1234",
            Model = RobotModel.ExR2,
            SerialNumber = "SS79",
            Status = RobotStatus.Busy,
            Enabled = true,
            Host = "localhost",
            Logs = "logs",
            Port = 3000,
            VideoStreams = new List<VideoStream>(),
            Pose = new Pose()
        };

        var robot3 = new Robot
        {
            Name = "Ultron",
            IsarId = "c68b679d-308b-460f-9fe0-87eaadbd5678",
            Model = RobotModel.AnymalX,
            SerialNumber = "Earth616",
            Status = RobotStatus.Available,
            Enabled = false,
            Host = "localhost",
            Logs = "logs",
            Port = 3000,
            VideoStreams = new List<VideoStream>(),
            Pose = new Pose()
        };

        return new List<Robot>(new Robot[] { robot1, robot2, robot3 });
    }

    private static List<Mission> GetMissions()
    {
        var mission1 = new Mission
        {
            Name = "Placeholder Mission 1",
            Robot = Robots[0],
            AssetCode = "test",
            EchoMissionId = 95,
            Status = MissionStatus.Successful,
            DesiredStartTime = DateTimeOffset.UtcNow,
            Tasks = new List<MissionTask>(),
            Map = new MissionMap()
        };

        var mission2 = new Mission
        {
            Name = "Placeholder Mission 2",
            Robot = Robots[1],
            AssetCode = "test",
            EchoMissionId = 95,
            Status = MissionStatus.Successful,
            DesiredStartTime = DateTimeOffset.UtcNow,
            Tasks = new List<MissionTask>(),
            Map = new MissionMap()
        };

        var mission3 = new Mission
        {
            Name = "Placeholder Mission 3",
            Robot = Robots[2],
            AssetCode = "kaa",
            Status = MissionStatus.Successful,
            DesiredStartTime = DateTimeOffset.UtcNow,
            Tasks = new List<MissionTask>(),
            Map = new MissionMap()
        };

        return new List<Mission>(new[] { mission1, mission2, mission3 });
    }

    public static void PopulateDb(FlotillaDbContext context)
    {
        // To make sure we are not trying to initialize database more than once during tests
        if (context.Robots.Any())
        {
            return;
        }
        foreach (var robot in Robots)
        {
            robot.VideoStreams.Add(VideoStream);
        }

        foreach (var mission in Missions)
        {
            var task = ExampleTask;
            task.Inspections.Add(Inspection);
            task.Inspections.Add(Inspection2);
            var tasks = new List<MissionTask> { task };
            mission.Tasks = tasks;
        }
        context.AddRange(Robots);
        context.AddRange(Missions);
        context.SaveChanges();
    }
}
