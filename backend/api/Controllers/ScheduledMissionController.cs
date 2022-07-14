using Api.Controllers.Models;
using Api.Database.Models;
using Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("scheduled-missions")]
public class ScheduledMissionController : ControllerBase
{
    private readonly IScheduledMissionService _scheduledMissionService;
    private readonly IRobotService _robotService;

    public ScheduledMissionController(
        IScheduledMissionService scheduledMissionService,
        IRobotService robotService
    )
    {
        _scheduledMissionService = scheduledMissionService;
        _robotService = robotService;
    }

    /// <summary>
    /// Gets a list of the scheduled missions in the database.
    /// </summary>
    /// <remarks>
    /// <para> This query gets all scheduled missions (paginated) </para>
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(IList<ScheduledMission>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IList<ScheduledMission>>> GetScheduledMissions()
    {
        var scheduledMissions = await _scheduledMissionService.ReadAll();
        return Ok(scheduledMissions);
    }

    /// <summary>
    /// Lookup scheduled mission with specified id
    /// </summary>
    /// <remarks>
    /// <para> This query gets the scheduled mission with the specified id </para>
    /// </remarks>
    [HttpGet]
    [Route("{id}")]
    [ProducesResponseType(typeof(ScheduledMission), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ScheduledMission>> GetScheduledMissionById([FromRoute] string id)
    {
        var scheduledMission = await _scheduledMissionService.ReadById(id);
        if (scheduledMission is null)
            return NotFound($"Scheduled mission with id {id} not found");
        return Ok(scheduledMission);
    }

    /// <summary>
    /// Lookup upcoming scheduled missions
    /// </summary>
    /// <remarks>
    /// <para> This query gets upcoming scheduled missions </para>
    /// </remarks>
    [HttpGet]
    [Route("upcoming")]
    [ProducesResponseType(typeof(IList<ScheduledMission>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ScheduledMission>> GetUpcomingScheduledMissions()
    {
        var upcomingScheduledMissions = await _scheduledMissionService.ReadByStatus(ScheduledMissionStatus.Pending);
        return Ok(upcomingScheduledMissions);
    }

    /// <summary>
    /// Create and add new scheduled mission to database
    /// </summary>
    /// <remarks>
    /// <para> This query creates a new scheduled mission and adds it to the database </para>
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(ScheduledMission), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ScheduledMission>> PostScheduledMission(
        [FromBody] ScheduledMissionQuery scheduledMissionQuery
    )
    {
        var robot = await _robotService.ReadById(scheduledMissionQuery.RobotId);
        if (robot is null)
            return NotFound($"Could not find robot with id {scheduledMissionQuery.RobotId}");

        var scheduledMission = new ScheduledMission
        {
            Robot = robot,
            EchoMissionId = scheduledMissionQuery.EchoMissionId
        };
        if (scheduledMissionQuery.StartTime is not null)
        {
            scheduledMission.StartTime = (DateTimeOffset)scheduledMissionQuery.StartTime;
        }
        var newScheduledMission = await _scheduledMissionService.Create(scheduledMission);
        return CreatedAtAction(
            nameof(GetScheduledMissionById),
            new { id = newScheduledMission.Id },
            newScheduledMission
        );
    }

    /// <summary>
    /// Deletes the scheduled mission with the specified id from the database.
    /// </summary>
    /// <remarks>
    /// <para> Deletes the scheduled mission with the specified id from the database </para>
    /// </remarks>
    [HttpDelete]
    [Route("{id}")]
    [ProducesResponseType(typeof(ScheduledMission), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ScheduledMission>> DeleteScheduledMission([FromRoute] string id)
    {
        var scheduledMission = await _scheduledMissionService.Delete(id);
        if (scheduledMission is null)
            return NotFound($"Scheduled mission with id {id} not found");
        return Ok(scheduledMission);
    }
}
