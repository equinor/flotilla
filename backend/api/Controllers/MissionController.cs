using System.Text.Json;
using Api.Controllers.Models;
using Api.Database.Models;
using Api.Services;
using Api.Utilities;
using Microsoft.AspNetCore.Authorization;
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
    [Authorize(Roles = Role.Any)]
    [ProducesResponseType(typeof(IList<Mission>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IList<Mission>>> GetMissions(
        [FromQuery] MissionQueryStringParameters parameters
    )
    {
        if (parameters.MaxDesiredStartTime < parameters.MinDesiredStartTime)
        {
            return BadRequest("Max DesiredStartTime cannot be less than min DesiredStartTime");
        }
        if (parameters.MaxStartTime < parameters.MinStartTime)
        {
            return BadRequest("Max StartTime cannot be less than min StartTime");
        }
        if (parameters.MaxEndTime < parameters.MinEndTime)
        {
            return BadRequest("Max EndTime cannot be less than min EndTime");
        }

        PagedList<Mission> missions;
        try
        {
            missions = await _missionService.ReadAll(parameters);
        }
        catch (InvalidDataException e)
        {
            _logger.LogError(e.Message);
            return BadRequest(e.Message);
        }

        var metadata = new
        {
            missions.TotalCount,
            missions.PageSize,
            missions.CurrentPage,
            missions.TotalPages,
            missions.HasNext,
            missions.HasPrevious
        };

        Response.Headers.Add(
            QueryStringParameters.PaginationHeader,
            JsonSerializer.Serialize(metadata)
        );

        return Ok(missions);
    }

    /// <summary>
    /// Lookup mission by specified id.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = Role.Any)]
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
    [Authorize(Roles = Role.Any)]
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
        var mission = await _missionService.ReadById(id);
        if (mission is null)
        {
            _logger.LogError("Mission not found for mission ID {missionId}", id);
            throw new MissionNotFoundException("Mission not found");
        }

        try
        {
            byte[] mapStream = await _mapService.FetchMapImage(mission);
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
    [Authorize(Roles = Role.User)]
    [ProducesResponseType(typeof(Mission), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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

        var missionTasks = echoMission.Tags
            .Select(
                t =>
                {
                    var tagPosition = _stidService
                        .GetTagPosition(t.TagId, scheduledMissionQuery.AssetCode)
                        .Result;
                    return new MissionTask(t, tagPosition);
                }
            )
            .ToList();

        var scheduledMission = new Mission
        {
            Name = echoMission.Name,
            Robot = robot,
            EchoMissionId = scheduledMissionQuery.EchoMissionId,
            Status = MissionStatus.Pending,
            DesiredStartTime = scheduledMissionQuery.DesiredStartTime,
            Tasks = missionTasks,
            AssetCode = scheduledMissionQuery.AssetCode,
            Map = new MissionMap()
        };

        await _mapService.AssignMapToMission(scheduledMission);

        if (scheduledMission.Tasks.Any())
            scheduledMission.CalculateEstimatedDuration();

        var newMission = await _missionService.Create(scheduledMission);

        return CreatedAtAction(nameof(GetMissionById), new { id = newMission.Id }, newMission);
    }

    /// <summary>
    /// Schedule a custom mission
    /// </summary>
    /// <remarks>
    /// <para> This query schedules a custom mission defined in the incoming json </para>
    /// </remarks>
    [HttpPost]
    [Authorize(Roles = Role.User)]
    [Route("custom")]
    [ProducesResponseType(typeof(Mission), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Mission>> Create(
        [FromBody] CustomMissionQuery customMissionQuery
    )
    {
        var robot = await _robotService.ReadById(customMissionQuery.RobotId);
        if (robot is null)
            return NotFound($"Could not find robot with id {customMissionQuery.RobotId}");

        var missionTasks = customMissionQuery.Tasks.Select(task => new MissionTask(task)).ToList();

        var scheduledMission = new Mission
        {
            Name = customMissionQuery.Name,
            Description = customMissionQuery.Description,
            Comment = customMissionQuery.Comment,
            Robot = robot,
            EchoMissionId = 0,
            Status = MissionStatus.Pending,
            DesiredStartTime = customMissionQuery.DesiredStartTime ?? DateTimeOffset.UtcNow,
            Tasks = missionTasks,
            AssetCode = customMissionQuery.AssetCode,
            Map = new MissionMap()
        };

        await _mapService.AssignMapToMission(scheduledMission);

        if (scheduledMission.Tasks.Any())
            scheduledMission.CalculateEstimatedDuration();

        var newMission = await _missionService.Create(scheduledMission);

        return CreatedAtAction(nameof(GetMissionById), new { id = newMission.Id }, newMission);
    }

    /// <summary>
    /// Deletes the mission with the specified id from the database.
    /// </summary>
    [HttpDelete]
    [Authorize(Roles = Role.Admin)]
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
