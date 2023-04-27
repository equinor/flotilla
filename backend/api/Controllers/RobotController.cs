using System.Text.Json;
using Api.Controllers.Models;
using Api.Database.Models;
using Api.Services;
using Api.Services.Models;
using Api.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("robots")]
public class RobotController : ControllerBase
{
    private readonly ILogger<RobotController> _logger;
    private readonly IRobotService _robotService;
    private readonly IIsarService _isarService;
    private readonly IMissionService _missionService;
    private readonly IRobotModelService _robotModelService;

    public RobotController(
        ILogger<RobotController> logger,
        IRobotService robotService,
        IIsarService isarService,
        IMissionService missionService,
        IRobotModelService robotModelService
    )
    {
        _logger = logger;
        _robotService = robotService;
        _isarService = isarService;
        _missionService = missionService;
        _robotModelService = robotModelService;
    }

    /// <summary>
    /// List all robots on the asset.
    /// </summary>
    /// <remarks>
    /// <para> This query gets all robots </para>
    /// </remarks>
    [HttpGet]
    [Authorize(Roles = Role.Any)]
    [ProducesResponseType(typeof(IList<Robot>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IList<Robot>>> GetRobots()
    {
        try
        {
            var robots = await _robotService.ReadAll();
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
    [Authorize(Roles = Role.Any)]
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
    [HttpPost]
    [Authorize(Roles = Role.Admin)]
    [ProducesResponseType(typeof(Robot), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Robot>> CreateRobot([FromBody] CreateRobotQuery robotQuery)
    {
        _logger.LogInformation("Creating new robot");
        try
        {
            var robotModel = await _robotModelService.ReadByRobotType(robotQuery.RobotType);
            if (robotModel == null)
                return BadRequest(
                    $"No robot model exists with robot type '{robotQuery.RobotType}'"
                );

            var robot = new Robot(robotQuery) { Model = robotModel };

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
    /// <response code="200"> The robot was successfully updated </response>
    /// <response code="400"> The robot data is invalid </response>
    /// <response code="404"> There was no robot with the given ID in the database </response>
    [HttpPut]
    [Authorize(Roles = Role.Admin)]
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
    /// Deletes the robot with the specified id from the database.
    /// </summary>
    [HttpDelete]
    [Authorize(Roles = Role.Admin)]
    [Route("{id}")]
    [ProducesResponseType(typeof(Mission), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Robot>> DeleteRobot([FromRoute] string id)
    {
        var robot = await _robotService.Delete(id);
        if (robot is null)
            return NotFound($"Robot with id {id} not found");
        return Ok(robot);
    }

    /// <summary>
    /// Updates a robot's status in the database
    /// </summary>
    /// <remarks>
    /// </remarks>
    /// <response code="200"> The robot status was succesfully updated </response>
    /// <response code="400"> The robot data is invalid </response>
    /// <response code="404"> There was no robot with the given ID in the database </response>
    [HttpPut]
    [Authorize(Roles = Role.Admin)]
    [Route("{id}/status")]
    [ProducesResponseType(typeof(Robot), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Robot>> UpdateRobotStatus(
        [FromRoute] string id,
        [FromBody] RobotStatus robotStatus
    )
    {
        _logger.LogInformation("Updating robot status with id={id}", id);

        if (!ModelState.IsValid)
            return BadRequest("Invalid data.");

        var robot = await _robotService.ReadById(id);
        if (robot == null)
            return NotFound($"No robot with id: {id} could be found");

        robot.Status = robotStatus;
        try
        {
            var updatedRobot = await _robotService.Update(robot);

            _logger.LogInformation("Successful PUT of robot to database");

            return Ok(updatedRobot);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while updating status for robot with id={id}", id);
            throw;
        }
    }

    /// <summary>
    /// Get video streams for a given robot
    /// </summary>
    /// <remarks>
    /// <para> Retrieves the video streams available for the given robot </para>
    /// </remarks>
    [HttpGet]
    [Authorize(Roles = Role.User)]
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
    [HttpPost]
    [Authorize(Roles = Role.Admin)]
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
    /// Start the mission in the database with the corresponding 'missionId' for the robot with id 'robotId'
    /// </summary>
    /// <remarks>
    /// <para> This query starts a mission for a given robot </para>
    /// </remarks>
    [HttpPost]
    [Authorize(Roles = Role.Admin)]
    [Route("{robotId}/start/{missionId}")]
    [ProducesResponseType(typeof(Mission), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Mission>> StartMission(
        [FromRoute] string robotId,
        [FromRoute] string missionId
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
            _logger.LogWarning(
                "Robot '{id}' is not available ({status})",
                robotId,
                robot.Status.ToString()
            );
            return Conflict($"The Robot is not available ({robot.Status})");
        }

        var mission = await _missionService.ReadById(missionId);

        if (mission == null)
        {
            _logger.LogWarning("Could not find mission with id={id}", missionId);
            return NotFound("Mission not found");
        }

        IsarMission isarMission;
        try
        {
            isarMission = await _isarService.StartMission(robot, mission);
        }
        catch (HttpRequestException e)
        {
            string message = $"Could not reach ISAR at {robot.IsarUri}";
            _logger.LogError(e, "{message}", message);
            OnIsarUnavailable(robot);
            return StatusCode(StatusCodes.Status502BadGateway, message);
        }
        catch (MissionException e)
        {
            _logger.LogError(e, "Error while starting ISAR mission");
            return StatusCode(StatusCodes.Status502BadGateway, $"{e.Message}");
        }
        catch (JsonException e)
        {
            string message = "Error while processing of the response from ISAR";
            _logger.LogError(e, "{message}", message);
            return StatusCode(StatusCodes.Status500InternalServerError, message);
        }
        catch (RobotPositionNotFoundException e)
        {
            string message =
                "A suitable robot position could not be found for one or more of the desired tags";
            _logger.LogError(e, "{message}", message);
            return StatusCode(StatusCodes.Status500InternalServerError, message);
        }

        mission.UpdateWithIsarInfo(isarMission);
        mission.Status = MissionStatus.Ongoing;

        await _missionService.Update(mission);

        if (robot.CurrentMissionId != null)
        {
            var orphanedMission = await _missionService.ReadById(robot.CurrentMissionId);
            if (orphanedMission != null)
            {
                orphanedMission.SetToFailed();
                await _missionService.Update(orphanedMission);
            }
        }

        robot.Status = RobotStatus.Busy;
        robot.CurrentMissionId = mission.Id;
        await _robotService.Update(robot);

        return Ok(mission);
    }

    /// <summary>
    /// Stops the current mission on a robot
    /// </summary>
    /// <remarks>
    /// <para> This query stops the current mission for a given robot </para>
    /// </remarks>
    [HttpPost]
    [Authorize(Roles = Role.User)]
    [Route("{robotId}/stop/")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> StopMission([FromRoute] string robotId)
    {
        var robot = await _robotService.ReadById(robotId);
        if (robot == null)
        {
            _logger.LogWarning("Could not find robot with id={id}", robotId);
            return NotFound();
        }

        try
        {
            await _isarService.StopMission(robot);
            robot.CurrentMissionId = null;
            await _robotService.Update(robot);
        }
        catch (HttpRequestException e)
        {
            string message = "Error connecting to ISAR while stopping mission";
            _logger.LogError(e, "{message}", message);
            OnIsarUnavailable(robot);
            return StatusCode(StatusCodes.Status502BadGateway, message);
        }
        catch (MissionException e)
        {
            _logger.LogError(e, "Error while stopping ISAR mission");
            return StatusCode(StatusCodes.Status502BadGateway, $"{e.Message}");
        }
        catch (JsonException e)
        {
            string message = "Error while processing the response from ISAR";
            _logger.LogError(e, "{message}", message);
            return StatusCode(StatusCodes.Status500InternalServerError, message);
        }

        return NoContent();
    }

    /// <summary>
    /// Pause the current mission on a robot
    /// </summary>
    /// <remarks>
    /// <para> This query pauses the current mission for a robot </para>
    /// </remarks>
    [HttpPost]
    [Authorize(Roles = Role.User)]
    [Route("{robotId}/pause/")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> PauseMission([FromRoute] string robotId)
    {
        var robot = await _robotService.ReadById(robotId);
        if (robot == null)
        {
            _logger.LogWarning("Could not find robot with id={id}", robotId);
            return NotFound();
        }

        try
        {
            await _isarService.PauseMission(robot);
        }
        catch (HttpRequestException e)
        {
            string message = "Error connecting to ISAR while pausing mission";
            _logger.LogError(e, "{message}", message);
            OnIsarUnavailable(robot);
            return StatusCode(StatusCodes.Status502BadGateway, message);
        }
        catch (MissionException e)
        {
            _logger.LogError(e, "Error while pausing ISAR mission");
            return StatusCode(StatusCodes.Status502BadGateway, $"{e.Message}");
        }
        catch (JsonException e)
        {
            string message = "Error while processing of the response from ISAR";
            _logger.LogError(e, "{message}", message);
            return StatusCode(StatusCodes.Status500InternalServerError, message);
        }

        return NoContent();
    }

    /// <summary>
    /// Resume paused mission on a robot
    /// </summary>
    /// <remarks>
    /// <para> This query resumes the currently paused mission for a robot </para>
    /// </remarks>
    [HttpPost]
    [Authorize(Roles = Role.User)]
    [Route("{robotId}/resume/")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> ResumeMission([FromRoute] string robotId)
    {
        var robot = await _robotService.ReadById(robotId);
        if (robot == null)
        {
            _logger.LogWarning("Could not find robot with id={id}", robotId);
            return NotFound();
        }

        try
        {
            await _isarService.ResumeMission(robot);
        }
        catch (HttpRequestException e)
        {
            string message = "Error connecting to ISAR while resuming mission";
            _logger.LogError(e, "{message}", message);
            OnIsarUnavailable(robot);
            return StatusCode(StatusCodes.Status502BadGateway, message);
        }
        catch (MissionException e)
        {
            _logger.LogError(e, "Error while resuming ISAR mission");
            return StatusCode(StatusCodes.Status502BadGateway, $"{e.Message}");
        }
        catch (JsonException e)
        {
            string message = "Error while processing of the response from ISAR";
            _logger.LogError(e, "{message}", message);
            return StatusCode(StatusCodes.Status500InternalServerError, message);
        }

        return NoContent();
    }

    /// <summary>
    /// Start a localization mission with localization in the pose 'localizationPose' for the robot with id 'robotId'
    /// </summary>
    /// <remarks>
    /// <para> This query starts a localization for a given robot </para>
    /// </remarks>
    [HttpPost]
    [Authorize(Roles = Role.User)]
    [Route("{robotId}/start-localization")]
    [ProducesResponseType(typeof(Mission), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Mission>> StartLocalizationMission(
        [FromRoute] string robotId,
        [FromBody] Pose localizationPose
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
            _logger.LogWarning(
                "Robot '{id}' is not available ({status})",
                robotId,
                robot.Status.ToString()
            );
            return Conflict($"The Robot is not available ({robot.Status})");
        }

        var mission = new Mission
        {
            Name = "Localization Mission",
            Robot = robot,
            AssetCode = "NA",
            EchoMissionId = 0,
            Status = MissionStatus.Pending,
            DesiredStartTime = DateTimeOffset.UtcNow,
            Tasks = new List<MissionTask>(),
            Map = new MissionMap()
        };

        IsarMission isarMission;
        try
        {
            isarMission = await _isarService.StartLocalizationMission(robot, localizationPose);
        }
        catch (HttpRequestException e)
        {
            string message = $"Could not reach ISAR at {robot.IsarUri}";
            _logger.LogError(e, "{message}", message);
            OnIsarUnavailable(robot);
            return StatusCode(StatusCodes.Status502BadGateway, message);
        }
        catch (MissionException e)
        {
            _logger.LogError(e, "Error while starting ISAR localization mission");
            return StatusCode(StatusCodes.Status502BadGateway, $"{e.Message}");
        }
        catch (JsonException e)
        {
            string message = "Error while processing of the response from ISAR";
            _logger.LogError(e, "{message}", message);
            return StatusCode(StatusCodes.Status500InternalServerError, message);
        }

        mission.UpdateWithIsarInfo(isarMission);
        mission.Status = MissionStatus.Ongoing;

        await _missionService.Create(mission);

        robot.Status = RobotStatus.Busy;
        robot.CurrentMissionId = mission.Id;
        await _robotService.Update(robot);
        return Ok(mission);
    }

    private async void OnIsarUnavailable(Robot robot)
    {
        robot.Enabled = false;
        robot.Status = RobotStatus.Offline;
        if (robot.CurrentMissionId != null)
        {
            var mission = await _missionService.ReadById(robot.CurrentMissionId);
            if (mission != null)
            {
                mission.SetToFailed();
                await _missionService.Update(mission);
                _logger.LogWarning(
                    "Mission '{id}' failed because ISAR could not be reached",
                    mission.Id
                );
            }
        }
        robot.CurrentMissionId = null;
        await _robotService.Update(robot);
    }
}
