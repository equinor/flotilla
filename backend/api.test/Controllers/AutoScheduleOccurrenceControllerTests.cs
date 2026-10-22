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

namespace Api.Test.Controllers;

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

    private Task<ActionResult> ChangeSkip(bool skip, string id = "tomorrow-occurrence") =>
        skip
            ? Controller()
                .SkipAutoScheduledMissionRun(
                    "mission",
                    new SkipAutoMissionQuery { OccurrenceId = id }
                )
            : Controller().UnskipAutoScheduledMissionRun("mission", id);

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"TimeOfDay\":\"10:00:00\"}")]
    public void LegacyOrMissingIdentityIsRejected(string json) =>
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<SkipAutoMissionQuery>(json));

    [Fact]
    public async Task MissingIdentityHasNoSideEffects()
    {
        Assert.IsType<BadRequestObjectResult>(
            await Controller().SkipAutoScheduledMissionRun("mission", default)
        );
        Assert.IsType<BadRequestObjectResult>(await ChangeSkip(false, " \t"));
        _definitions.VerifyNoOtherCalls();
        _schedule.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public async Task ChangesUseExactIdentityAndWriteAccess(bool skip, bool eligible)
    {
        var definition = new MissionDefinition { Id = "mission" };
        _definitions
            .Setup(s => s.ReadById("mission", true, AccessMode.Write))
            .ReturnsAsync(definition);
        if (skip)
            _schedule
                .Setup(s => s.SkipOccurrence(definition, "tomorrow-occurrence"))
                .ReturnsAsync(eligible);
        else
            _schedule
                .Setup(s => s.UnskipOccurrence(definition, "tomorrow-occurrence"))
                .ReturnsAsync(eligible);
        var result = await ChangeSkip(skip);
        if (eligible)
            Assert.IsType<NoContentResult>(result);
        else
            Assert.IsType<ConflictObjectResult>(result);
        _schedule.VerifyAll();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task InaccessibleMissionHasNoSideEffects(bool skip)
    {
        _definitions
            .Setup(s => s.ReadById("mission", true, AccessMode.Write))
            .ReturnsAsync((MissionDefinition?)null);
        Assert.IsType<NotFoundObjectResult>(await ChangeSkip(skip));
        _schedule.VerifyNoOtherCalls();
    }
}
