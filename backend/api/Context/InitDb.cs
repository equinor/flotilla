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
            Name = "Robot1",
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
            IsarMissionId = "2",
            StartTime = DateTimeOffset.UtcNow,
            EndTime = DateTimeOffset.UtcNow,
            Status = ScheduledMissionStatus.Successful
        };

        var scheduledMission2 = new ScheduledMission
        {
            Robot = Robots[1],
            IsarMissionId = "2",
            StartTime = DateTimeOffset.UtcNow,
            EndTime = DateTimeOffset.UtcNow,
            Status = ScheduledMissionStatus.Successful
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
