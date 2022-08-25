using System.Text.Json;
using Api.Controllers.Models;
using Api.Database.Models;
using Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("missions")]
public class MissionController : ControllerBase
{
    private readonly IMissionService _missionService;
    private readonly IRobotService _robotService;
    private readonly IEchoService _echoService;
    private readonly ILogger<MissionController> _logger;

    public MissionController(
        IMissionService missionService,
        IRobotService robotService,
        IEchoService echoService,
        ILogger<MissionController> logger
    )
    {
        _missionService = missionService;
        _robotService = robotService;
        _echoService = echoService;
        _logger = logger;
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
        var robot = await _robotService.ReadById(scheduledMissionQuery.RobotId);
        if (robot is null)
            return NotFound($"Could not find robot with id {scheduledMissionQuery.RobotId}");

        EchoMission? echoMission;
        try
        {
            echoMission = await _echoService.GetMissionById(scheduledMissionQuery.EchoMissionId);
        }
        catch (HttpRequestException e)
        {
            if (e.StatusCode.HasValue && (int)e.StatusCode.Value == 404)
            {
                _logger.LogWarning(
                    "Could not find echo mission with id={id}",
                    scheduledMissionQuery.EchoMissionId
                );
                return NotFound("Echo mission not found");
            }

            _logger.LogError(e, "Error getting mission from Echo");
            return StatusCode(StatusCodes.Status502BadGateway, $"{e.Message}");
        }
        catch (JsonException e)
        {
            string message = "Error deserializing mission from Echo";
            _logger.LogError(e, "{message}", message);
            return StatusCode(StatusCodes.Status500InternalServerError, message);
        }

        var plannedTasks = echoMission.Tags.Select(t => new PlannedTask(t)).ToList();

        var scheduledMission = new Mission
        {
            Robot = robot,
            EchoMissionId = scheduledMissionQuery.EchoMissionId,
            MissionStatus = MissionStatus.Pending,
            StartTime = scheduledMissionQuery.StartTime,
            PlannedTasks = plannedTasks,
            Tasks = new List<IsarTask>()
        };

        var newMission = await _missionService.Create(scheduledMission);

        return CreatedAtAction(nameof(GetMissionById), new { id = newMission.Id }, newMission);
    }

    /// <summary>
    /// Deletes the mission with the specified id from the database.
    /// </summary>
    /// <remarks>
    /// <para> Deletes the mission with the specified id from the database </para>
    /// </remarks>
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
