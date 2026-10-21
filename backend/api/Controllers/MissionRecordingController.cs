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
    /// <summary>Get mission camera recording status and short-lived playback and download URLs.</summary>
    /// <remarks>
    /// Requires Admin access. Existing missions without a recording return NotRecorded.
    /// Video URLs are valid for 30 minutes. Responses must not be cached.
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(MissionRecordingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(string), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<MissionRecordingResponse>> GetRecordings(
        [FromRoute] string runId,
        CancellationToken cancellationToken
    )
    {
        var mission = await missionRunService.ReadById(runId);
        if (mission is null)
            return NotFound();
        Response.Headers.CacheControl = "no-store";
        try
        {
            return Ok(await missionRecordingService.GetRecordings(runId, cancellationToken));
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
