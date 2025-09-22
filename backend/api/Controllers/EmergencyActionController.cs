using Api.Controllers.Models;
using Api.Services;
using Api.Services.Events;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController]
    [Route("emergency-action")]
    public class EmergencyActionController(
        IRobotService robotService,
        IEmergencyActionService emergencyActionService
    ) : ControllerBase
    {
        /// <summary>
        ///     This endpoint will abort the current running mission run and attempt to return the robot to the docking station in the
        ///     area. The mission run queue for the robot will be frozen and no further missions will run until the emergency
        ///     action has been reversed.
        /// </summary>
        /// <remarks>
        ///     <para> The endpoint fires an event which is then processed to stop the robot and schedule the next mission </para>
        /// </remarks>
        [HttpPost]
        [Route("{installationCode}/abort-current-missions-and-send-all-robots-to-safe-zone")]
        [Authorize(Roles = Role.User)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<string>> AbortCurrentMissionAndSendAllRobotsToDock(
            [FromRoute] string installationCode
        )
        {
            var robots = await robotService.ReadRobotsForInstallation(
                installationCode,
                readOnly: true
            );

            foreach (var robot in robots)
            {
                emergencyActionService.LockdownRobot(
                    new RobotEmergencyEventArgs(
                        robot,
                        "Robot couldn't complete mission as 'Send robots to dock'-button was clicked"
                    )
                );
            }

            return NoContent();
        }

        /// <summary>
        ///     This query will clear the emergency state that is introduced by aborting the current mission and returning to the
        ///     docking station. Clearing the emergency state means that mission runs that may be in the robots queue will start."
        /// </summary>
        [HttpPost]
        [Route("{installationCode}/clear-emergency-state")]
        [Authorize(Roles = Role.User)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<string>> ClearEmergencyStateForAllRobots(
            [FromRoute] string installationCode
        )
        {
            var robots = await robotService.ReadRobotsForInstallation(
                installationCode,
                readOnly: true
            );

            foreach (var robot in robots)
            {
                emergencyActionService.ReleaseRobotFromLockdown(new RobotEmergencyEventArgs(robot));
            }

            return NoContent();
        }
    }
}
