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

    private static readonly DateTimeOffset ScheduledAt = DateTimeOffset.Parse(
        "2026-10-17T01:00:00.123456+02:00"
    );

    private Task<ActionResult> ChangeSkip(bool skip) =>
        skip
            ? Controller()
                .SkipAutoScheduledMissionRun(
                    "mission",
                    new SkipAutoMissionQuery { ScheduledAt = ScheduledAt }
                )
            : Controller().UnskipAutoScheduledMissionRun("mission", ScheduledAt);

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"TimeOfDay\":\"10:00:00\"}")]
    [InlineData("{\"scheduleKey\":\"old-key\"}")]
    public void LegacyOrMissingIdentityIsRejected(string json) =>
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<SkipAutoMissionQuery>(json));

    [Fact]
    public void SkipRequestPreservesTimestampPrecisionAndOffset()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var request = JsonSerializer.Deserialize<SkipAutoMissionQuery>(
            """{"scheduledAt":"2026-10-17T01:00:00.123456+02:00"}""",
            options
        );
        Assert.Equal(ScheduledAt, request.ScheduledAt);
        Assert.Equal(
            """{"scheduledAt":"2026-10-17T01:00:00.123456+02:00"}""",
            JsonSerializer.Serialize(request, options)
        );
    }

    [Fact]
    public async Task MissingIdentityHasNoSideEffects()
    {
        Assert.IsType<BadRequestObjectResult>(
            await Controller().SkipAutoScheduledMissionRun("mission", default)
        );
        Assert.IsType<BadRequestObjectResult>(
            await Controller().UnskipAutoScheduledMissionRun("mission", default)
        );
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
            _schedule.Setup(s => s.SkipOccurrence(definition, ScheduledAt)).ReturnsAsync(eligible);
        else
            _schedule
                .Setup(s => s.UnskipOccurrence(definition, ScheduledAt))
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
        controller.ModelState.AddModelError("ScheduledAt", "Invalid");
        var result = skip
            ? await controller.SkipAutoScheduledMissionRun(
                "mission",
                new SkipAutoMissionQuery { ScheduledAt = ScheduledAt }
            )
            : await controller.UnskipAutoScheduledMissionRun("mission", ScheduledAt);
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
                .Setup(s => s.SkipOccurrence(definition, ScheduledAt))
                .ThrowsAsync(new ArgumentException("Invalid scheduled timestamp"));
        else
            _schedule
                .Setup(s => s.UnskipOccurrence(definition, ScheduledAt))
                .ThrowsAsync(new ArgumentException("Invalid scheduled timestamp"));
        Assert.IsType<BadRequestObjectResult>(await ChangeSkip(skip));
    }
}
