using Api.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Database.Context;

public class FlotillaDbContext : DbContext
{
    public DbSet<Robot> Robots => Set<Robot>();
    public DbSet<Mission> Reports => Set<Mission>();
    public DbSet<IsarTask> Tasks => Set<IsarTask>();
    public DbSet<IsarStep> Steps => Set<IsarStep>();
    public DbSet<ScheduledMission> ScheduledMissions => Set<ScheduledMission>();
    public DbSet<VideoStream> VideoStreams => Set<VideoStream>();
    public DbSet<Pose> Poses => Set<Pose>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<Orientation> Orientations => Set<Orientation>();

    public FlotillaDbContext(DbContextOptions options) : base(options) { }
}
