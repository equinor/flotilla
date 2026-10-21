using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
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
    public class ScheduledMissionRunControllerTests
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
        public async Task DefaultRequestIsRejectedWithoutSideEffects()
        {
            var result = await Controller().SkipAutoScheduledMissionRun("mission", default);
            Assert.IsType<BadRequestObjectResult>(result);
            _definitions.VerifyNoOtherCalls();
            _schedule.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData("{}")]
        [InlineData("{\"TimeOfDay\":\"10:00:00\"}")]
        public void MissingMissionRunIdIsRejectedDuringDeserialization(string json)
        {
            Assert.Throws<JsonException>(() =>
                JsonSerializer.Deserialize<SkipAutoMissionQuery>(json)
            );
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" \t")]
        public async Task MissingMissionRunIdFailsValidationAndControllerGuard(string? occurrenceId)
        {
            var query = JsonSerializer.Deserialize<SkipAutoMissionQuery>(
                JsonSerializer.Serialize(new { MissionRunId = occurrenceId })
            );
            var validationContext = new ValidationContext(query);
            var results = new List<ValidationResult>();
            Assert.False(
                Validator.TryValidateObject(
                    validationContext.ObjectInstance,
                    validationContext,
                    results,
                    true
                )
            );
            Assert.Contains(
                nameof(SkipAutoMissionQuery.MissionRunId),
                Assert.Single(results).MemberNames
            );

            Assert.IsType<BadRequestObjectResult>(
                await Controller().SkipAutoScheduledMissionRun("mission", query)
            );
            _definitions.VerifyNoOtherCalls();
            _schedule.VerifyNoOtherCalls();
        }

        [Fact]
        public void MissionRunIdPassesRequestValidation()
        {
            var query = new SkipAutoMissionQuery { MissionRunId = "tomorrow-occurrence" };
            Assert.Equal(
                query.MissionRunId,
                JsonSerializer
                    .Deserialize<SkipAutoMissionQuery>(JsonSerializer.Serialize(query))
                    .MissionRunId
            );
            var validationContext = new ValidationContext(query);
            Assert.True(
                Validator.TryValidateObject(
                    validationContext.ObjectInstance,
                    validationContext,
                    [],
                    true
                )
            );
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ExactOccurrenceIsUsedWithWriteAccessAndConflictIsExplicit(bool eligible)
        {
            var definition = new MissionDefinition { Id = "mission" };
            _definitions
                .Setup(s => s.ReadById("mission", true, AccessMode.Write, false))
                .ReturnsAsync(definition);
            _schedule
                .Setup(s => s.SkipMissionRun(definition, "tomorrow-occurrence"))
                .ReturnsAsync(eligible);
            var result = await Controller()
                .SkipAutoScheduledMissionRun(
                    "mission",
                    new SkipAutoMissionQuery { MissionRunId = "tomorrow-occurrence" }
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
                .Setup(s => s.ReadById("mission", true, AccessMode.Write, false))
                .ReturnsAsync((MissionDefinition?)null);
            Assert.IsType<NotFoundObjectResult>(
                await Controller()
                    .SkipAutoScheduledMissionRun(
                        "mission",
                        new SkipAutoMissionQuery { MissionRunId = "occurrence" }
                    )
            );
            _schedule.VerifyNoOtherCalls();
        }
    }
}
