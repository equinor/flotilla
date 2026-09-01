using System.Text.Json;
using Api.Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Api.Database.Context
{
    public class FlotillaDbContext : DbContext
    {
        public FlotillaDbContext(DbContextOptions options)
            : base(options)
        {
            ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        }

        public DbSet<Robot> Robots => Set<Robot>();
        public DbSet<MissionRun> MissionRuns => Set<MissionRun>();
        public DbSet<MissionTask> MissionTasks => Set<MissionTask>();
        public DbSet<MissionDefinition> MissionDefinitions => Set<MissionDefinition>();
        public DbSet<Plant> Plants => Set<Plant>();
        public DbSet<Installation> Installations => Set<Installation>();
        public DbSet<InspectionArea> InspectionAreas => Set<InspectionArea>();
        public DbSet<ExclusionArea> ExclusionAreas => Set<ExclusionArea>();
        public DbSet<AccessRole> AccessRoles => Set<AccessRole>();
        public DbSet<UserInfo> UserInfos => Set<UserInfo>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // https://docs.microsoft.com/en-us/ef/core/modeling/owned-entities
            // https://docs.microsoft.com/en-us/ef/core/modeling/owned-entities#collections-of-owned-types
            modelBuilder.Entity<MissionTask>(missionTaskEntity =>
            {
                missionTaskEntity.OwnsOne(
                    task => task.RobotPose,
                    poseEntity =>
                    {
                        poseEntity.OwnsOne(pose => pose.Position);
                        poseEntity.OwnsOne(pose => pose.Orientation);
                    }
                );
            });

            modelBuilder
                .Entity<MissionDefinition>()
                .OwnsMany(
                    p => p.Tasks,
                    tasks =>
                    {
                        tasks.WithOwner();
                        tasks.HasKey("MissionDefinitionId", "Index");
                        // HaveConversion<string>() convention does not apply to collection elements; must be explicit
                        tasks
                            .PrimitiveCollection(t => t.AnalysisTypes)
                            .ElementType()
                            .HasConversion<string>();
                    }
                );

            // Store enum arrays as text[] — HaveConversion<string>() does not cover collection elements
            modelBuilder
                .Entity<MissionTask>()
                .PrimitiveCollection(t => t.AnalysisTypes)
                .ElementType()
                .HasConversion<string>();

            modelBuilder
                .Entity<Robot>()
                .PrimitiveCollection(r => r.RobotCapabilities)
                .ElementType()
                .HasConversion<string>();

            modelBuilder
                .Entity<MissionDefinition>()
                .HasOne(m => m.InspectionArea)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder
                .Entity<MissionRun>()
                .HasOne(m => m.InspectionArea)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            // There can only be one unique installation and plant shortname
            modelBuilder
                .Entity<Installation>()
                .HasIndex(a => new { a.InstallationCode })
                .IsUnique();
            modelBuilder.Entity<Plant>().HasIndex(a => new { a.PlantCode }).IsUnique();

            modelBuilder
                .Entity<InspectionArea>()
                .HasOne(d => d.Plant)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder
                .Entity<InspectionArea>()
                .HasOne(d => d.Installation)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder
                .Entity<Plant>()
                .HasOne(p => p.Installation)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            var polygonPointsComparer = new ValueComparer<List<PolygonPoint>?>(
                (c1, c2) =>
                    (c1 == null && c2 == null)
                    || (c1 != null && c2 != null && c1.SequenceEqual(c2)),
                c => c == null ? 0 : c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c == null ? null : c.ToList()
            );

            modelBuilder
                .Entity<InspectionArea>()
                .OwnsOne(
                    i => i.AreaPolygon,
                    areaPolygon =>
                    {
                        areaPolygon.WithOwner();
#pragma warning disable CS8603
                        areaPolygon
                            .Property(p => p.Positions)
                            .HasConversion(
                                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                                v =>
                                    JsonSerializer.Deserialize<List<PolygonPoint>>(
                                        v,
                                        (JsonSerializerOptions?)null
                                    )
                            )
                            .Metadata.SetValueComparer(polygonPointsComparer);
#pragma warning restore CS8603
                    }
                );
            modelBuilder
                .Entity<ExclusionArea>()
                .OwnsOne(
                    i => i.AreaPolygon,
                    areaPolygon =>
                    {
                        areaPolygon.WithOwner();
#pragma warning disable CS8603
                        areaPolygon
                            .Property(p => p.Positions)
                            .HasConversion(
                                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                                v =>
                                    JsonSerializer.Deserialize<List<PolygonPoint>>(
                                        v,
                                        (JsonSerializerOptions?)null
                                    )
                            )
                            .Metadata.SetValueComparer(polygonPointsComparer);
#pragma warning restore CS8603
                    }
                );

            var schedulingTimesComparer = new ValueComparer<IList<TimeAndDay>>(
                (c1, c2) =>
                    (c1 == null && c2 == null)
                    || (c1 != null && c2 != null && c1.SequenceEqual(c2)),
                c => c == null ? 0 : c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c == null ? new List<TimeAndDay>() : c.ToList()
            );

            // Owned inline on the MissionDefinitions row; scheduling times stored as a JSON column.
            modelBuilder
                .Entity<MissionDefinition>()
                .OwnsOne(
                    m => m.AutoScheduleFrequency,
                    autoSchedule =>
                    {
                        autoSchedule.WithOwner();
                        autoSchedule
                            .Property(a => a.SchedulingTimesCETperWeek)
                            .HasConversion(
                                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                                v =>
                                    JsonSerializer.Deserialize<IList<TimeAndDay>>(
                                        v,
                                        (JsonSerializerOptions?)null
                                    ) ?? new List<TimeAndDay>()
                            )
                            .Metadata.SetValueComparer(schedulingTimesComparer);
                    }
                );
        }

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            configurationBuilder.Properties(typeof(Enum)).HaveConversion<string>();
        }
    }
}
