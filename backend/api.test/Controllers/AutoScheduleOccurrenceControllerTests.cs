using System;
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

    private Task<ActionResult> ChangeSkip(bool skip, string id = "tomorrow-key") =>
        skip
            ? Controller()
                .SkipAutoScheduledMissionRun(
                    "mission",
                    new SkipAutoMissionQuery { ScheduleKey = id }
                )
            : Controller().UnskipAutoScheduledMissionRun("mission", id);

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"TimeOfDay\":\"10:00:00\"}")]
    [InlineData("{\"occurrenceId\":\"old-key\"}")]
    public void LegacyOrMissingIdentityIsRejected(string json) =>
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<SkipAutoMissionQuery>(json));

    [Fact]
    public void SkipRequestUsesScheduleKey()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var request = JsonSerializer.Deserialize<SkipAutoMissionQuery>(
            """{"scheduleKey":"mission-time-key"}""",
            options
        );
        Assert.Equal("mission-time-key", request.ScheduleKey);
        Assert.Equal(
            """{"scheduleKey":"mission-time-key"}""",
            JsonSerializer.Serialize(request, options)
        );
    }

    [Fact]
    public async Task MissingIdentityHasNoSideEffects()
    {
        Assert.IsType<BadRequestObjectResult>(
            await Controller().SkipAutoScheduledMissionRun("mission", default)
        );
        Assert.IsType<BadRequestObjectResult>(await ChangeSkip(false, " \t"));
        Assert.IsType<BadRequestObjectResult>(await ChangeSkip(true, " \t"));
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
                .Setup(s => s.SkipOccurrence(definition, "tomorrow-key"))
                .ReturnsAsync(eligible);
        else
            _schedule
                .Setup(s => s.UnskipOccurrence(definition, "tomorrow-key"))
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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task InvalidModelHasNoSideEffects(bool skip)
    {
        var controller = Controller();
        controller.ModelState.AddModelError("ScheduleKey", "Invalid");
        var result = skip
            ? await controller.SkipAutoScheduledMissionRun(
                "mission",
                new SkipAutoMissionQuery { ScheduleKey = "mission-time-key" }
            )
            : await controller.UnskipAutoScheduledMissionRun("mission", "mission-time-key");
        Assert.IsType<BadRequestObjectResult>(result);
        _definitions.VerifyNoOtherCalls();
        _schedule.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task InvalidServiceInputReturnsBadRequest(bool skip)
    {
        var definition = new MissionDefinition { Id = "mission" };
        _definitions
            .Setup(s => s.ReadById("mission", true, AccessMode.Write))
            .ReturnsAsync(definition);
        if (skip)
            _schedule
                .Setup(s => s.SkipOccurrence(definition, "tomorrow-key"))
                .ThrowsAsync(new ArgumentException("Invalid schedule key"));
        else
            _schedule
                .Setup(s => s.UnskipOccurrence(definition, "tomorrow-key"))
                .ThrowsAsync(new ArgumentException("Invalid schedule key"));
        Assert.IsType<BadRequestObjectResult>(await ChangeSkip(skip));
    }
}
