using Api.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Database.Context;

public class FlotillaDbContext : DbContext
{
    public DbSet<Robot> Robots => Set<Robot>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<IsarTask> Tasks => Set<IsarTask>();
    public DbSet<IsarStep> Steps => Set<IsarStep>();
    public DbSet<ScheduledMission> ScheduledMissions => Set<ScheduledMission>();
    public DbSet<VideoStream> VideoStreams => Set<VideoStream>();

    public FlotillaDbContext(DbContextOptions options) : base(options)
    { }
}
