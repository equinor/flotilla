using Api.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Context;

public class FlotillaDbContext : DbContext
{
    public DbSet<Robot> Robots => Set<Robot>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<IsarTask> Tasks => Set<IsarTask>();
    public DbSet<IsarStep> Steps => Set<IsarStep>();
    public DbSet<ScheduledMission> ScheduledMissions => Set<ScheduledMission>();

    private static bool initialized;

    public FlotillaDbContext(DbContextOptions options) : base(options)
    {
        if (initialized == false)
        {
            InitDb.PopulateDb(this);
            initialized = true;
        }
    }
}
