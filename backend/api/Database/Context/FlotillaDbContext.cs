using Api.Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Api.Database.Context;

public class FlotillaDbContext : DbContext
{
    public DbSet<Robot> Robots => Set<Robot>();
    public DbSet<Mission> Missions => Set<Mission>();

    public FlotillaDbContext(DbContextOptions options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        bool isSqlLite = Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite";

        // https://docs.microsoft.com/en-us/ef/core/modeling/owned-entities
        // https://docs.microsoft.com/en-us/ef/core/modeling/owned-entities#collections-of-owned-types
        modelBuilder.Entity<Mission>(
            missionEntity =>
            {
                if (isSqlLite)
                    AddConverterForDateTimeOffsets(ref missionEntity);
                missionEntity.OwnsMany(
                    mission => mission.Tasks,
                    taskEntity =>
                    {
                        if (isSqlLite)
                            AddConverterForDateTimeOffsets(ref taskEntity);
                        taskEntity.OwnsMany(
                            task => task.Inspections,
                            inspectionEntity =>
                            {
                                if (isSqlLite)
                                    AddConverterForDateTimeOffsets(ref inspectionEntity);
                            }
                        );
                        taskEntity.OwnsOne(task => task.InspectionTarget);
                        taskEntity.OwnsOne(
                            task => task.RobotPose,
                            poseEntity =>
                            {
                                poseEntity.OwnsOne(pose => pose.Position);
                                poseEntity.OwnsOne(pose => pose.Orientation);
                            }
                        );
                    }
                );
            }
        );

        modelBuilder.Entity<Mission>().OwnsOne(m => m.Map).OwnsOne(t => t.TransformationMatrices);
        modelBuilder.Entity<Mission>().OwnsOne(m => m.Map).OwnsOne(b => b.Boundary);
        modelBuilder.Entity<Robot>().OwnsOne(r => r.Pose).OwnsOne(p => p.Orientation);
        modelBuilder.Entity<Robot>().OwnsOne(r => r.Pose).OwnsOne(p => p.Position);
        modelBuilder.Entity<Robot>().OwnsMany(r => r.VideoStreams);
    }

    // SQLite does not have proper support for DateTimeOffset via Entity Framework Core, see the limitations
    // here: https://docs.microsoft.com/en-us/ef/core/providers/sqlite/limitations#query-limitations
    // To work around this, when the Sqlite database provider is used, all model properties of type DateTimeOffset
    // use the DateTimeOffsetToBinaryConverter
    // Based on: https://github.com/aspnet/EntityFrameworkCore/issues/10784#issuecomment-415769754
    // This only supports millisecond precision, but should be sufficient for most use cases.
    private static void AddConverterForDateTimeOffsets<TOwnerEntity, TDependentEntity>(
        ref OwnedNavigationBuilder<TOwnerEntity, TDependentEntity> entity
    )
        where TOwnerEntity : class
        where TDependentEntity : class
    {
        var properties = entity.OwnedEntityType.ClrType
            .GetProperties()
            .Where(
                p =>
                    p.PropertyType == typeof(DateTimeOffset)
                    || p.PropertyType == typeof(DateTimeOffset?)
            );
        foreach (var property in properties)
        {
            entity.Property(property.Name).HasConversion(new DateTimeOffsetToBinaryConverter());
        }
    }

    private static void AddConverterForDateTimeOffsets<T>(ref EntityTypeBuilder<T> entity)
        where T : class
    {
        var properties = entity.Metadata.ClrType
            .GetProperties()
            .Where(
                p =>
                    p.PropertyType == typeof(DateTimeOffset)
                    || p.PropertyType == typeof(DateTimeOffset?)
            );
        foreach (var property in properties)
        {
            entity.Property(property.Name).HasConversion(new DateTimeOffsetToBinaryConverter());
        }
    }
}
