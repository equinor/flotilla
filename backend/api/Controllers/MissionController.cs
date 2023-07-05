using System.Globalization;
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
    private readonly IMissionDefinitionService _missionDefinitionService;
    private readonly IMissionRunService _missionRunService;
    private readonly IAreaService _areaService;
    private readonly IRobotService _robotService;
    private readonly IEchoService _echoService;
    private readonly ICustomMissionService _customMissionService;
    private readonly ILogger<MissionController> _logger;
    private readonly IStidService _stidService;
    private readonly IMapService _mapService;

    public MissionController(
        IMissionDefinitionService missionDefinitionService,
        IMissionRunService missionRunService,
        IAreaService areaService,
        IRobotService robotService,
        IEchoService echoService,
        ICustomMissionService customMissionService,
        ILogger<MissionController> logger,
        IMapService mapService,
        IStidService stidService
    )
    {
        _missionDefinitionService = missionDefinitionService;
        _missionRunService = missionRunService;
        _areaService = areaService;
        _robotService = robotService;
        _echoService = echoService;
        _customMissionService = customMissionService;
        _mapService = mapService;
        _stidService = stidService;
        _logger = logger;
    }

    /// <summary>
    /// List all mission runs in the Flotilla database
    /// </summary>
    /// <remarks>
    /// <para> This query gets all mission runs </para>
    /// </remarks>
    [HttpGet("runs")]
    [Authorize(Roles = Role.Any)]
    [ProducesResponseType(typeof(IList<MissionRun>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IList<MissionRun>>> GetMissionRuns(
        [FromQuery] MissionRunQueryStringParameters parameters
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

        PagedList<MissionRun> missionRuns;
        try
        {
            missionRuns = await _missionRunService.ReadAll(parameters);
        }
        catch (InvalidDataException e)
        {
            _logger.LogError(e.Message);
            return BadRequest(e.Message);
        }

        var metadata = new
        {
            missionRuns.TotalCount,
            missionRuns.PageSize,
            missionRuns.CurrentPage,
            missionRuns.TotalPages,
            missionRuns.HasNext,
            missionRuns.HasPrevious
        };

        Response.Headers.Add(
            QueryStringParameters.PaginationHeader,
            JsonSerializer.Serialize(metadata)
        );

        return Ok(missionRuns);
    }

    /// <summary>
    /// List all mission definitions in the Flotilla database
    /// </summary>
    /// <remarks>
    /// <para> This query gets all mission definitions </para>
    /// </remarks>
    [HttpGet("definitions")]
    [Authorize(Roles = Role.Any)]
    [ProducesResponseType(typeof(IList<MissionDefinition>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IList<MissionDefinition>>> GetMissionDefinitions(
        [FromQuery] MissionDefinitionQueryStringParameters parameters
    )
    {
        PagedList<MissionDefinition> missionDefinitions;
        try
        {
            missionDefinitions = await _missionDefinitionService.ReadAll(parameters);
        }
        catch (InvalidDataException e)
        {
            _logger.LogError(e.Message);
            return BadRequest(e.Message);
        }

        var metadata = new
        {
            missionDefinitions.TotalCount,
            missionDefinitions.PageSize,
            missionDefinitions.CurrentPage,
            missionDefinitions.TotalPages,
            missionDefinitions.HasNext,
            missionDefinitions.HasPrevious
        };

        Response.Headers.Add(
            QueryStringParameters.PaginationHeader,
            JsonSerializer.Serialize(metadata)
        );

        return Ok(missionDefinitions);
    }

    /// <summary>
    /// Lookup mission run by specified id.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = Role.Any)]
    [Route("runs/{id}")]
    [ProducesResponseType(typeof(MissionRun), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<MissionRun>> GetMissionRunById([FromRoute] string id)
    {
        var missioRun = await _missionRunService.ReadById(id);
        if (missioRun == null)
            return NotFound($"Could not find mission run with id {id}");
        return Ok(missioRun);
    }

    /// <summary>
    /// Lookup mission definition by specified id.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = Role.Any)]
    [Route("definitions/{id}")]
    [ProducesResponseType(typeof(MissionRun), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<MissionDefinition>> GetMissionDefinitionById([FromRoute] string id)
    {
        var missionDefinition = await _missionDefinitionService.ReadById(id);
        if (missionDefinition == null)
            return NotFound($"Could not find mission definition with id {id}");
        return Ok(missionDefinition);
    }

    /// <summary>
    /// Get map for mission with specified id.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = Role.Any)]
    [Route("{assetCode}/{mapName}/map")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<byte[]>> GetMap([FromRoute] string assetCode, string mapName)
    {
        try
        {
            byte[] mapStream = await _mapService.FetchMapImage(mapName, assetCode);
            return File(mapStream, "image/png");
        }
        catch (Azure.RequestFailedException)
        {
            return NotFound("Could not find map for this area.");
        }
    }

    /// <summary>
    /// Schedule an existing mission definition
    /// </summary>
    /// <remarks>
    /// <para> This query schedules an existing mission and adds it to the database </para>
    /// </remarks>
    [HttpPost("schedule")]
    [Authorize(Roles = Role.User)]
    [ProducesResponseType(typeof(MissionRun), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<MissionRun>> Schedule(
        [FromBody] ScheduleMissionQuery scheduledMissionQuery
    )
    {
        var robot = await _robotService.ReadById(scheduledMissionQuery.RobotId);
        if (robot is null)
            return NotFound($"Could not find robot with id {scheduledMissionQuery.RobotId}");

        var missionDefinition = await _missionDefinitionService.ReadById(scheduledMissionQuery.MissionDefinitionId);
        if (missionDefinition == null)
            return NotFound("Mission definition not found");

        List<MissionTask>? missionTasks;
        missionTasks = missionDefinition.Source.Type switch
        {
            MissionSourceType.Echo =>
                // CultureInfo is not important here since we are not using decimal points
                missionTasks = _echoService.GetMissionById(
                        int.Parse(missionDefinition.Source.SourceId, new CultureInfo("en-US"))
                    ).Result.Tags
                    .Select(
                        t =>
                        {
                            var tagPosition = _stidService
                                .GetTagPosition(t.TagId, missionDefinition.AssetCode)
                                .Result;
                            return new MissionTask(t, tagPosition);
                        }
                    )
                    .ToList(),
            MissionSourceType.Custom =>
                missionTasks = await _customMissionService.GetMissionTasksFromMissionId(missionDefinition.Source.SourceId),
            _ =>
                throw new MissionSourceTypeException($"Mission type {missionDefinition.Source.Type} is not accounted for")
        };

        if (missionTasks == null)
            return NotFound("No mission tasks were found for the requested mission");

        var missionRun = new MissionRun
        {
            Name = missionDefinition.Name,
            Robot = robot,
            MissionId = missionDefinition.Id,
            Status = MissionStatus.Pending,
            DesiredStartTime = scheduledMissionQuery.DesiredStartTime,
            Tasks = missionTasks,
            AssetCode = missionDefinition.AssetCode,
            Area = missionDefinition.Area,
            MapMetadata = new MapMetadata()
        };

        await _mapService.AssignMapToMission(missionRun);

        if (missionRun.Tasks.Any())
            missionRun.CalculateEstimatedDuration();

        var newMissionRun = await _missionRunService.Create(missionRun);

        return CreatedAtAction(nameof(GetMissionRunById), new { id = newMissionRun.Id }, newMissionRun);
    }

    /// <summary>
    /// Schedule a new echo mission
    /// </summary>
    /// <remarks>
    /// <para> This query schedules a new echo mission and adds it to the database </para>
    /// </remarks>
    [HttpPost]
    [Authorize(Roles = Role.User)]
    [ProducesResponseType(typeof(MissionRun), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<MissionRun>> Create(
        [FromBody] ScheduledMissionQuery scheduledMissionQuery
    )
    {
        // TODO: once we have a good way of creating mission definitions for echo missions,
        //       we can delete this endpoint
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

        var area = await _areaService.ReadByAssetAndName(scheduledMissionQuery.AssetCode, scheduledMissionQuery.AreaName);

        if (area == null)
        {
            // This is disabled for now as the Area database is not yet populated
            //return NotFound($"Could not find area with name {scheduledMissionQuery.AreaName} in asset {scheduledMissionQuery.AssetCode}");
        }

        // TODO: search for if a source with the given type and URL exists, then reuse it

        var scheduledMissionDefinition = new MissionDefinition
        {
            Id = Guid.NewGuid().ToString(),
            Source = new Source
            {
                SourceId = $"{echoMission.Id}",
                Type = MissionSourceType.Echo
            },
            Name = echoMission.Name,
            InspectionFrequency = scheduledMissionQuery.InspectionFrequency,
            AssetCode = scheduledMissionQuery.AssetCode,
            Area = area
        };

        var missionRun = new MissionRun
        {
            Name = echoMission.Name,
            Robot = robot,
            MissionId = scheduledMissionDefinition.Id,
            Status = MissionStatus.Pending,
            DesiredStartTime = scheduledMissionQuery.DesiredStartTime,
            Tasks = missionTasks,
            AssetCode = scheduledMissionQuery.AssetCode,
            Area = area,
            MapMetadata = new MapMetadata()
        };

        await _mapService.AssignMapToMission(missionRun);

        if (missionRun.Tasks.Any())
            missionRun.CalculateEstimatedDuration();

        var newMissionDefinition = await _missionDefinitionService.Create(scheduledMissionDefinition);

        var newMissionRun = await _missionRunService.Create(missionRun);

        return CreatedAtAction(nameof(GetMissionRunById), new { id = newMissionRun.Id }, newMissionRun);
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
    [ProducesResponseType(typeof(MissionRun), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<MissionRun>> Create(
        [FromBody] CustomMissionQuery customMissionQuery
    )
    {
        var robot = await _robotService.ReadById(customMissionQuery.RobotId);
        if (robot is null)
            return NotFound($"Could not find robot with id {customMissionQuery.RobotId}");

        var missionTasks = customMissionQuery.Tasks.Select(task => new MissionTask(task)).ToList();

        var area = await _areaService.ReadByAssetAndName(customMissionQuery.AssetCode, customMissionQuery.AreaName);

        if (area == null)
            return NotFound($"Could not find area with name {customMissionQuery.AreaName} in asset {customMissionQuery.AssetCode}");

        string sourceURL = await _customMissionService.UploadSource(missionTasks);

        var customMissionDefinition = new MissionDefinition
        {
            Source = new Source
            {
                SourceId = sourceURL.ToString(),
                Type = MissionSourceType.Echo
            },
            Name = customMissionQuery.Name,
            InspectionFrequency = customMissionQuery.InspectionFrequency,
            AssetCode = customMissionQuery.AssetCode,
            Area = area
        };

        var scheduledMission = new MissionRun
        {
            Name = customMissionQuery.Name,
            Description = customMissionQuery.Description,
            MissionId = customMissionDefinition.Id,
            Comment = customMissionQuery.Comment,
            Robot = robot,
            Status = MissionStatus.Pending,
            DesiredStartTime = customMissionQuery.DesiredStartTime ?? DateTimeOffset.UtcNow,
            Tasks = missionTasks,
            AssetCode = customMissionQuery.AssetCode,
            Area = area,
            MapMetadata = new MapMetadata()
        };

        await _mapService.AssignMapToMission(scheduledMission);

        if (scheduledMission.Tasks.Any())
            scheduledMission.CalculateEstimatedDuration();

        var newMissionDefinition = await _missionDefinitionService.Create(customMissionDefinition);

        var newMissionRun = await _missionRunService.Create(scheduledMission);

        return CreatedAtAction(nameof(GetMissionRunById), new { id = newMissionRun.Id }, newMissionRun);
    }

    /// <summary>
    /// Deletes the mission definition with the specified id from the database.
    /// </summary>
    [HttpDelete]
    [Authorize(Roles = Role.Admin)]
    [Route("definitions/{id}")]
    [ProducesResponseType(typeof(MissionDefinition), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<MissionDefinition>> DeleteMissionDefinition([FromRoute] string id)
    {
        var missionDefinition = await _missionDefinitionService.Delete(id);
        if (missionDefinition is null)
            return NotFound($"Mission definition with id {id} not found");
        return Ok(missionDefinition);
    }

    /// <summary>
    /// Deletes the mission run with the specified id from the database.
    /// </summary>
    [HttpDelete]
    [Authorize(Roles = Role.Admin)]
    [Route("runs/{id}")]
    [ProducesResponseType(typeof(MissionRun), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<MissionRun>> DeleteMissionRun([FromRoute] string id)
    {
        var missionRun = await _missionRunService.Delete(id);
        if (missionRun is null)
            return NotFound($"Mission run with id {id} not found");
        return Ok(missionRun);
    }
}
