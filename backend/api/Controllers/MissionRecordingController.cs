using Api.Controllers.Models;
using Api.Services;
using Api.Utilities;
using Azure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("missions/runs/{runId}/recordings")]
[Authorize(Roles = Role.Admin)]
public class MissionRecordingController(
    IMissionRunService missionRunService,
    IMissionRecordingService missionRecordingService,
    ILogger<MissionRecordingController> logger
) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(MissionRecordingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(string), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<MissionRecordingResponse>> GetRecordings(
        [FromRoute] string runId,
        CancellationToken cancellationToken
    ) =>
        await ReadRecordingResponse(
            runId,
            async () => await missionRecordingService.GetRecordings(runId, cancellationToken)
        );

    [HttpGet("{cameraKey}/access")]
    [ProducesResponseType(typeof(RecordingAccessResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(string), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<RecordingAccessResponse>> GetRecordingAccess(
        [FromRoute] string runId,
        [FromRoute] string cameraKey,
        CancellationToken cancellationToken
    ) =>
        await ReadRecordingResponse(
            runId,
            () => missionRecordingService.GetRecordingAccess(runId, cameraKey, cancellationToken)
        );

    private async Task<ActionResult<T>> ReadRecordingResponse<T>(string runId, Func<Task<T?>> read)
        where T : class
    {
        var mission = await missionRunService.ReadById(runId);
        if (mission is null)
            return NotFound();
        Response.Headers.CacheControl = "no-store";
        try
        {
            var result = await read();
            return result is null ? NotFound() : Ok(result);
        }
        catch (ConfigException error)
        {
            logger.LogError(error, "Mission recording configuration is invalid");
            return StatusCode(503, "Recording storage configuration unavailable");
        }
        catch (RequestFailedException)
        {
            return StatusCode(503, "Recording storage unavailable");
        }
    }
}
