using api.Models;

namespace api.Context;

public static class InitDb
{
    public static readonly List<Robot> Robots = GetRobots();

    private static List<Robot> GetRobots()
    {
        Robot robot1 = new Robot
        {
            Name = "Robot1",
            Model = "Model1",
            SerialNumber = "123",
            Status = RobotStatus.Available
        };

        Robot robot2 = new Robot
        {
            Name = "Robot2",
            Model = "Model2",
            SerialNumber = "456",
            Status = RobotStatus.Busy
        };

        return new List<Robot>(new Robot[] { robot1, robot2 });
    }

    public static void PopulateDb(FlotillaDbContext context)
    {
        context.AddRange(Robots);

        context.SaveChanges();
    }
}
