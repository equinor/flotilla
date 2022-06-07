using System.Text.Json;
using Api.Controllers.Models;
using Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("missions")]
public class MissionController : ControllerBase
{
    private readonly ILogger<MissionController> _logger;

    private readonly EchoService _echoService;

    public MissionController(ILogger<MissionController> logger, EchoService echoService)
    {
        _logger = logger;
        _echoService = echoService;
    }

    /// <summary>
    /// List all available missions on the asset
    /// </summary>
    /// <remarks>
    /// Overview
    ///
    /// List all available missions on the asset in the Echo mission planner
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(List<EchoMission>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<IList<EchoMission>>> GetMissions()
    {
        try
        {
            var missions = await _echoService.GetMissions();
            return Ok(missions);
        }
        catch (HttpRequestException e)
        {
            _logger.LogError(e, "Error retrieving missions from Echo");

            int statusCode = (int?)e.StatusCode ?? StatusCodes.Status502BadGateway;

            // If error is caused by user (400 codes), let them know
            if (400 <= statusCode && statusCode < 500)
                return new StatusCodeResult(statusCode);

            return new StatusCodeResult(StatusCodes.Status502BadGateway);
        }
        catch (JsonException e)
        {
            _logger.LogError(e, "Error retrieving missions from Echo");
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Lookup mission by specified missionId
    /// </summary>
    /// <remarks>
    /// Overview
    ///
    /// Lookup mission by specified missionId
    /// <para>Returns a mission corresponding to the specified missionId</para>
    /// </remarks>
    [HttpGet]
    [Route("{missionId}")]
    [ProducesResponseType(typeof(EchoMission), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<EchoMission>> GetMission([FromRoute] int missionId)
    {
        try
        {
            var mission = await _echoService.GetMissionById(missionId);
            return Ok(mission);
        }
        catch (HttpRequestException e)
        {
            _logger.LogError(e, "Error retrieving mission from Echo");

            int statusCode = (int?)e.StatusCode ?? StatusCodes.Status502BadGateway;

            // If error is caused by user (400 codes), let them know
            if (400 <= statusCode && statusCode < 500)
                return new StatusCodeResult(statusCode);

            return new StatusCodeResult(StatusCodes.Status502BadGateway);
        }
        catch (JsonException e)
        {
            _logger.LogError(e, "Error retrieving mission from Echo");
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }
}
