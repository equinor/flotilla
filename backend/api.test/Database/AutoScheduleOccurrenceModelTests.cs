using System.Linq;
using Api.Database.Context;
using Api.Database.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Api.Test.Database
{
    public class AutoScheduleOccurrenceModelTests
    {
        [Fact]
        public void OccurrenceModelPreservesUniqueSlotAndRequiredCascadeRelationship()
        {
            using var context = new FlotillaDbContext(
                new DbContextOptionsBuilder<FlotillaDbContext>()
                    .UseNpgsql("Host=localhost;Database=model_check")
                    .Options
            );
            var occurrence = context.Model.FindEntityType(typeof(AutoScheduleOccurrence));
            Assert.NotNull(occurrence);

            var index = Assert.Single(occurrence.GetIndexes());
            Assert.True(index.IsUnique);
            Assert.Equal(
                [
                    nameof(AutoScheduleOccurrence.MissionDefinitionId),
                    nameof(AutoScheduleOccurrence.ScheduledAt),
                ],
                index.Properties.Select(property => property.Name)
            );

            var relationship = Assert.Single(occurrence.GetForeignKeys());
            Assert.Equal(typeof(MissionDefinition), relationship.PrincipalEntityType.ClrType);
            Assert.Equal(
                nameof(AutoScheduleOccurrence.MissionDefinitionId),
                Assert.Single(relationship.Properties).Name
            );
            Assert.True(relationship.IsRequired);
            Assert.Equal(DeleteBehavior.Cascade, relationship.DeleteBehavior);
            Assert.Equal(
                nameof(AutoScheduleOccurrence.MissionDefinition),
                relationship.DependentToPrincipal?.Name
            );
        }
    }
}
