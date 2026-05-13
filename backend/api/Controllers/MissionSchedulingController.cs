using Api.Controllers.Models;
using Api.Database.Models;
using Api.Services;
using Api.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController]
    [Route("missions")]
    public class MissionSchedulingController(
        IMissionDefinitionService missionDefinitionService,
        IMissionRunService missionRunService,
        IMissionSchedulingService missionSchedulingService,
        IInstallationService installationService,
        ILogger<MissionSchedulingController> logger,
        IRobotService robotService
    ) : ControllerBase
    {
        /// <summary>
        ///     Rerun a mission run, running only the parts that did not previously complete
        /// </summary>
        /// <remarks>
        ///     <para> This query runs the unfinished tasks of a previous mission run </para>
        /// </remarks>
        [HttpPost("rerun/{missionRunId}")]
        [Authorize(Roles = Role.User)]
        [ProducesResponseType(typeof(MissionRun), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<MissionRun>> Rerun(
            [FromRoute] string missionRunId,
            [FromBody] ScheduleMissionQuery scheduledMissionQuery
        )
        {
            scheduledMissionQuery = Sanitize.SanitizeUserInput(scheduledMissionQuery);

            Robot robot;
            try
            {
                robot = await robotService.GetRobotWithSchedulingPreCheck(
                    scheduledMissionQuery.RobotId,
                    readOnly: true
                );
            }
            catch (RobotNotFoundException e)
            {
                return NotFound(e.Message);
            }
            catch (RobotPreCheckFailedException e)
            {
                return BadRequest(e.Message);
            }

            var missionRun = await missionRunService.ReadById(missionRunId, readOnly: true);
            if (missionRun == null)
                return NotFound("Mission run not found");

            var missionTasks = missionRun
                .Tasks.Where(t =>
                    t.Status != Database.Models.TaskStatus.Successful
                    && t.Status != Database.Models.TaskStatus.PartiallySuccessful
                )
                .Select(t => new MissionTask(t))
                .ToList();

            if (missionTasks == null || missionTasks.Count == 0)
                return NotFound("No unfinished mission tasks were found for the requested mission");

            foreach (var task in missionTasks)
            {
                task.Id = Guid.NewGuid().ToString();
                if (task.Inspection != null)
                    task.Inspection.Id = Guid.NewGuid().ToString();
            }

            var newMissionRun = new MissionRun
            {
                Name = missionRun.Name,
                Robot = robot,
                MissionId = missionRun.MissionId,
                Status = MissionStatus.Queued,
                Tasks = missionTasks,
                CreationTime = DateTime.UtcNow,
                InstallationCode = missionRun.InstallationCode,
                InspectionArea = missionRun.InspectionArea,
            };

            if (newMissionRun.Tasks.Any())
            {
                newMissionRun.SetEstimatedTaskDuration();
            }

            // Compare with GetTasksFromSource

            try
            {
                newMissionRun = await missionRunService.Create(newMissionRun);
            }
            catch (UnsupportedRobotCapabilityException)
            {
                return BadRequest(
                    $"The robot {robot.Name} does not have the necessary sensors to run the mission."
                );
            }

            try
            {
                await missionSchedulingService.StartNextMissionRunIfSystemIsAvailable(
                    newMissionRun.Robot
                );
            }
            catch (MissionRunNotFoundException e)
            {
                logger.LogError(
                    $"Mission run created but then not found for robot ID: {newMissionRun.Robot.Id}. Exception: {e.Message}"
                );
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    "Not able to create mission run. "
                );
            }

            return CreatedAtAction(nameof(Rerun), new { id = newMissionRun.Id }, newMissionRun);
        }

        /// <summary>
        ///     Schedule an existing mission definition
        /// </summary>
        /// <remarks>
        ///     <para> This query schedules an existing mission and adds it to the database </para>
        /// </remarks>
        [HttpPost("schedule/{missionDefinitionId}")]
        [Authorize(Roles = Role.User)]
        [ProducesResponseType(typeof(MissionRun), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<MissionRun>> Schedule(
            [FromRoute] string missionDefinitionId,
            [FromBody] ScheduleMissionQuery scheduledMissionQuery
        )
        {
            scheduledMissionQuery = Sanitize.SanitizeUserInput(scheduledMissionQuery);

            Robot robot;
            try
            {
                robot = await robotService.GetRobotWithSchedulingPreCheck(
                    scheduledMissionQuery.RobotId,
                    readOnly: true
                );
            }
            catch (RobotNotFoundException e)
            {
                return NotFound(e.Message);
            }
            catch (RobotPreCheckFailedException e)
            {
                return BadRequest(e.Message);
            }

            var missionDefinition = await missionDefinitionService.ReadById(
                missionDefinitionId,
                readOnly: true
            );
            if (missionDefinition == null)
            {
                return NotFound("Mission definition not found");
            }

            if (missionDefinition.InspectionArea.Id != robot.CurrentInspectionAreaId)
                return BadRequest("Robot is not in the same inspection area as the mission.");

            try
            {
                await installationService.AssertRobotIsOnSameInstallationAsMission(
                    robot,
                    missionDefinition
                );
            }
            catch (InstallationNotFoundException e)
            {
                return NotFound(e.Message);
            }
            catch (RobotNotInSameInstallationAsMissionException e)
            {
                return Conflict(e.Message);
            }

            if (missionDefinition.Tasks == null)
                return NotFound("No mission tasks were found for the requested mission");

            var missionRun = new MissionRun
            {
                Name = missionDefinition.Name,
                Robot = robot,
                MissionId = missionDefinition.Id,
                Status = MissionStatus.Queued,
                CreationTime = DateTime.UtcNow,
                Tasks = [.. missionDefinition.Tasks.Select((t) => t.ToMissionRunTask())],
                InstallationCode = missionDefinition.InstallationCode,
                InspectionArea = missionDefinition.InspectionArea,
            };

            if (missionRun.Tasks.Any())
            {
                missionRun.SetEstimatedTaskDuration();
            }

            MissionRun newMissionRun;
            try
            {
                newMissionRun = await missionRunService.Create(missionRun);
            }
            catch (UnsupportedRobotCapabilityException)
            {
                return BadRequest(
                    $"The robot {robot.Name} does not have the necessary sensors to run the mission."
                );
            }

            try
            {
                await missionSchedulingService.StartNextMissionRunIfSystemIsAvailable(
                    newMissionRun.Robot
                );
            }
            catch (MissionRunNotFoundException e)
            {
                logger.LogError(
                    $"Mission run created but then not found for robot ID: {newMissionRun.Robot.Id}. Exception: {e.Message}"
                );
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    "Not able to create mission run. "
                );
            }

            return CreatedAtAction(nameof(Schedule), new { id = newMissionRun.Id }, newMissionRun);
        }
    }
}
