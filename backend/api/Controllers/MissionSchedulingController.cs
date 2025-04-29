using System.Globalization;
using System.Text.Json;
using Api.Controllers.Models;
using Api.Database.Models;
using Api.Services;
using Api.Services.MissionLoaders;
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
        IInstallationService installationService,
        IMissionLoader missionLoader,
        ILogger<MissionSchedulingController> logger,
        IMapService mapService,
        IRobotService robotService,
        ISourceService sourceService,
        IInspectionAreaService inspectionAreaService
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
                .Tasks.Where(
                    (t) =>
                        t.Status != Database.Models.TaskStatus.Successful
                        && t.Status != Database.Models.TaskStatus.PartiallySuccessful
                )
                .Select((t) => new MissionTask(t))
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
                Status = MissionStatus.Pending,
                MissionRunType = MissionRunType.Normal,
                Tasks = missionTasks,
                DesiredStartTime = scheduledMissionQuery.DesiredStartTime ?? DateTime.UtcNow,
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
            else if (missionDefinition.InspectionArea == null)
            {
                logger.LogWarning(
                    "Mission definition with ID {id} does not have an inspection area when scheduling",
                    missionDefinition.Id
                );
            }

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

            var missionTasks = await missionLoader.GetTasksForMission(
                missionDefinition.Source.SourceId
            );
            if (missionTasks == null)
                return NotFound("No mission tasks were found for the requested mission");

            var missionRun = new MissionRun
            {
                Name = missionDefinition.Name,
                Robot = robot,
                MissionId = missionDefinition.Id,
                Status = MissionStatus.Pending,
                MissionRunType = MissionRunType.Normal,
                DesiredStartTime = scheduledMissionQuery.DesiredStartTime ?? DateTime.UtcNow,
                Tasks = missionTasks,
                InstallationCode = missionDefinition.InstallationCode,
                InspectionArea = missionDefinition.InspectionArea,
            };

            if (missionDefinition.Map == null)
            {
                var newMap = await mapService.ChooseMapFromMissionRunTasks(missionRun);
                if (newMap != null)
                {
                    logger.LogInformation(
                        $"Assigned map {newMap.MapName} to mission definition with id {missionDefinition.Id}"
                    );
                    missionDefinition.Map = newMap;
                    await missionDefinitionService.Update(missionDefinition);
                }
            }

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

            return CreatedAtAction(nameof(Schedule), new { id = newMissionRun.Id }, newMissionRun);
        }

        /// <summary>
        ///     Schedule a mission based on mission loader
        /// </summary>
        /// <remarks>
        ///     <para> This query schedules a new mission and adds it to the database </para>
        /// </remarks>
        [HttpPost]
        [Authorize(Roles = Role.User)]
        [ProducesResponseType(typeof(MissionRun), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<MissionRun>> Create(
            [FromBody] ScheduledMissionQuery scheduledMissionQuery
        )
        {
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
            string missionSourceId = scheduledMissionQuery.MissionSourceId.ToString(
                CultureInfo.CurrentCulture
            );
            MissionDefinition? missionDefinition;
            try
            {
                missionDefinition = await missionLoader.GetMissionById(missionSourceId);
                if (missionDefinition == null)
                {
                    return NotFound("Mission not found");
                }
            }
            catch (HttpRequestException e)
            {
                if (e.StatusCode.HasValue && (int)e.StatusCode.Value == 404)
                {
                    logger.LogWarning("Could not find mission with id={Id}", missionSourceId);
                    return NotFound("Mission not found");
                }

                logger.LogError(e, "Error getting mission from mission loader");
                return StatusCode(StatusCodes.Status502BadGateway, $"{e.Message}");
            }
            catch (JsonException e)
            {
                const string Message = "Error deserializing mission";
                logger.LogError(e, "{Message}", Message);
                return StatusCode(StatusCodes.Status500InternalServerError, Message);
            }
            catch (InvalidDataException e)
            {
                const string Message =
                    "Can not schedule mission because Mission is invalid. One or more tasks does not contain a robot pose";
                logger.LogError(e, "Message: {errorMessage}", Message);
                return StatusCode(StatusCodes.Status502BadGateway, Message);
            }

            var missionTasks = await missionLoader.GetTasksForMission(missionSourceId);

            if (missionTasks == null)
            {
                return NotFound("No mission tasks were found for the requested mission");
            }

            var inspectionAreaForMission =
                inspectionAreaService.TryFindInspectionAreaForMissionTasks(
                    missionTasks,
                    scheduledMissionQuery.InstallationCode
                );
            if (inspectionAreaForMission == null)
            {
                return BadRequest("No inspection area found for the mission tasks");
            }

            if (robot.CurrentInspectionAreaId == null)
            {
                return BadRequest("Robot does not have an inspection area");
            }

            if (inspectionAreaForMission.Id != robot.CurrentInspectionAreaId)
            {
                return BadRequest(
                    $"The tasks of the mission are not inside the inspection area of the robot"
                );
            }

            var source = await sourceService.CheckForExistingSource(
                scheduledMissionQuery.MissionSourceId
            );
            MissionDefinition? existingMissionDefinition = null;
            if (source == null)
            {
                source = await sourceService.Create(
                    new Source { SourceId = $"{missionDefinition.Id}" }
                );
            }
            else
            {
                existingMissionDefinition = await missionDefinitionService.ReadBySourceId(
                    source.SourceId,
                    readOnly: true
                );
            }

            var scheduledMissionDefinition =
                existingMissionDefinition
                ?? new MissionDefinition
                {
                    Id = Guid.NewGuid().ToString(),
                    Source = source,
                    Name = missionDefinition.Name,
                    InspectionFrequency = scheduledMissionQuery.InspectionFrequency,
                    InstallationCode = scheduledMissionQuery.InstallationCode,
                    Map = new MapMetadata(),
                };

            if (
                scheduledMissionDefinition.InspectionArea == null
                || scheduledMissionDefinition.InspectionArea.Id != inspectionAreaForMission.Id
            )
            {
                logger.LogWarning(
                    "Inspection area for mission definition {Id} was changed from {OldInspectionAreaId} to {NewInspectionAreaId}",
                    scheduledMissionDefinition.Id,
                    scheduledMissionDefinition.InspectionArea?.Id,
                    inspectionAreaForMission.Id
                );
                scheduledMissionDefinition.InspectionArea = inspectionAreaForMission;
            }

            var missionRun = new MissionRun
            {
                Name = missionDefinition.Name,
                Robot = robot,
                MissionId = scheduledMissionDefinition.Id,
                Status = MissionStatus.Pending,
                MissionRunType = MissionRunType.Normal,
                DesiredStartTime = scheduledMissionQuery.DesiredStartTime ?? DateTime.UtcNow,
                Tasks = missionTasks,
                InstallationCode = scheduledMissionQuery.InstallationCode,
                InspectionArea = scheduledMissionDefinition.InspectionArea,
            };

            scheduledMissionDefinition.Map = await mapService.ChooseMapFromMissionRunTasks(
                missionRun
            );

            if (missionRun.Tasks.Any())
            {
                missionRun.SetEstimatedTaskDuration();
            }

            if (existingMissionDefinition == null)
            {
                await missionDefinitionService.Create(scheduledMissionDefinition);
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

            return CreatedAtAction(nameof(Create), new { id = newMissionRun.Id }, newMissionRun);
        }

        /// <summary>
        ///     Schedule a custom mission
        /// </summary>
        /// <remarks>
        ///     <para> This query schedules a custom mission defined in the incoming json </para>
        /// </remarks>
        [HttpPost]
        [Authorize(Roles = Role.User)]
        [Route("custom")]
        [ProducesResponseType(typeof(MissionRun), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<MissionRun>> Create(
            [FromBody] CustomMissionQuery customMissionQuery
        )
        {
            customMissionQuery.InstallationCode = customMissionQuery.InstallationCode.ToUpper();

            Robot robot;
            try
            {
                robot = await robotService.GetRobotWithSchedulingPreCheck(
                    customMissionQuery.RobotId,
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

            var installation = await installationService.ReadByInstallationCode(
                customMissionQuery.InstallationCode,
                readOnly: true
            );
            if (installation == null)
            {
                return NotFound(
                    $"Could not find installation with name {customMissionQuery.InstallationCode}"
                );
            }

            var missionTasks = customMissionQuery
                .Tasks.Select(task => new MissionTask(task))
                .ToList();

            MissionDefinition? customMissionDefinition;
            try
            {
                var inspectionAreaForMission =
                    inspectionAreaService.TryFindInspectionAreaForMissionTasks(
                        missionTasks,
                        customMissionQuery.InstallationCode
                    );
                if (inspectionAreaForMission == null)
                {
                    return BadRequest("No inspection area found for the mission tasks");
                }

                if (robot.CurrentInspectionAreaId == null)
                {
                    return BadRequest("Robot does not have an inspection area");
                }

                if (inspectionAreaForMission.Id != robot.CurrentInspectionAreaId)
                {
                    return BadRequest(
                        $"The tasks of the mission are not inside the inspection area of the robot"
                    );
                }

                var source = await sourceService.CheckForExistingSourceFromTasks(missionTasks);

                MissionDefinition? existingMissionDefinition = null;
                if (source == null)
                {
                    source = await sourceService.CreateSourceIfDoesNotExist(missionTasks);
                }
                else
                {
                    var missionDefinition = await missionDefinitionService.ReadBySourceId(
                        source.SourceId,
                        readOnly: true
                    );
                    if (missionDefinition != null)
                    {
                        existingMissionDefinition = missionDefinition;
                    }
                }

                customMissionDefinition =
                    existingMissionDefinition
                    ?? new MissionDefinition
                    {
                        Id = Guid.NewGuid().ToString(),
                        Source = source,
                        Name = customMissionQuery.Name,
                        InspectionFrequency = customMissionQuery.InspectionFrequency,
                        InstallationCode = customMissionQuery.InstallationCode,
                        InspectionArea = inspectionAreaForMission,
                    };

                try
                {
                    customMissionDefinition.Map ??= await mapService.ChooseMapFromPositions(
                        [.. missionTasks.Select(t => t.RobotPose.Position)],
                        customMissionQuery.InstallationCode
                    );
                }
                catch (ArgumentOutOfRangeException)
                {
                    logger.LogWarning(
                        $"Could not find a suitable map for mission definition {customMissionDefinition.Id}"
                    );
                }

                if (existingMissionDefinition == null)
                {
                    await missionDefinitionService.Create(customMissionDefinition);
                }
            }
            catch (SourceException e)
            {
                return StatusCode(StatusCodes.Status502BadGateway, e.Message);
            }

            try
            {
                await installationService.AssertRobotIsOnSameInstallationAsMission(
                    robot,
                    customMissionDefinition
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

            MissionRun? newMissionRun;
            try
            {
                var scheduledMission = new MissionRun
                {
                    Name = customMissionQuery.Name,
                    Description = customMissionQuery.Description,
                    MissionId = customMissionDefinition.Id,
                    Comment = customMissionQuery.Comment,
                    Robot = robot,
                    Status = MissionStatus.Pending,
                    MissionRunType = MissionRunType.Normal,
                    DesiredStartTime = customMissionQuery.DesiredStartTime ?? DateTime.UtcNow,
                    Tasks = missionTasks,
                    InstallationCode = customMissionQuery.InstallationCode,
                    InspectionArea = customMissionDefinition.InspectionArea,
                };

                if (scheduledMission.Tasks.Any())
                {
                    scheduledMission.SetEstimatedTaskDuration();
                }

                newMissionRun = await missionRunService.Create(scheduledMission);
            }
            catch (MissionNotFoundException e)
            {
                return NotFound(e.Message);
            }
            catch (RobotNotFoundException e)
            {
                return NotFound(e.Message);
            }
            catch (UnsupportedRobotCapabilityException)
            {
                return BadRequest(
                    $"The robot {robot.Name} does not have the necessary sensors to run the mission."
                );
            }

            return CreatedAtAction(nameof(Create), new { id = newMissionRun.Id }, newMissionRun);
        }
    }
}
