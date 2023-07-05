using Api.Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Api.Database.Context;

public class FlotillaDbContext : DbContext
{
    public DbSet<Robot> Robots => Set<Robot>();
    public DbSet<RobotModel> RobotModels => Set<RobotModel>();
    public DbSet<MissionRun> MissionRuns => Set<MissionRun>();
    public DbSet<MissionDefinition> MissionDefinitions => Set<MissionDefinition>();
    public DbSet<Installation> Installations => Set<Installation>();
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<Deck> Decks => Set<Deck>();
    public DbSet<Area> Areas => Set<Area>();
    public DbSet<SafePosition> SafePositions => Set<SafePosition>();

    public FlotillaDbContext(DbContextOptions options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        bool isSqlLite = Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite";

        // https://docs.microsoft.com/en-us/ef/core/modeling/owned-entities
        // https://docs.microsoft.com/en-us/ef/core/modeling/owned-entities#collections-of-owned-types
        modelBuilder.Entity<MissionRun>(
            missionRunEntity =>
            {
                if (isSqlLite)
                    AddConverterForDateTimeOffsets(ref missionRunEntity);
                missionRunEntity.OwnsMany(
                    missionRun => missionRun.Tasks,
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
                //missionRunEntity.HasOne(missionRun => missionRun.MissionDefinition);
            }
        );

        modelBuilder.Entity<MissionRun>().OwnsOne(m => m.MapMetadata).OwnsOne(t => t.TransformationMatrices);
        modelBuilder.Entity<MissionRun>().OwnsOne(m => m.MapMetadata).OwnsOne(b => b.Boundary);
        //modelBuilder.Entity<MissionDefinition>().HasOne(m => m.LastRun).WithOne(m => m.MissionDefinition).HasForeignKey<MissionRun>(m => m.Id);
        modelBuilder.Entity<Robot>().OwnsOne(r => r.Pose).OwnsOne(p => p.Orientation);
        modelBuilder.Entity<Robot>().OwnsOne(r => r.Pose).OwnsOne(p => p.Position);
        modelBuilder.Entity<Robot>().OwnsMany(r => r.VideoStreams);
        modelBuilder.Entity<Area>().OwnsOne(a => a.DefaultLocalizationPose, poseBuilder =>
        {
            poseBuilder.OwnsOne(pose => pose.Position);
            poseBuilder.OwnsOne(pose => pose.Orientation);
        });
        modelBuilder.Entity<Area>().HasOne(a => a.Deck);
        modelBuilder.Entity<Deck>().HasOne(a => a.Installation);
        modelBuilder.Entity<Installation>().HasOne(a => a.Asset);

        modelBuilder.Entity<SafePosition>().OwnsOne(s => s.Pose, poseBuilder =>
        {
            poseBuilder.OwnsOne(pose => pose.Position);
            poseBuilder.OwnsOne(pose => pose.Orientation);
        });

        // There can only be one robot model per robot type
        modelBuilder.Entity<RobotModel>().HasIndex(model => model.Type).IsUnique();

        // There can only be one unique asset and installation shortname
        modelBuilder.Entity<Asset>().HasIndex(a => new { a.AssetCode }).IsUnique();
        modelBuilder.Entity<Installation>().HasIndex(a => new { a.InstallationCode }).IsUnique();
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
