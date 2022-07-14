using System.Text.Json;
using Api.Controllers.Models;
using Api.Database.Models;
using Api.Services;
using Api.Utilities;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("robots")]
public class RobotController : ControllerBase
{
    private readonly ILogger<RobotController> _logger;
    private readonly IRobotService _robotService;
    private readonly IIsarService _isarService;
    private readonly IEchoService _echoService;

    public RobotController(
        ILogger<RobotController> logger,
        IRobotService robotService,
        IIsarService isarService,
        IEchoService echoService
    )
    {
        _logger = logger;
        _robotService = robotService;
        _isarService = isarService;
        _echoService = echoService;
    }

    /// <summary>
    /// List all robots on the asset.
    /// </summary>
    /// <remarks>
    /// <para> This query gets all robots (paginated) </para>
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(IList<Robot>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IList<Robot>>> GetRobots()
    {
        _logger.LogInformation("Getting robots from database");
        try
        {
            var robots = await _robotService.ReadAll();
            _logger.LogInformation("Successful GET of robots from database");
            return Ok(robots);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error during GET of robots  from database");
            throw;
        }
    }

    /// <summary>
    /// Gets the robot with the specified id
    /// </summary>
    /// <remarks>
    /// <para> This query gets the robot with the specified id </para>
    /// </remarks>
    [HttpGet]
    [Route("{id}")]
    [ProducesResponseType(typeof(Robot), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Robot>> GetRobotById([FromRoute] string id)
    {
        _logger.LogInformation("Getting robot with id={id}", id);
        try
        {
            var robot = await _robotService.ReadById(id);
            if (robot == null)
            {
                _logger.LogWarning("Could not find robot with id={id}", id);
                return NotFound();
            }

            _logger.LogInformation("Successful GET of robot with id={id}", id);
            return Ok(robot);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error during GET of robot with id={id}", id);
            throw;
        }
    }

    /// <summary>
    /// Create robot and add to database
    /// </summary>
    /// <remarks>
    /// <para> This query creates a robot and adds it to the database </para>
    /// </remarks>
    /// <returns> Robot </returns>
    [HttpPost]
    [ProducesResponseType(typeof(Robot), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Robot>> PostRobot([FromBody] Robot robot)
    {
        _logger.LogInformation("Creating new robot");
        try
        {
            var newRobot = await _robotService.Create(robot);
            _logger.LogInformation("Succesfully created new robot");
            return CreatedAtAction(nameof(GetRobotById), new { id = newRobot.Id }, newRobot);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while creating new robot");
            throw;
        }
    }

    /// <summary>
    /// Updates a robot in the database
    /// </summary>
    /// <remarks>
    /// </remarks>
    /// <returns> Updated robot </returns>
    /// <response code="200"> The robot was succesfully updated </response>
    /// <response code="400"> The robot data is invalid </response>
    /// <response code="404"> There was no robot with the given ID in the database </response>
    [HttpPut]
    [Route("{id}")]
    [ProducesResponseType(typeof(Robot), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Robot>> UpdateRobot(
        [FromRoute] string id,
        [FromBody] Robot robot
    )
    {
        _logger.LogInformation("Updating robot with id={id}", id);

        if (!ModelState.IsValid)
            return BadRequest("Invalid data.");

        if (id != robot.Id)
        {
            _logger.LogWarning("Id: {id} not corresponding to updated robot", id);
            return BadRequest("Inconsistent Id");
        }

        try
        {
            var updatedRobot = await _robotService.Update(robot);

            _logger.LogInformation("Successful PUT of robot to database");

            return Ok(updatedRobot);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while updating robot with id={id}", id);
            throw;
        }
    }

    /// <summary>
    /// Start the echo mission with the corresponding 'missionId' for the robot with id 'robotId'
    /// </summary>
    /// <remarks>
    /// <para> This query starts a mission for a given robot and creates a report </para>
    /// </remarks>
    [HttpPost]
    [Route("{robotId}/start/{echoMissionId}")]
    [ProducesResponseType(typeof(Report), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Report>> StartMission(
        [FromRoute] string robotId,
        [FromRoute] int missionId
    )
    {
        var robot = await _robotService.ReadById(robotId);
        if (robot == null)
        {
            _logger.LogWarning("Could not find robot with id={id}", robotId);
            return NotFound("Robot not found");
        }

        if (robot.Status is not RobotStatus.Available)
        {
            _logger.LogWarning("Robot '{id}' is not available ({status})", missionId, robot.Status.ToString());
            return Conflict($"The Robot is not available ({robot.Status})");
        }

        EchoMission? echoMission;
        try
        {
            echoMission = await _echoService.GetMissionById(missionId);
        }
        catch (HttpRequestException e)
        {
            if (e.StatusCode.HasValue && (int)e.StatusCode.Value == 404)
            {
                _logger.LogWarning("Could not find echo mission with id={id}", missionId);
                return NotFound("Echo mission not found");
            }

            _logger.LogError(e, "Error getting mission from Echo");
            return new StatusCodeResult(StatusCodes.Status502BadGateway);
        }
        catch (JsonException e)
        {
            _logger.LogError(e, "Error deserializing mission from Echo");
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }

        try
        {
            var report = await _isarService.StartMission(robot, new IsarMissionDefinition(echoMission));
            robot.Status = RobotStatus.Busy;
            await _robotService.Update(robot);
            return Ok(report);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while starting isar mission");
            throw;
        }
    }

    /// <summary>
    /// Stop robot
    /// </summary>
    /// <remarks>
    /// <para> This query stops a robot based on id </para>
    /// </remarks>
    [HttpPost]
    [Route("{robotId}/stop/")]
    [ProducesResponseType(typeof(IsarStopMissionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IsarStopMissionResponse>> StopMission([FromRoute] string robotId)
    {
        var robot = await _robotService.ReadById(robotId);
        if (robot == null)
        {
            _logger.LogWarning("Could not find robot with id={id}", robotId);
            return NotFound();
        }
        var response = await _isarService.StopMission(robot);
        if (!response.IsSuccessStatusCode || response.Content is null)
        {
            _logger.LogError("Could not stop mission on robot: {robotId}", robotId);

            int statusCode = (int)response.StatusCode;

            // If error is caused by user (400 codes), let them know
            if (statusCode is >= 400 and < 500)
                return new StatusCodeResult(statusCode);

            return new StatusCodeResult(StatusCodes.Status502BadGateway);
        }

        string? responseContent = await response.Content.ReadAsStringAsync();
        var isarResponse = JsonSerializer.Deserialize<IsarStopMissionResponse>(responseContent);
        return Ok(isarResponse);
    }

    /// <summary>
    /// Get video streams for a given robot
    /// </summary>
    /// <remarks>
    /// <para> Retrieves the video streams available for the given robot </para>
    /// </remarks>
    [HttpGet]
    [Route("{robotId}/video-streams/")]
    [ProducesResponseType(typeof(IList<VideoStream>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IList<VideoStream>>> GetVideoStreams([FromRoute] string robotId)
    {
        var robot = await _robotService.ReadById(robotId);
        if (robot == null)
        {
            _logger.LogWarning("Could not find robot with id={id}", robotId);
            return NotFound();
        }

        return Ok(robot.VideoStreams);
    }

    /// <summary>
    /// Add a video stream to a given robot
    /// </summary>
    /// <remarks>
    /// <para> Adds a provided video stream to the given robot </para>
    /// </remarks>
    /// <returns> The updated robot </returns>
    [HttpPost]
    [Route("{robotId}/video-streams/")]
    [ProducesResponseType(typeof(Robot), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Robot>> CreateVideoStream(
        [FromRoute] string robotId,
        [FromBody] VideoStream videoStream
    )
    {
        var robot = await _robotService.ReadById(robotId);
        if (robot == null)
        {
            _logger.LogWarning("Could not find robot with id={id}", robotId);
            return NotFound();
        }

        if (robot.VideoStreams is null)
            robot.VideoStreams = new List<VideoStream>();

        // These will be autogenerated
        videoStream.Id = null;
        videoStream.RobotId = null;

        robot.VideoStreams.Add(videoStream);

        try
        {
            var updatedRobot = await _robotService.Update(robot);

            return CreatedAtAction(
                nameof(GetVideoStreams),
                new { robotId = updatedRobot.Id },
                updatedRobot
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding video stream to robot");
            throw;
        }
    }

    /// <summary>
    /// Pause current mission
    /// </summary>
    /// <remarks>
    /// <para> This query pauses the currently executing mission for a robot </para>
    /// </remarks>
    [HttpPost]
    [Route("{robotId}/pause/")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<string>> PauseMission([FromRoute] string robotId)
    {
        var robot = await _robotService.ReadById(robotId);
        if (robot == null)
        {
            _logger.LogWarning("Could not find robot with id={id}", robotId);
            return NotFound();
        }
        try
        {
            var response = await _isarService.PauseMission(robot);
            string? responseContent = await response.Content.ReadAsStringAsync();
            string? isarResponse = JsonSerializer.Deserialize<string>(responseContent);
            return Ok(isarResponse);
        }
        catch (MissionException e)
        {
            _logger.LogError(e, "Error while pausing isar mission");
            return StatusCode(StatusCodes.Status502BadGateway, $"{e.Message}");
        }
    }

    /// <summary>
    /// Resume paused mission
    /// </summary>
    /// <remarks>
    /// <para> This query resumes the currently paused mission for a robot </para>
    /// </remarks>
    [HttpPost]
    [Route("{robotId}/resume/")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<string>> ResumeMission([FromRoute] string robotId)
    {
        var robot = await _robotService.ReadById(robotId);
        if (robot == null)
        {
            _logger.LogWarning("Could not find robot with id={id}", robotId);
            return NotFound();
        }
        try
        {
            var response = await _isarService.ResumeMission(robot);
            string? responseContent = await response.Content.ReadAsStringAsync();
            string? isarResponse = JsonSerializer.Deserialize<string>(responseContent);
            return Ok(isarResponse);
        }
        catch (MissionException e)
        {
            _logger.LogError(e, "Error while resuming isar mission");
            return StatusCode(StatusCodes.Status502BadGateway, $"{e.Message}");
        }
    }
}
