using Api.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Database.Context;

public class FlotillaDbContext : DbContext
{
    public DbSet<Robot> Robots => Set<Robot>();
    public DbSet<Mission> Missions => Set<Mission>();
    public DbSet<IsarTask> Tasks => Set<IsarTask>();
    public DbSet<IsarStep> Steps => Set<IsarStep>();
    public DbSet<Mission> ScheduledMissions => Set<Mission>();
    public DbSet<VideoStream> VideoStreams => Set<VideoStream>();
    public DbSet<Pose> Poses => Set<Pose>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<Orientation> Orientations => Set<Orientation>();

    public FlotillaDbContext(DbContextOptions options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Defining this collection as owned because we shouldn't have a seperate table with the planned tasks,
        // they should only exist as a subset of the mission
        // https://docs.microsoft.com/en-us/ef/core/modeling/owned-entities
        // https://docs.microsoft.com/en-us/ef/core/modeling/owned-entities#collections-of-owned-types
        modelBuilder
            .Entity<Mission>()
            .OwnsMany(m => m.PlannedTasks)
            .OwnsMany(t => t.Inspections);
    }
}
