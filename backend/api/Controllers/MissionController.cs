using System.Text.Json;
using Api.Services;
using Api.Utilities;
using Database.Models;
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
    [ProducesResponseType(typeof(List<Mission>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IList<Mission>>> GetMissions()
    {
        try
        {
            var missions = await _echoService.GetMissions();
            return Ok(missions);
        }
        catch (Exception e) when (e is MissionNotFoundException || e is JsonException)
        {
            _logger.LogError(e, "Missions not found");
            return NotFound();
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
    [ProducesResponseType(typeof(Mission), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Mission>> GetMission([FromRoute] int missionId)
    {
        try
        {
            var mission = await _echoService.GetMission(missionId);
            return Ok(mission);
        }
        catch (Exception e) when (e is MissionNotFoundException || e is JsonException)
        {
            _logger.LogError(e, "Mission not found");
            return NotFound();
        }
    }
}
