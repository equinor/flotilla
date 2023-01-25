using System.Text.Json;
using Api.Controllers.Models;
using Api.Database.Models;
using Api.Services;
using Api.Utilities;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("missions")]
public class MissionController : ControllerBase
{
    private readonly IMissionService _missionService;
    private readonly ILogger<MissionController> _logger;
    private readonly IMapService _mapService;

    public MissionController(
        IMissionService missionService,
        ILogger<MissionController> logger,
        IMapService mapService
    )
    {
        _missionService = missionService;
        _mapService = mapService;
        _logger = logger;
    }

    /// <summary>
    /// List all missions in the Flotilla database
    /// </summary>
    /// <remarks>
    /// <para> This query gets all missions </para>
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(IList<Mission>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IList<Mission>>> GetMissions(
        [FromQuery] string? assetCode,
        [FromQuery] MissionStatus? status
    )
    {
        IList<Mission> missions;
        missions = await _missionService.ReadAll(assetCode, status);

        return Ok(missions);
    }

    /// <summary>
    /// Lookup mission by specified id.
    /// </summary>
    [HttpGet]
    [Route("{id}")]
    [ProducesResponseType(typeof(Mission), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Mission>> GetMissionById([FromRoute] string id)
    {
        var mission = await _missionService.ReadById(id);
        if (mission == null)
            return NotFound($"Could not find mission with id {id}");
        return Ok(mission);
    }

    /// <summary>
    /// Get map for mission with specified id.
    /// </summary>
    [HttpGet]
    [Route("{id}/map")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<byte[]>> GetMap([FromRoute] string id)
    {
        try
        {
            string filePath = await _mapService.FetchMapImage(id);
            var returnFile = PhysicalFile(filePath, "image/png");
            return returnFile;
        }
        catch (Azure.RequestFailedException)
        {
            return NotFound("Could not find map for this area.");
        }
        catch (MissionNotFoundException)
        {
            return NotFound("Could not find this mission");
        }
    }

    /// <summary>
    /// Schedule a new mission
    /// </summary>
    /// <remarks>
    /// <para> This query schedules a new mission and adds it to the database </para>
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(Mission), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Mission>> Create(
        [FromBody] ScheduledMissionQuery scheduledMissionQuery
    )
    {
        try
        {
            var newMission = await _missionService.Create(scheduledMissionQuery);
            return CreatedAtAction(nameof(GetMissionById), new { id = newMission.Id }, newMission);
        }
        catch (KeyNotFoundException e)
        {
            return NotFound(e.Message);
        }
        catch (HttpRequestException e)
        {
            return StatusCode(StatusCodes.Status502BadGateway, e.Message);
        }
        catch (JsonException e)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
        }
    }

    /// <summary>
    /// Deletes the mission with the specified id from the database.
    /// </summary>
    [HttpDelete]
    [Route("{id}")]
    [ProducesResponseType(typeof(Mission), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Mission>> DeleteMission([FromRoute] string id)
    {
        var mission = await _missionService.Delete(id);
        if (mission is null)
            return NotFound($"Mission with id {id} not found");
        return Ok(mission);
    }
}
