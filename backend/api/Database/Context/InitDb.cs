using Api.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Database.Context;

public static class InitDb
{
    private static readonly VideoStream streamExample =
        new() { Name = "FrontCamera", Url = "localhost:3000_test" };

    public static readonly List<Robot> Robots = GetRobots();
    public static readonly List<ScheduledMission> ScheduledMissions = GetScheduledMissions();
    public static readonly List<Report> Reports = GetReports();

    private static List<Robot> GetRobots()
    {
        var robot1 = new Robot
        {
            Name = "william",
            Model = "Model1",
            SerialNumber = "123",
            Status = RobotStatus.Available,
            Enabled = false,
            Host = "localhost",
            Logs = "logs",
            Port = 3000,
        };

        var robot2 = new Robot
        {
            Name = "Robot2",
            Model = "Model2",
            SerialNumber = "456",
            Status = RobotStatus.Busy,
            Enabled = false,
            Host = "localhost",
            Logs = "logs",
            Port = 3000
        };

        return new List<Robot>(new Robot[] { robot1, robot2 });
    }

    private static List<Report> GetReports()
    {
        var report1 = new Report
        {
            EchoMissionId = "1",
            IsarMissionId = "1",
            Log = "log",
            ReportStatus = ReportStatus.NotStarted,
            Robot = Robots[0],
            StartTime = DateTimeOffset.UtcNow
        };
        return new List<Report>(
            new Report[] { report1 }
        );
    }


    private static List<ScheduledMission> GetScheduledMissions()
    {
        var scheduledMission1 = new ScheduledMission
        {
            Robot = Robots[0],
            EchoMissionId = 2,
            StartTime = DateTimeOffset.UtcNow.AddMinutes(1),
            EndTime = DateTimeOffset.UtcNow.AddMinutes(5),
            Status = ScheduledMissionStatus.Pending
        };

        var scheduledMission2 = new ScheduledMission
        {
            Robot = Robots[1],
            EchoMissionId = 2,
            StartTime = DateTimeOffset.UtcNow.AddMinutes(2),
            EndTime = DateTimeOffset.UtcNow.AddMinutes(5),
            Status = ScheduledMissionStatus.Pending
        };

        return new List<ScheduledMission>(
            new ScheduledMission[] { scheduledMission1, scheduledMission2 }
        );
    }



    public static void PopulateDb(FlotillaDbContext context)
    {
        foreach (var robot in Robots)
        {
            var videoStream = new VideoStream() { Name = "test", Url = "urlTest" };
            robot.VideoStreams = new List<VideoStream>() { videoStream, streamExample };
        }
        context.AddRange(Robots);
        context.AddRange(ScheduledMissions);
        context.AddRange(Reports);

        context.SaveChanges();
    }
}
