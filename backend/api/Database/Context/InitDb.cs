using Api.Database.Models;

namespace Api.Database.Context;

public static class InitDb
{
    public static readonly List<Robot> Robots = GetRobots();
    public static readonly List<ScheduledMission> ScheduledMissions = GetScheduledMissions();
    public static readonly List<Report> Reports = GetReports();

    private static List<Robot> GetRobots()
    {
        var videoStream = new VideoStream()
        {
            Name = "Front camera",
            Url = "http://localhost:5000/stream?topic=/camera/rgb/image_raw"
        };

        var robot1 = new Robot
        {
            Name = "R2-D2",
            Model = "R2",
            SerialNumber = "D2",
            Status = RobotStatus.Available,
            Enabled = true,
            Host = "localhost",
            Logs = "",
            Port = 3000,
            VideoStreams = new List<VideoStream>() { videoStream },
            Pose = new Pose() { }
        };

        var robot2 = new Robot
        {
            Name = "Shockwave",
            Model = "Decepticon",
            SerialNumber = "SS79",
            Status = RobotStatus.Busy,
            Enabled = true,
            Host = "localhost",
            Logs = "logs",
            Port = 3000,
            VideoStreams = new List<VideoStream>() { videoStream },
            Pose = new Pose() { }
        };

        var robot3 = new Robot
        {
            Name = "Ultron",
            Model = "AISATW",
            SerialNumber = "Earth616",
            Status = RobotStatus.Available,
            Enabled = false,
            Host = "localhost",
            Logs = "logs",
            Port = 3000,
            VideoStreams = new List<VideoStream>() { videoStream },
            Pose = new Pose() { }
        };

        return new List<Robot>(new Robot[] { robot1, robot2, robot3 });
    }

    private static List<Report> GetReports()
    {
        var report1 = new Report
        {
            AssetCode = "test",
            EchoMissionId = 1,
            IsarMissionId = "1",
            Log = "log",
            ReportStatus = ReportStatus.NotStarted,
            Robot = Robots[0],
            StartTime = DateTimeOffset.UtcNow,
        };
        return new List<Report>(new Report[] { report1 });
    }

    private static List<ScheduledMission> GetScheduledMissions()
    {
        var scheduledMission1 = new ScheduledMission
        {
            Robot = Robots[0],
            EchoMissionId = 2,
            StartTime = DateTimeOffset.UtcNow.AddHours(7),
            EndTime = DateTimeOffset.UtcNow.AddHours(9),
            Status = ScheduledMissionStatus.Pending
        };

        var scheduledMission2 = new ScheduledMission
        {
            Robot = Robots[1],
            EchoMissionId = 2,
            StartTime = DateTimeOffset.UtcNow.AddHours(8),
            EndTime = DateTimeOffset.UtcNow.AddHours(9),
            Status = ScheduledMissionStatus.Pending
        };

        return new List<ScheduledMission>(
            new ScheduledMission[] { scheduledMission1, scheduledMission2 }
        );
    }

    public static void PopulateDb(FlotillaDbContext context)
    {
        context.AddRange(Robots);
        context.AddRange(ScheduledMissions);
        context.AddRange(Reports);

        context.SaveChanges();
    }
}
