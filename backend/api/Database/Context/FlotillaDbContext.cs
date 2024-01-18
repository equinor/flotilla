using Api.Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
namespace Api.Database.Context
{
    public class FlotillaDbContext(DbContextOptions options) : DbContext(options)
    {
        public DbSet<Robot> Robots => Set<Robot>();
        public DbSet<RobotModel> RobotModels => Set<RobotModel>();
        public DbSet<MissionRun> MissionRuns => Set<MissionRun>();
        public DbSet<MissionTask> MissionTasks => Set<MissionTask>();
        public DbSet<Inspection> Inspections => Set<Inspection>();
        public DbSet<InspectionFinding> InspectionFindings => Set<InspectionFinding>();
        public DbSet<MissionDefinition> MissionDefinitions => Set<MissionDefinition>();
        public DbSet<Plant> Plants => Set<Plant>();
        public DbSet<Installation> Installations => Set<Installation>();
        public DbSet<Deck> Decks => Set<Deck>();
        public DbSet<Area> Areas => Set<Area>();
        public DbSet<Source> Sources => Set<Source>();
        public DbSet<SafePosition> SafePositions => Set<SafePosition>();
        public DbSet<DefaultLocalizationPose> DefaultLocalizationPoses => Set<DefaultLocalizationPose>();
        public DbSet<AccessRole> AccessRoles => Set<AccessRole>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            bool isSqlLite = Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite";

            // https://docs.microsoft.com/en-us/ef/core/modeling/owned-entities
            // https://docs.microsoft.com/en-us/ef/core/modeling/owned-entities#collections-of-owned-types
            modelBuilder.Entity<MissionRun>(missionRunEntity =>
            {
                if (isSqlLite) { AddConverterForDateTimeOffsets(ref missionRunEntity); }
            });
            modelBuilder.Entity<MissionTask>(missionTaskEntity =>
            {
                if (isSqlLite) { AddConverterForDateTimeOffsets(ref missionTaskEntity); }
                missionTaskEntity.OwnsOne(
                    task => task.RobotPose,
                    poseEntity =>
                    {
                        poseEntity.OwnsOne(pose => pose.Position);
                        poseEntity.OwnsOne(pose => pose.Orientation);
                    }
                );
            });

            modelBuilder.Entity<MissionDefinition>()
                .Property(m => m.InspectionFrequency)
                .HasConversion(new TimeSpanToTicksConverter());

            modelBuilder.Entity<MissionRun>().OwnsOne(m => m.Map).OwnsOne(t => t.TransformationMatrices);
            modelBuilder.Entity<MissionRun>().OwnsOne(m => m.Map).OwnsOne(b => b.Boundary);
            modelBuilder.Entity<Robot>().OwnsOne(r => r.Pose).OwnsOne(p => p.Orientation);
            modelBuilder.Entity<Robot>().OwnsOne(r => r.Pose).OwnsOne(p => p.Position);
            modelBuilder.Entity<Robot>().OwnsMany(r => r.VideoStreams);

            modelBuilder.Entity<SafePosition>().OwnsOne(s => s.Pose, poseBuilder =>
            {
                poseBuilder.OwnsOne(pose => pose.Position);
                poseBuilder.OwnsOne(pose => pose.Orientation);
            });
            modelBuilder.Entity<DefaultLocalizationPose>().OwnsOne(d => d.Pose, poseBuilder =>
            {
                poseBuilder.OwnsOne(pose => pose.Position);
                poseBuilder.OwnsOne(pose => pose.Orientation);
            });

            // There can only be one robot model per robot type
            modelBuilder.Entity<RobotModel>().HasIndex(model => model.Type).IsUnique();

            // There can only be one unique installation and plant shortname
            modelBuilder.Entity<Installation>().HasIndex(a => new
            {
                a.InstallationCode
            }).IsUnique();
            modelBuilder.Entity<Plant>().HasIndex(a => new
            {
                a.PlantCode
            }).IsUnique();

            modelBuilder.Entity<Area>().HasOne(a => a.Deck).WithMany().OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Area>().HasOne(a => a.Plant).WithMany().OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Area>().HasOne(a => a.Installation).WithMany().OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Deck>().HasOne(d => d.Plant).WithMany().OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Deck>().HasOne(d => d.Installation).WithMany().OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Plant>().HasOne(p => p.Installation).WithMany().OnDelete(DeleteBehavior.Restrict);
        }

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            configurationBuilder.Properties(typeof(Enum)).HaveConversion<string>();
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
}
