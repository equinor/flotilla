using Api.Controllers.Models;
using Api.Services;
using Api.Services.Models;
using Api.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController]
    [Route("return-to-home")]
    public class ReturnToHomeController(
        ILogger<RobotController> logger,
        IRobotService robotService,
        IIsarService isarService
    ) : ControllerBase
    {
        /// <summary>
        ///     Replace the robot's remaining return-home wait with a positive number of seconds.
        /// </summary>
        /// <response code="204">ISAR accepted the timeout.</response>
        /// <response code="400">Seconds is missing or is not a positive integer.</response>
        /// <response code="404">The robot was not found or is not accessible.</response>
        /// <response code="409">ISAR rejected the state or encountered a state-machine queue conflict or timeout.</response>
        /// <response code="500">ISAR could not be reached or returned another failure, including an unsupported endpoint.</response>
        [HttpPost("set-return-home-timeout/{robotId}")]
        [Authorize(Roles = Role.User)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> SetReturnHomeTimeout(
            [FromRoute] string robotId,
            [FromBody] SetReturnHomeTimeoutQuery query,
            CancellationToken cancellationToken
        )
        {
            robotId = Sanitize.SanitizeUserInput(robotId);

            var robot = await robotService.ReadByIdForWrite(robotId, readOnly: true);
            if (robot is null)
            {
                logger.LogWarning("Could not find robot with id {Id}", robotId);
                return NotFound();
            }

            try
            {
                await isarService.SetReturnHomeTimeout(robot, query.Seconds, cancellationToken);
            }
            catch (RobotBusyException ex)
            {
                logger.LogWarning(
                    ex,
                    "Could not set return-home timeout for robot {RobotId}",
                    robot.Id
                );
                return Conflict();
            }
            catch (Exception ex)
                when (ex is not OperationCanceledException
                    || !cancellationToken.IsCancellationRequested
                )
            {
                logger.LogError(
                    ex,
                    "Failed to set return-home timeout for robot {RobotId}",
                    robot.Id
                );
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            return NoContent();
        }

        /// <summary>
        ///     Sends the robots to their home.
        /// </summary>
        [HttpPost("schedule-return-to-home/{robotId}")]
        [Authorize(Roles = Role.User)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> ScheduleReturnToHomeMission([FromRoute] string robotId)
        {
            robotId = Sanitize.SanitizeUserInput(robotId);

            var robot = await robotService.ReadByIdForWrite(robotId, readOnly: true);
            if (robot is null)
            {
                logger.LogWarning("Could not find robot with id {Id}", robotId);
                return NotFound();
            }

            try
            {
                await isarService.ReturnHome(robot);
            }
            catch (RobotBusyException e)
            {
                string errorMessage =
                    $"Failed to create return to home mission for robot {robotId}";
                logger.LogError(e, "{Message}", errorMessage);
                return StatusCode(StatusCodes.Status409Conflict);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send robot {RobotId} home", robot.Id);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            return NoContent();
        }
    }
}
