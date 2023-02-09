using Api.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Database.Context;

public class FlotillaDbContext : DbContext
{
    public DbSet<Robot> Robots => Set<Robot>();
    public DbSet<Mission> Missions => Set<Mission>();

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
        modelBuilder.Entity<Mission>().OwnsMany(m => m.PlannedTasks).OwnsOne(r => r.TagPosition);
        modelBuilder.Entity<Mission>().OwnsMany(m => m.PlannedTasks).OwnsOne(r => r.Pose).OwnsOne(p => p.Position);
        modelBuilder.Entity<Mission>().OwnsMany(m => m.PlannedTasks).OwnsOne(r => r.Pose).OwnsOne(p => p.Orientation);
        modelBuilder.Entity<Mission>().OwnsMany(m => m.Tasks).OwnsMany(t => t.Steps);
        modelBuilder.Entity<Mission>().OwnsOne(m => m.Map).OwnsOne(t => t.TransformationMatrices);
        modelBuilder.Entity<Mission>().OwnsOne(m => m.Map).OwnsOne(b => b.Boundary);
        modelBuilder.Entity<Robot>().OwnsOne(r => r.Pose).OwnsOne(p => p.Orientation);
        modelBuilder.Entity<Robot>().OwnsOne(r => r.Pose).OwnsOne(p => p.Position);
        modelBuilder.Entity<Robot>().OwnsMany(r => r.VideoStreams);
    }
}
