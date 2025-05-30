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

            var robot = await robotService.ReadById(robotId, readOnly: true);
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
