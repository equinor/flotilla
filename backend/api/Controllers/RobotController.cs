using System.Text.Json;
using Api.Controllers.Models;
using Api.Database.Models;
using Api.Services;
using Api.Services.Models;
using Api.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Api.Controllers
{
    [ApiController]
    [Route("robots")]
    public class RobotController : ControllerBase
    {
        private readonly IAreaService _areaService;
        private readonly IIsarService _isarService;
        private readonly ILogger<RobotController> _logger;
        private readonly IMissionRunService _missionRunService;
        private readonly IRobotModelService _robotModelService;
        private readonly IRobotService _robotService;

        public RobotController(
            ILogger<RobotController> logger,
            IRobotService robotService,
            IIsarService isarService,
            IMissionRunService missionRunService,
            IRobotModelService robotModelService,
            IAreaService areaService
        )
        {
            _logger = logger;
            _robotService = robotService;
            _isarService = isarService;
            _missionRunService = missionRunService;
            _robotModelService = robotModelService;
            _areaService = areaService;
        }

        /// <summary>
        ///     List all robots on the installation.
        /// </summary>
        /// <remarks>
        ///     <para> This query gets all robots </para>
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
        ///     Gets the robot with the specified id
        /// </summary>
        /// <remarks>
        ///     <para> This query gets the robot with the specified id </para>
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
            _logger.LogInformation("Getting robot with id={Id}", id);
            try
            {
                var robot = await _robotService.ReadById(id);
                if (robot == null)
                {
                    _logger.LogWarning("Could not find robot with id={Id}", id);
                    return NotFound();
                }

                _logger.LogInformation("Successful GET of robot with id={Id}", id);
                return Ok(robot);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error during GET of robot with id={Id}", id);
                throw;
            }
        }

        /// <summary>
        ///     Create robot and add to database
        /// </summary>
        /// <remarks>
        ///     <para> This query creates a robot and adds it to the database </para>
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
                {
                    return BadRequest(
                        $"No robot model exists with robot type '{robotQuery.RobotType}'"
                    );
                }

                var robot = new Robot(robotQuery)
                {
                    Model = robotModel
                };

                var newRobot = await _robotService.Create(robot);
                _logger.LogInformation("Successfully created new robot");
                return CreatedAtAction(nameof(GetRobotById), new
                {
                    id = newRobot.Id
                }, newRobot);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error while creating new robot");
                throw;
            }
        }

        /// <summary>
        ///     Updates a robot in the database
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
            _logger.LogInformation("Updating robot with id={Id}", id);

            if (!ModelState.IsValid)
            {
                return BadRequest("Invalid data");
            }

            if (id != robot.Id)
            {
                _logger.LogWarning("Id: {Id} not corresponding to updated robot", id);
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
                _logger.LogError(e, "Error while updating robot with id={Id}", id);
                throw;
            }
        }

        /// <summary>
        ///     Deletes the robot with the specified id from the database
        /// </summary>
        [HttpDelete]
        [Authorize(Roles = Role.Admin)]
        [Route("{id}")]
        [ProducesResponseType(typeof(MissionRun), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<Robot>> DeleteRobot([FromRoute] string id)
        {
            var robot = await _robotService.Delete(id);
            if (robot is null)
            {
                return NotFound($"Robot with id {id} not found");
            }
            return Ok(robot);
        }

        /// <summary>
        ///     Updates a robot's status in the database
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <response code="200"> The robot status was successfully updated </response>
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
            _logger.LogInformation("Updating robot status with id={Id}", id);

            if (!ModelState.IsValid)
            {
                return BadRequest("Invalid data");
            }

            var robot = await _robotService.ReadById(id);
            if (robot == null)
            {
                string errorMessage = $"No robot with id: {id} could be found";
                _logger.LogError("{Message}", errorMessage);
                return NotFound(errorMessage);
            }

            robot.Status = robotStatus;
            try
            {
                var updatedRobot = await _robotService.Update(robot);

                _logger.LogInformation("Successful PUT of robot to database");

                return Ok(updatedRobot);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error while updating status for robot with id={Id}", id);
                throw;
            }
        }

        /// <summary>
        ///     Get video streams for a given robot
        /// </summary>
        /// <remarks>
        ///     <para> Retrieves the video streams available for the given robot </para>
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
                _logger.LogWarning("Could not find robot with id={Id}", robotId);
                return NotFound();
            }

            return Ok(robot.VideoStreams);
        }

        /// <summary>
        ///     Add a video stream to a given robot
        /// </summary>
        /// <remarks>
        ///     <para> Adds a provided video stream to the given robot </para>
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
                _logger.LogWarning("Could not find robot with id={Id}", robotId);
                return NotFound();
            }

            robot.VideoStreams.Add(videoStream);

            try
            {
                var updatedRobot = await _robotService.Update(robot);

                return CreatedAtAction(
                    nameof(GetVideoStreams),
                    new
                    {
                        robotId = updatedRobot.Id
                    },
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
        ///     Start the mission in the database with the corresponding 'missionRunId' for the robot with id 'robotId'
        /// </summary>
        /// <remarks>
        ///     <para> This query starts a mission for a given robot </para>
        /// </remarks>
        [HttpPost]
        [Authorize(Roles = Role.Admin)]
        [Route("{robotId}/start/{missionRunId}")]
        [ProducesResponseType(typeof(MissionRun), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<MissionRun>> StartMission(
            [FromRoute] string robotId,
            [FromRoute] string missionRunId
        )
        {
            var robot = await _robotService.ReadById(robotId);
            if (robot == null)
            {
                _logger.LogWarning("Could not find robot with id={Id}", robotId);
                return NotFound("Robot not found");
            }

            if (robot.Status is not RobotStatus.Available)
            {
                _logger.LogWarning(
                    "Robot '{Id}' is not available ({Status})",
                    robotId,
                    robot.Status.ToString()
                );
                return Conflict($"The Robot is not available ({robot.Status})");
            }

            var missionRun = await _missionRunService.ReadById(missionRunId);

            if (missionRun == null)
            {
                _logger.LogWarning("Could not find mission with id={Id}", missionRunId);
                return NotFound("Mission not found");
            }

            IsarMission isarMission;
            try
            {
                isarMission = await _isarService.StartMission(robot, missionRun);
            }
            catch (HttpRequestException e)
            {
                string errorMessage = $"Could not reach ISAR at {robot.IsarUri}";
                _logger.LogError(e, "{Message}", errorMessage);
                OnIsarUnavailable(robot);
                return StatusCode(StatusCodes.Status502BadGateway, errorMessage);
            }
            catch (MissionException e)
            {
                _logger.LogError(e, "Error while starting ISAR mission");
                return StatusCode(StatusCodes.Status502BadGateway, $"{e.Message}");
            }
            catch (JsonException e)
            {
                const string Message = "Error while processing of the response from ISAR";
                _logger.LogError(e, "{Message}", Message);
                return StatusCode(StatusCodes.Status500InternalServerError, Message);
            }
            catch (RobotPositionNotFoundException e)
            {
                const string Message = "A suitable robot position could not be found for one or more of the desired tags";
                _logger.LogError(e, "{Message}", Message);
                return StatusCode(StatusCodes.Status500InternalServerError, Message);
            }

            missionRun.UpdateWithIsarInfo(isarMission);
            missionRun.Status = MissionStatus.Ongoing;

            await _missionRunService.Update(missionRun);

            robot.Status = RobotStatus.Busy;
            robot.CurrentMissionId = missionRun.Id;
            await _robotService.Update(robot);

            return Ok(missionRun);
        }

        /// <summary>
        ///     Stops the current mission on a robot
        /// </summary>
        /// <remarks>
        ///     <para> This query stops the current mission for a given robot </para>
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
                _logger.LogWarning("Could not find robot with id={Id}", robotId);
                return NotFound();
            }

            try { await _isarService.StopMission(robot); }
            catch (HttpRequestException e)
            {
                const string Message = "Error connecting to ISAR while stopping mission";
                _logger.LogError(e, "{Message}", Message);
                OnIsarUnavailable(robot);
                return StatusCode(StatusCodes.Status502BadGateway, Message);
            }
            catch (MissionException e)
            {
                _logger.LogError(e, "Error while stopping ISAR mission");
                return StatusCode(StatusCodes.Status502BadGateway, $"{e.Message}");
            }
            catch (JsonException e)
            {
                const string Message = "Error while processing the response from ISAR";
                _logger.LogError(e, "{Message}", Message);
                return StatusCode(StatusCodes.Status500InternalServerError, Message);
            }

            try { await _robotService.SetCurrentMissionId(robotId, null); }
            catch (RobotNotFoundException e) { return NotFound(e.Message); }

            return NoContent();
        }

        /// <summary>
        ///     Pause the current mission on a robot
        /// </summary>
        /// <remarks>
        ///     <para> This query pauses the current mission for a robot </para>
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
                _logger.LogWarning("Could not find robot with id={Id}", robotId);
                return NotFound();
            }

            try
            {
                await _isarService.PauseMission(robot);
            }
            catch (HttpRequestException e)
            {
                const string Message = "Error connecting to ISAR while pausing mission";
                _logger.LogError(e, "{Message}", Message);
                OnIsarUnavailable(robot);
                return StatusCode(StatusCodes.Status502BadGateway, Message);
            }
            catch (MissionException e)
            {
                _logger.LogError(e, "Error while pausing ISAR mission");
                return StatusCode(StatusCodes.Status502BadGateway, $"{e.Message}");
            }
            catch (JsonException e)
            {
                const string Message = "Error while processing of the response from ISAR";
                _logger.LogError(e, "{Message}", Message);
                return StatusCode(StatusCodes.Status500InternalServerError, Message);
            }

            return NoContent();
        }

        /// <summary>
        ///     Resume paused mission on a robot
        /// </summary>
        /// <remarks>
        ///     <para> This query resumes the currently paused mission for a robot </para>
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
                _logger.LogWarning("Could not find robot with id={Id}", robotId);
                return NotFound();
            }

            try
            {
                await _isarService.ResumeMission(robot);
            }
            catch (HttpRequestException e)
            {
                const string Message = "Error connecting to ISAR while resuming mission";
                _logger.LogError(e, "{Message}", Message);
                OnIsarUnavailable(robot);
                return StatusCode(StatusCodes.Status502BadGateway, Message);
            }
            catch (MissionException e)
            {
                _logger.LogError(e, "Error while resuming ISAR mission");
                return StatusCode(StatusCodes.Status502BadGateway, $"{e.Message}");
            }
            catch (JsonException e)
            {
                const string Message = "Error while processing of the response from ISAR";
                _logger.LogError(e, "{Message}", Message);
                return StatusCode(StatusCodes.Status500InternalServerError, Message);
            }

            return NoContent();
        }


        /// <summary>
        ///     Post new arm position ("battery_change", "transport", "lookout") for the robot with id 'robotId'
        /// </summary>
        /// <remarks>
        ///     <para> This query moves the arm to a given position for a given robot </para>
        /// </remarks>
        [HttpPut]
        [Authorize(Roles = Role.User)]
        [Route("{robotId}/SetArmPosition/{armPosition}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> SetArmPosition(
            [FromRoute] string robotId,
            [FromRoute] string armPosition
        )
        {
            var robot = await _robotService.ReadById(robotId);
            if (robot == null)
            {
                string errorMessage = $"Could not find robot with id {robotId}";
                _logger.LogWarning("{Message}", errorMessage);
                return NotFound(errorMessage);
            }

            if (robot.Status is not RobotStatus.Available)
            {
                string errorMessage = $"Robot {robotId} has status ({robot.Status}) and is not available";
                _logger.LogWarning("{Message}", errorMessage);
                return Conflict(errorMessage);
            }
            try { await _isarService.StartMoveArm(robot, armPosition); }
            catch (HttpRequestException e)
            {
                string errorMessage = $"Error connecting to ISAR at {robot.IsarUri}";
                _logger.LogError(e, "{Message}", errorMessage);
                OnIsarUnavailable(robot);
                return StatusCode(StatusCodes.Status502BadGateway, errorMessage);
            }
            catch (MissionException e)
            {
                const string ErrorMessage = "An error occurred while setting the arm position mission";
                _logger.LogError(e, "{Message}", ErrorMessage);
                return StatusCode(StatusCodes.Status502BadGateway, ErrorMessage);
            }
            catch (JsonException e)
            {
                const string ErrorMessage = "Error while processing of the response from ISAR";
                _logger.LogError(e, "{Message}", ErrorMessage);
                return StatusCode(StatusCodes.Status500InternalServerError, ErrorMessage);
            }

            return NoContent();
        }

        /// <summary>
        ///     Start a localization mission with localization in the pose 'localizationPose' for the robot with id 'robotId'
        /// </summary>
        /// <remarks>
        ///     <para> This query starts a localization for a given robot </para>
        /// </remarks>
        [HttpPost]
        [Authorize(Roles = Role.User)]
        [Route("start-localization")]
        [ProducesResponseType(typeof(MissionRun), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<MissionRun>> StartLocalizationMission(
            [FromBody] ScheduleLocalizationMissionQuery scheduleLocalizationMissionQuery
        )
        {
            var robot = await _robotService.ReadById(scheduleLocalizationMissionQuery.RobotId);
            if (robot == null)
            {
                _logger.LogWarning("Could not find robot with id={Id}", scheduleLocalizationMissionQuery.RobotId);
                return NotFound("Robot not found");
            }

            if (robot.Status is not RobotStatus.Available)
            {
                _logger.LogWarning(
                    "Robot '{Id}' is not available ({Status})",
                    scheduleLocalizationMissionQuery.RobotId,
                    robot.Status.ToString()
                );
                return Conflict($"The Robot is not available ({robot.Status})");
            }

            var area = await _areaService.ReadById(scheduleLocalizationMissionQuery.AreaId);

            if (area == null)
            {
                _logger.LogWarning("Could not find area with id={Id}", scheduleLocalizationMissionQuery.AreaId);
                return NotFound("Area not found");
            }

            var missionRun = new MissionRun
            {
                Name = "Localization Mission",
                Robot = robot,
                MissionRunPriority = MissionRunPriority.Normal,
                InstallationCode = "NA",
                Area = area,
                Status = MissionStatus.Pending,
                DesiredStartTime = DateTime.UtcNow,
                Tasks = new List<MissionTask>(),
                Map = new MapMetadata()
            };

            IsarMission isarMission;
            try
            {
                isarMission = await _isarService.StartLocalizationMission(robot, scheduleLocalizationMissionQuery.LocalizationPose);
            }
            catch (HttpRequestException e)
            {
                string message = $"Could not reach ISAR at {robot.IsarUri}";
                _logger.LogError(e, "{Message}", message);
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
                const string Message = "Error while processing of the response from ISAR";
                _logger.LogError(e, "{Message}", Message);
                return StatusCode(StatusCodes.Status500InternalServerError, Message);
            }

            missionRun.UpdateWithIsarInfo(isarMission);
            missionRun.Status = MissionStatus.Ongoing;

            await _missionRunService.Create(missionRun);

            robot.Status = RobotStatus.Busy;
            robot.CurrentMissionId = missionRun.Id;
            robot.CurrentArea = area;
            await _robotService.Update(robot);
            return Ok(missionRun);
        }

        private async void OnIsarUnavailable(Robot robot)
        {
            robot.Enabled = false;
            robot.Status = RobotStatus.Offline;
            if (robot.CurrentMissionId != null)
            {
                var missionRun = await _missionRunService.ReadById(robot.CurrentMissionId);
                if (missionRun != null)
                {
                    missionRun.SetToFailed();
                    await _missionRunService.Update(missionRun);
                    _logger.LogWarning(
                        "Mission '{Id}' failed because ISAR could not be reached",
                        missionRun.Id
                    );
                }
            }
            robot.CurrentMissionId = null;
            await _robotService.Update(robot);
        }
    }
}
