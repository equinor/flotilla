using Api.Controllers.Models;
using Api.Database.Models;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController]
    [Route("return-to-home")]
    public class ReturnToHomeController(
        ILogger<RobotController> logger,
        IReturnToHomeService returnToHomeService,
        IRobotService robotService
    ) : ControllerBase
    {
        /// <summary>
        ///     Sends the robots to their home.
        /// </summary>
        [HttpPost("schedule-return-to-home/{robotId}")]
        [Authorize(Roles = Role.User)]
        [ProducesResponseType(typeof(MissionRun), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IList<MissionRun>>> ScheduleReturnToHomeMission(
            [FromRoute] string robotId
        )
        {
            var robot = await robotService.ReadById(robotId, readOnly: true);
            if (robot is null)
            {
                logger.LogWarning("Could not find robot with id {Id}", robotId);
                return NotFound();
            }

            var returnToHomeMission =
                await returnToHomeService.ScheduleReturnToHomeMissionRunIfNotAlreadyScheduled(
                    robot,
                    true
                );
            if (returnToHomeMission is null)
            {
                string errorMessage = "Error while scheduling Return Home mission";
                logger.LogError(errorMessage);
                return StatusCode(StatusCodes.Status502BadGateway, $"{errorMessage}");
            }

            return Ok(returnToHomeMission);
        }
    }
}
