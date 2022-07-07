using Api.Database.Models;

namespace Api.Context;

public static class InitDb
{
    private static readonly VideoStream streamExample =
        new() { Name = "FrontCamera", Url = "localhost:3000_test" };

    public static readonly List<Robot> Robots = GetRobots();

    private static List<Robot> GetRobots()
    {
        var robot1 = new Robot
        {
            Name = "william",
            Model = "Model1",
            SerialNumber = "123",
            Status = RobotStatus.Available,
            Enabled = true,
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
            Enabled = true,
            Host = "localhost",
            Logs = "logs",
            Port = 3000
        };

        return new List<Robot>(new Robot[] { robot1, robot2 });
    }

    public static readonly List<ScheduledMission> ScheduledMissions = GetScheduledMissions();

    private static List<ScheduledMission> GetScheduledMissions()
    {
        var scheduledMission1 = new ScheduledMission
        {
            Robot = Robots[0],
            EchoMissionId = "2",
            StartTime = DateTimeOffset.UtcNow.AddMinutes(1),
            EndTime = DateTimeOffset.UtcNow.AddMinutes(5),
            Status = ScheduledMissionStatus.Pending
        };

        var scheduledMission2 = new ScheduledMission
        {
            Robot = Robots[1],
            EchoMissionId = "2",
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
        context.SaveChanges();
    }
}
