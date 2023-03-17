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
    private readonly IRobotService _robotService;
    private readonly IEchoService _echoService;
    private readonly ILogger<MissionController> _logger;
    private readonly IStidService _stidService;
    private readonly IMapService _mapService;

    public MissionController(
        IMissionService missionService,
        IRobotService robotService,
        IEchoService echoService,
        ILogger<MissionController> logger,
        IMapService mapService,
        IStidService stidService
    )
    {
        _missionService = missionService;
        _robotService = robotService;
        _echoService = echoService;
        _mapService = mapService;
        _stidService = stidService;
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
            byte[] mapStream = await _mapService.FetchMapImage(id);
            return File(mapStream, "image/png");
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
        catch (InvalidDataException e)
        {
            string message =
                "Can not schedule mission because EchoMission is invalid. One or more tasks does not contain a robot pose.";
            _logger.LogError(e, message);
            return StatusCode(StatusCodes.Status502BadGateway, message);
        }

        var plannedTasks = echoMission.Tags
            .Select(
                t =>
                {
                    var tagPosition = _stidService
                        .GetTagPosition(t.TagId, scheduledMissionQuery.AssetCode)
                        .Result;
                    return new PlannedTask(t, tagPosition);
                }
            )
            .ToList();

        var map = await _mapService.AssignMapToMission(echoMission.AssetCode, plannedTasks);

        var scheduledMission = new Mission
        {
            Name = echoMission.Name,
            Robot = robot,
            EchoMissionId = scheduledMissionQuery.EchoMissionId,
            MissionStatus = MissionStatus.Pending,
            DesiredStartTime = scheduledMissionQuery.DesiredStartTime,
            PlannedTasks = plannedTasks,
            Tasks = new List<IsarTask>(),
            AssetCode = scheduledMissionQuery.AssetCode,
            Map = map
        };

        if (plannedTasks.Any())
            scheduledMission.CalculateEstimatedDuration();

        var newMission = await _missionService.Create(scheduledMission);

        return CreatedAtAction(nameof(GetMissionById), new { id = newMission.Id }, newMission);
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
