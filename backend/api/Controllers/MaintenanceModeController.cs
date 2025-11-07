using Api.Controllers.Models;
using Api.Services;
using Api.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController]
    [Route("robots")]
    public class MaintenanceModeController(
        ILogger<RobotController> logger,
        IRobotService robotService,
        IIsarService isarService
    ) : ControllerBase
    {
        /// <summary>
        ///     Send the robot to maintenance mode.
        /// </summary>
        [HttpPost("set-maintenance-mode/{robotId}")]
        [Authorize(Roles = Role.User)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> SetMaintenanceMode([FromRoute] string robotId)
        {
            robotId = Sanitize.SanitizeUserInput(robotId);

            var robot = await robotService.ReadById(robotId, readOnly: true);
            if (robot is null)
            {
                logger.LogWarning("Could not find robot with id {Id}", robotId);
                return NotFound();
            }

            try
            {
                await isarService.SetMaintenanceMode(robot.IsarUri);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to set maintenance mode for robot {RobotId}", robot.Id);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            return NoContent();
        }

        /// <summary>
        ///     Release the robot from maintenance mode.
        /// </summary>
        [HttpPost("release-maintenance-mode/{robotId}")]
        [Authorize(Roles = Role.User)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> ReleaseMaintenanceMode([FromRoute] string robotId)
        {
            robotId = Sanitize.SanitizeUserInput(robotId);

            var robot = await robotService.ReadById(robotId, readOnly: true);
            if (robot is null)
            {
                logger.LogWarning("Could not find robot with id {Id}", robotId);
                return NotFound();
            }

            try
            {
                await isarService.ReleaseMaintenanceMode(robot.IsarUri);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to release maintenance mode for robot {RobotId}",
                    robot.Id
                );
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            return NoContent();
        }
    }
}
