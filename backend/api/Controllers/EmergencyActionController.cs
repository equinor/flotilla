using System.Globalization;
using Api.Controllers.Models;
using Api.Database.Models;
using Api.Services;
using Api.Services.Events;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Api.Controllers
{
    [ApiController]
    [Route("emergency-action")]
    public class EmergencyActionController : ControllerBase
    {
        private readonly IAreaService _areaService;
        private readonly IEmergencyActionService _emergencyActionService;
        private readonly ILogger<EmergencyActionController> _logger;
        private readonly IRobotService _robotService;

        public EmergencyActionController(ILogger<EmergencyActionController> logger, IRobotService robotService, IAreaService areaService, IEmergencyActionService emergencyActionService)
        {
            _logger = logger;
            _robotService = robotService;
            _areaService = areaService;
            _emergencyActionService = emergencyActionService;
        }

        /// <summary>
        ///     This endpoint will abort the current running mission run and attempt to return the robot to a safe position in the
        ///     area. The mission run queue for the robot will be frozen and no further missions will run until the emergency
        ///     action has been reversed.
        /// </summary>
        /// <remarks>
        ///     <para> The endpoint fires an event which is then processed to stop the robot and schedule the next mission </para>
        /// </remarks>
        [HttpPost]
        [Route("{robotId}/{installationCode}/{areaName}/abort-current-mission-and-go-to-safe-zone")]
        [Authorize(Roles = Role.User)]
        [ProducesResponseType(typeof(MissionRun), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> AbortCurrentMissionAndGoToSafeZone(
            [FromRoute] string robotId,
            [FromRoute] string installationCode,
            [FromRoute] string areaName)
        {
            var robot = await _robotService.ReadById(robotId);
            if (robot == null)
            {
                _logger.LogWarning("Could not find robot with id {Id}", robotId);
                return NotFound("Robot not found");
            }

            var area = await _areaService.ReadByInstallationAndName(installationCode, areaName);
            if (area == null)
            {
                _logger.LogError("Could not find area {AreaName} for installation code {InstallationCode}", areaName, installationCode);
                return NotFound("Area not found");
            }

            _emergencyActionService.TriggerEmergencyButtonPressedForRobot(new EmergencyButtonPressedForRobotEventArgs(robot.Id, area.Id));

            return Ok("Request to abort current mission and move robot back to safe position received");
        }

        /// <summary>
        ///     This endpoint will abort the current running mission run and attempt to return the robot to a safe position in the
        ///     area. The mission run queue for the robot will be frozen and no further missions will run until the emergency
        ///     action has been reversed.
        /// </summary>
        /// <remarks>
        ///     <para> The endpoint fires an event which is then processed to stop the robot and schedule the next mission </para>
        /// </remarks>
        [HttpPost]
        [Route("{installationCode}/abort-current-missions-and-send-all-robots-to-safe-zone")]
        [Authorize(Roles = Role.User)]
        [ProducesResponseType(typeof(MissionRun), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public ActionResult AbortCurrentMissionAndSendAllRobotsToSafeZone(
            [FromRoute] string installationCode)
        {

            var robots = _robotService.ReadAll().Result.ToList().FindAll(a =>
                            a.CurrentInstallation.ToLower(CultureInfo.CurrentCulture).Equals(installationCode.ToLower(CultureInfo.CurrentCulture), StringComparison.Ordinal) &&
                            a.CurrentArea != null);

            foreach (var robot in robots)
            {
                _emergencyActionService.TriggerEmergencyButtonPressedForRobot(new EmergencyButtonPressedForRobotEventArgs(robot.Id, robot.CurrentArea!.Id));

            }

            return Ok("Request to abort current mission and move all robots back to safe position received");
        }

        /// <summary>
        ///     This query will clear the emergency state that is introduced by aborting the current mission and returning to a
        ///     safe zone. Clearing the emergency state means that mission runs that may be in the robots queue will start."
        /// </summary>
        [HttpPost]
        [Route("{robotId}/{installationCode}/{areaName}/clear-robot-emergency-state")]
        [Authorize(Roles = Role.User)]
        [ProducesResponseType(typeof(MissionRun), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> ClearEmergencyState(
            [FromRoute] string robotId,
            [FromRoute] string installationCode,
            [FromRoute] string areaName)
        {
            var robot = await _robotService.ReadById(robotId);
            if (robot == null)
            {
                _logger.LogWarning("Could not find robot with id {Id}", robotId);
                return NotFound("Robot not found");
            }

            var area = await _areaService.ReadByInstallationAndName(installationCode, areaName);
            if (area == null)
            {
                _logger.LogError("Could not find area {AreaName} for installation code {InstallationCode}", areaName, installationCode);
                return NotFound("Area not found");
            }

            _emergencyActionService.TriggerEmergencyButtonDepressedForRobot(new EmergencyButtonPressedForRobotEventArgs(robot.Id, area.Id));

            return Ok("Request to clear emergency state for robot was received");
        }

        /// <summary>
        ///     This query will clear the emergency state that is introduced by aborting the current mission and returning to a
        ///     safe zone. Clearing the emergency state means that mission runs that may be in the robots queue will start."
        /// </summary>
        [HttpPost]
        [Route("{installationCode}/clear-emergency-state")]
        [Authorize(Roles = Role.User)]
        [ProducesResponseType(typeof(MissionRun), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public ActionResult ClearEmergencyStateForAllRobots(
            [FromRoute] string installationCode)
        {
            var robots = _robotService.ReadAll().Result.ToList().FindAll(a =>
                            a.CurrentInstallation.ToLower(CultureInfo.CurrentCulture).Equals(installationCode.ToLower(CultureInfo.CurrentCulture), StringComparison.Ordinal) &&
                            a.CurrentArea != null);

            foreach (var robot in robots)
            {
                _emergencyActionService.TriggerEmergencyButtonDepressedForRobot(new EmergencyButtonPressedForRobotEventArgs(robot.Id, robot.CurrentArea!.Id));
            }

            return Ok("Request to clear emergency state for robot was received");
        }
    }
}
