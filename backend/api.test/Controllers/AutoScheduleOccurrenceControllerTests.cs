using System.Threading.Tasks;
using Api.Controllers;
using Api.Controllers.Models;
using Api.Database.Models;
using Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Api.Test.Controllers
{
    public class AutoScheduleOccurrenceControllerTests
    {
        private readonly Mock<IMissionDefinitionService> _definitions = new(MockBehavior.Strict);
        private readonly Mock<IAutoScheduleService> _schedule = new(MockBehavior.Strict);

        private MissionDefinitionController Controller() =>
            new(
                NullLogger<MissionDefinitionController>.Instance,
                _definitions.Object,
                Mock.Of<IInstallationService>(),
                _schedule.Object,
                Mock.Of<IInspectionAreaService>()
            );

        [Fact]
        public async Task LegacyTimeOnlyRequestIsRejectedWithoutSideEffects()
        {
            var result = await Controller()
                .SkipAutoScheduledMissionRun("mission", new SkipAutoMissionQuery());
            Assert.IsType<BadRequestObjectResult>(result);
            _definitions.VerifyNoOtherCalls();
            _schedule.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ExactOccurrenceIsUsedWithWriteAccessAndConflictIsExplicit(bool eligible)
        {
            var definition = new MissionDefinition { Id = "mission" };
            _definitions
                .Setup(s => s.ReadById("mission", true, AccessMode.Write))
                .ReturnsAsync(definition);
            _schedule
                .Setup(s => s.SkipOccurrence(definition, "tomorrow-occurrence"))
                .ReturnsAsync(eligible);
            var result = await Controller()
                .SkipAutoScheduledMissionRun(
                    "mission",
                    new SkipAutoMissionQuery { OccurrenceId = "tomorrow-occurrence" }
                );
            if (eligible)
                Assert.IsType<NoContentResult>(result);
            else
                Assert.IsType<ConflictObjectResult>(result);
        }

        [Fact]
        public async Task InaccessibleMissionDoesNotReachSkipService()
        {
            _definitions
                .Setup(s => s.ReadById("mission", true, AccessMode.Write))
                .ReturnsAsync((MissionDefinition?)null);
            Assert.IsType<NotFoundObjectResult>(
                await Controller()
                    .SkipAutoScheduledMissionRun(
                        "mission",
                        new SkipAutoMissionQuery { OccurrenceId = "occurrence" }
                    )
            );
            _schedule.VerifyNoOtherCalls();
        }
    }
}
