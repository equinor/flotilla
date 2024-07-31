using System.Text.Json;
using Api.Controllers.Models;
using Api.Database.Models;
using Api.Services;
using Api.Services.Events;
using Api.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Api.Controllers
{
    [ApiController]
    [Route("robots")]
    public class RobotController(
            ILogger<RobotController> logger,
            IRobotService robotService,
            IIsarService isarService,
            IMissionSchedulingService missionSchedulingService,
            IRobotModelService robotModelService,
            IAreaService areaService
        ) : ControllerBase
    {
        /// <summary>
        ///     List all robots on the installation.
        /// </summary>
        /// <remarks>
        ///     <para> This query gets all robots </para>
        /// </remarks>
        [HttpGet]
        [Authorize(Roles = Role.Any)]
        [ProducesResponseType(typeof(IList<RobotResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IList<RobotResponse>>> GetRobots()
        {
            try
            {
                var robots = await robotService.ReadAll();
                var robotResponses = robots.Select(robot => new RobotResponse(robot));
                return Ok(robotResponses);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error during GET of robots  from database");
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
        [ProducesResponseType(typeof(RobotResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<RobotResponse>> GetRobotById([FromRoute] string id)
        {
            logger.LogInformation("Getting robot with id={Id}", id);
            try
            {
                var robot = await robotService.ReadById(id);
                if (robot == null)
                {
                    logger.LogWarning("Could not find robot with id={Id}", id);
                    return NotFound();
                }

                var robotResponse = new RobotResponse(robot);
                logger.LogInformation("Successful GET of robot with id={id}", id);
                return Ok(robotResponse);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error during GET of robot with id={Id}", id);
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
        [ProducesResponseType(typeof(RobotResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<RobotResponse>> CreateRobot([FromBody] CreateRobotQuery robotQuery)
        {
            logger.LogInformation("Creating new robot");
            try
            {
                var robotModel = await robotModelService.ReadByRobotType(robotQuery.RobotType);
                if (robotModel == null)
                {
                    return BadRequest(
                        $"No robot model exists with robot type '{robotQuery.RobotType}'"
                    );
                }

                var newRobot = await robotService.CreateFromQuery(robotQuery);
                var robotResponses = new RobotResponse(newRobot);

                logger.LogInformation("Succesfully created new robot");
                return CreatedAtAction(nameof(GetRobotById), new
                {
                    id = newRobot.Id
                }, robotResponses);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error while creating new robot");
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
        [ProducesResponseType(typeof(RobotResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<RobotResponse>> UpdateRobot(
            [FromRoute] string id,
            [FromBody] Robot robot
        )
        {
            logger.LogInformation("Updating robot with id={Id}", id);

            if (!ModelState.IsValid)
            {
                return BadRequest("Invalid data");
            }

            if (id != robot.Id)
            {
                logger.LogWarning("Id: {Id} not corresponding to updated robot", id);
                return BadRequest("Inconsistent Id");
            }

            try
            {
                var updatedRobot = await robotService.Update(robot);
                var robotResponse = new RobotResponse(updatedRobot);
                logger.LogInformation("Successful PUT of robot to database");

                return Ok(robotResponse);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error while updating robot with id={Id}", id);
                throw;
            }
        }

        /// <summary>
        ///     Updates a specific field of a robot in the database
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <response code="200"> The robot was successfully updated </response>
        /// <response code="400"> The robot data is invalid </response>
        /// <response code="404"> There was no robot with the given ID in the database </response>
        /// /// <response code="404"> The given field name is not valid </response>
        [HttpPut]
        [Authorize(Roles = Role.Admin)]
        [Route("{id}/{fieldName}")]
        [ProducesResponseType(typeof(RobotResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<RobotResponse>> UpdateRobotField(
            [FromRoute] string id,
            [FromRoute] string fieldName,
            [FromBody] UpdateRobotQuery query
        )
        {
            logger.LogInformation("Updating robot with id={Id}", id);

            if (!ModelState.IsValid)
                return BadRequest("Invalid data");

            try
            {
                var robot = await robotService.ReadById(id);
                if (robot == null)
                {
                    string errorMessage = $"No robot with id: {id} could be found";
                    logger.LogError("{Message}", errorMessage);
                    return NotFound(errorMessage);
                }

                Robot updatedRobot;
                switch (fieldName)
                {
                    case "areaId":
                        if (query.AreaId == null)
                            updatedRobot = await robotService.UpdateCurrentArea(id, null);
                        else
                        {
                            var area = await areaService.ReadById(query.AreaId, readOnly: true);
                            if (area == null) return NotFound($"No area with ID {query.AreaId} was found");
                            updatedRobot = await robotService.UpdateCurrentArea(id, area.Id);
                        }
                        break;
                    case "pose":
                        if (query.Pose == null) return BadRequest("Cannot set robot pose to null");
                        updatedRobot = await robotService.UpdateRobotPose(id, query.Pose);
                        break;
                    case "missionId":
                        updatedRobot = await robotService.UpdateCurrentMissionId(id, query.MissionId);
                        break;
                    default:
                        return NotFound($"Could not find any field with name {fieldName}");
                }

                var robotResponse = new RobotResponse(updatedRobot);
                logger.LogInformation("Successful PUT of robot to database");

                return Ok(robotResponse);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error while updating robot with id={Id}", id);
                throw;
            }
        }

        /// <summary>
        ///     Updates deprecated field of a robot in the database
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <response code="200"> The robot was successfully updated </response>
        /// <response code="404"> There was no robot with the given ID in the database </response>
        [HttpPut]
        [Authorize(Roles = Role.Admin)]
        [Route("{id}/deprecated/{deprecated}")]
        [ProducesResponseType(typeof(RobotResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<RobotResponse>> UpdateRobotDeprecated(
            [FromRoute] string id,
            [FromRoute] bool deprecated
        )
        {
            logger.LogInformation("Updating deprecated on robot with id={Id} to deprecated={Deprecated}", id, deprecated);

            try
            {
                var robot = await robotService.ReadById(id);
                if (robot == null)
                {
                    string errorMessage = $"No robot with id: {id} could be found";
                    logger.LogError("{Message}", errorMessage);
                    return NotFound(errorMessage);
                }

                Robot updatedRobot;
                updatedRobot = await robotService.UpdateDeprecated(id, deprecated);

                var robotResponse = new RobotResponse(updatedRobot);
                logger.LogInformation("Successful updated deprecated on robot to database");

                return Ok(robotResponse);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error while updating robot with id={Id}", id);
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
        public async Task<ActionResult<RobotResponse>> DeleteRobot([FromRoute] string id)
        {
            var robot = await robotService.Delete(id);
            if (robot is null)
            {
                return NotFound($"Robot with id {id} not found");
            }
            var robotResponse = new RobotResponse(robot);
            return Ok(robotResponse);
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
        [ProducesResponseType(typeof(RobotResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<RobotResponse>> UpdateRobotStatus(
            [FromRoute] string id,
            [FromBody] RobotStatus robotStatus
        )
        {
            logger.LogInformation("Updating robot status with id={Id}", id);

            if (!ModelState.IsValid) return BadRequest("Invalid data");

            var robot = await robotService.ReadById(id);
            if (robot == null)
            {
                string errorMessage = $"No robot with id: {id} could be found";
                logger.LogError("{Message}", errorMessage);
                return NotFound(errorMessage);
            }

            try
            {
                var updatedRobot = await robotService.UpdateRobotStatus(id, robotStatus);
                logger.LogInformation("Successfully updated robot {RobotId}", updatedRobot.Id);

                var robotResponse = new RobotResponse(updatedRobot);

                if (robotStatus == RobotStatus.Available) missionSchedulingService.TriggerRobotAvailable(new RobotAvailableEventArgs(robot.Id));

                return Ok(robotResponse);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error while updating status for robot with id={Id}", id);
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
            var robot = await robotService.ReadById(robotId);
            if (robot == null)
            {
                logger.LogWarning("Could not find robot with id={Id}", robotId);
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
        [ProducesResponseType(typeof(RobotResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<RobotResponse>> CreateVideoStream(
            [FromRoute] string robotId,
            [FromBody] VideoStream videoStream
        )
        {
            var robot = await robotService.ReadById(robotId);
            if (robot == null)
            {
                logger.LogWarning("Could not find robot with id={Id}", robotId);
                return NotFound();
            }

            robot.VideoStreams.Add(videoStream);

            try
            {
                var updatedRobot = await robotService.Update(robot);
                var robotResponse = new RobotResponse(updatedRobot);

                return CreatedAtAction(
                    nameof(GetVideoStreams),
                    new
                    {
                        robotId = updatedRobot.Id
                    },
                    robotResponse
                );
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error adding video stream to robot");
                throw;
            }
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
            var robot = await robotService.ReadById(robotId);
            if (robot == null)
            {
                logger.LogWarning("Could not find robot with id={Id}", robotId);
                return NotFound();
            }

            try { await isarService.StopMission(robot); }
            catch (HttpRequestException e)
            {
                const string Message = "Error connecting to ISAR while stopping mission";
                logger.LogError(e, "{Message}", Message);
                await robotService.HandleLosingConnectionToIsar(robot.Id);
                return StatusCode(StatusCodes.Status502BadGateway, Message);
            }
            catch (MissionException e)
            {
                logger.LogError(e, "Error while stopping ISAR mission");
                return StatusCode(StatusCodes.Status502BadGateway, $"{e.Message}");
            }
            catch (JsonException e)
            {
                const string Message = "Error while processing the response from ISAR";
                logger.LogError(e, "{Message}", Message);
                return StatusCode(StatusCodes.Status500InternalServerError, Message);
            }
            catch (MissionNotFoundException)
            {
                logger.LogWarning($"No mission was runnning for robot {robot.Id}");
                try { await robotService.UpdateCurrentMissionId(robotId, null); }
                catch (RobotNotFoundException e) { return NotFound(e.Message); }

            }
            try { await robotService.UpdateCurrentMissionId(robotId, null); }
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
            var robot = await robotService.ReadById(robotId);
            if (robot == null)
            {
                logger.LogWarning("Could not find robot with id={Id}", robotId);
                return NotFound();
            }

            try
            {
                await isarService.PauseMission(robot);
            }
            catch (HttpRequestException e)
            {
                const string Message = "Error connecting to ISAR while pausing mission";
                logger.LogError(e, "{Message}", Message);
                await robotService.HandleLosingConnectionToIsar(robot.Id);
                return StatusCode(StatusCodes.Status502BadGateway, Message);
            }
            catch (MissionException e)
            {
                logger.LogError(e, "Error while pausing ISAR mission");
                return StatusCode(StatusCodes.Status502BadGateway, $"{e.Message}");
            }
            catch (JsonException e)
            {
                const string Message = "Error while processing of the response from ISAR";
                logger.LogError(e, "{Message}", Message);
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
            var robot = await robotService.ReadById(robotId);
            if (robot == null)
            {
                logger.LogWarning("Could not find robot with id={Id}", robotId);
                return NotFound();
            }

            try
            {
                await isarService.ResumeMission(robot);
            }
            catch (HttpRequestException e)
            {
                const string Message = "Error connecting to ISAR while resuming mission";
                logger.LogError(e, "{Message}", Message);
                await robotService.HandleLosingConnectionToIsar(robot.Id);
                return StatusCode(StatusCodes.Status502BadGateway, Message);
            }
            catch (MissionException e)
            {
                logger.LogError(e, "Error while resuming ISAR mission");
                return StatusCode(StatusCodes.Status502BadGateway, $"{e.Message}");
            }
            catch (JsonException e)
            {
                const string Message = "Error while processing of the response from ISAR";
                logger.LogError(e, "{Message}", Message);
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
            var robot = await robotService.ReadById(robotId);
            if (robot == null)
            {
                string errorMessage = $"Could not find robot with id {robotId}";
                logger.LogWarning("{Message}", errorMessage);
                return NotFound(errorMessage);
            }

            if (robot.Status is not RobotStatus.Available)
            {
                string errorMessage = $"Robot {robotId} has status ({robot.Status}) and is not available";
                logger.LogWarning("{Message}", errorMessage);
                return Conflict(errorMessage);
            }

            if (robot.Deprecated)
            {
                string errorMessage = $"Robot {robotId} is deprecated ({robot.Status}) and cannot run missions";
                logger.LogWarning("{Message}", errorMessage);
                return Conflict(errorMessage);
            }

            try { await isarService.StartMoveArm(robot, armPosition); }
            catch (HttpRequestException e)
            {
                string errorMessage = $"Error connecting to ISAR at {robot.IsarUri}";
                logger.LogError(e, "{Message}", errorMessage);
                await robotService.HandleLosingConnectionToIsar(robot.Id);
                return StatusCode(StatusCodes.Status502BadGateway, errorMessage);
            }
            catch (MissionException e)
            {
                const string ErrorMessage = "An error occurred while setting the arm position mission";
                logger.LogError(e, "{Message}", ErrorMessage);
                return StatusCode(StatusCodes.Status502BadGateway, ErrorMessage);
            }
            catch (JsonException e)
            {
                const string ErrorMessage = "Error while processing of the response from ISAR";
                logger.LogError(e, "{Message}", ErrorMessage);
                return StatusCode(StatusCodes.Status500InternalServerError, ErrorMessage);
            }

            return NoContent();
        }

        /// <summary>
        ///   Empties the mission queue for the robot, stops the ongoing mission, sets the robot to available and current area is set to null
        /// </summary>
        /// <remarks>
        ///     <para> This query resets the robot </para>
        /// </remarks>
        [HttpPut]
        [Authorize(Roles = Role.Admin)]
        [Route("{robotId}/reset-robot")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> ResetRobot(
            [FromRoute] string robotId
        )
        {
            var robot = await robotService.ReadById(robotId);
            if (robot == null)
            {
                string errorMessage = $"Could not find robot with id {robotId}";
                logger.LogWarning("{Message}", errorMessage);
                return NotFound(errorMessage);
            }

            try { await missionSchedulingService.AbortAllScheduledMissions(robot.Id, "Aborted: Robot was reset"); }
            catch (RobotNotFoundException)
            {
                string errorMessage = $"Failed to abort scheduled missions for robot with id {robotId}";
                logger.LogWarning("{Message}", errorMessage);
                return NotFound(errorMessage);
            }

            try { await missionSchedulingService.StopCurrentMissionRun(robot.Id); }
            catch (RobotNotFoundException)
            {
                string errorMessage = $"Failed to stop current mission for robot with id {robotId} because the robot was not found";
                logger.LogWarning("{Message}", errorMessage);
                return NotFound(errorMessage);
            }
            catch (MissionRunNotFoundException)
            {
                string errorMessage = $"Failed to stop current mission for robot with id {robotId} because the mission was not found";
                logger.LogWarning("{Message}", errorMessage);
                return Conflict(errorMessage);
            }
            catch (MissionException ex)
            {
                if (ex.IsarStatusCode != StatusCodes.Status409Conflict)
                {
                    string errorMessage = "Error while stopping ISAR mission";
                    logger.LogError(ex, "{Message}", errorMessage);
                    return Conflict(errorMessage);
                }
            }
            catch (Exception ex)
            {
                string errorMessage = "Error in ISAR while stopping current mission";
                logger.LogError(ex, "{Message}", errorMessage);
                return Conflict(errorMessage);
            }

            try { await robotService.UpdateCurrentArea(robot.Id, null); }
            catch (RobotNotFoundException)
            {
                string errorMessage = $"Failed to set current area to null for robot with id {robotId} because the robot was not found";
                logger.LogWarning("{Message}", errorMessage);
                return NotFound(errorMessage);
            }

            return NoContent();
        }
    }
}
