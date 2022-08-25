using Api.Database.Models;
using Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("missions")]
public class MissionController : ControllerBase
{
    private readonly IMissionService _missionService;

    public MissionController(IMissionService missionService)
    {
        _missionService = missionService;
    }

    /// <summary>
    /// List all missions on the asset.
    /// </summary>
    /// <remarks>
    /// <para> This query gets all missions (paginated) </para>
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(IList<Mission>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IList<Mission>>> GetMissions([FromQuery] string? assetCode)
    {
        IList<Mission> missions;
        if (assetCode != null)
            missions = await _missionService.ReadAll(assetCode);
        else
            missions = await _missionService.ReadAll();
        if (missions == null)
            return NotFound($"Could not find any missions matching the query");
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
        var mission = await _missionService.Read(id);
        if (mission == null)
            return NotFound($"Could not find mission with id {id}");
        return Ok(mission);
    }
}
