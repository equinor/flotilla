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
        IInspectionGroupService inspectionGroupService
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
            catch (Exception e) when (e is RobotNotFoundException)
            {
                return NotFound(e.Message);
            }
            catch (Exception e) when (e is RobotPreCheckFailedException)
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
                .Select((t) => new MissionTask(t, Database.Models.TaskStatus.NotStarted))
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
                Installation = missionRun.Installation,
                InspectionGroups = missionRun.InspectionGroups,
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
            catch (Exception e) when (e is RobotNotFoundException)
            {
                return NotFound(e.Message);
            }
            catch (Exception e) when (e is RobotPreCheckFailedException)
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
            else if (!missionDefinition.InspectionGroups.Any())
            {
                logger.LogInformation(
                    "Mission definition with ID {id} does not have an inspection group when scheduling",
                    missionDefinition.Id
                );
            }

            // TODO: Remove
            // try
            // {
            //     await installationService.EnsureRobotIsOnSameInstallationAsMission(
            //         robot,
            //         missionDefinition
            //     );
            // }
            // catch (InstallationNotFoundException e)
            // {
            //     return NotFound(e.Message);
            // }
            // catch (RobotNotInSameInstallationAsMissionException e)
            // {
            //     return Conflict(e.Message);
            // }

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
                Installation = missionDefinition.Installation,
                InspectionGroups = missionDefinition.InspectionGroups,
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
            catch (Exception e) when (e is RobotNotFoundException)
            {
                return NotFound(e.Message);
            }
            catch (Exception e) when (e is RobotPreCheckFailedException)
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
                var missionDefinitions = await missionDefinitionService.ReadBySourceId(
                    source.SourceId,
                    readOnly: true
                );
                if (missionDefinitions.Count > 0)
                {
                    existingMissionDefinition = missionDefinitions.First();
                }
            }
            var installation = await installationService.ReadByInstallationCode(
                scheduledMissionQuery.InstallationCode,
                readOnly: true
            );
            if (installation == null)
            {
                return NotFound(
                    $"Could not find installation with name {scheduledMissionQuery.InstallationCode}"
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
                    Installation = installation,
                    Map = new MapMetadata(),
                };

            if (!scheduledMissionDefinition.InspectionGroups.Any())
            {
                logger.LogInformation(
                    "Mission definition with ID {id} does not have an inspection group when scheduling",
                    scheduledMissionDefinition.Id
                );
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
                Installation = installation,
                InspectionGroups = scheduledMissionDefinition.InspectionGroups,
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
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<MissionRun>> Create(
            [FromBody] CustomMissionQuery customMissionQuery
        )
        {
            Robot robot;
            try
            {
                robot = await robotService.GetRobotWithSchedulingPreCheck(
                    customMissionQuery.RobotId,
                    readOnly: true
                );
            }
            catch (Exception e) when (e is RobotNotFoundException)
            {
                return NotFound(e.Message);
            }
            catch (Exception e) when (e is RobotPreCheckFailedException)
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
            InspectionGroup? inspectionGroup = null;
            try
            {
                if (customMissionQuery.InspectionGroupName != null)
                {
                    inspectionGroup = await inspectionGroupService.ReadByInstallationAndName(
                        customMissionQuery.InstallationCode,
                        customMissionQuery.InspectionGroupName,
                        readOnly: true
                    );
                }
                if (inspectionGroup == null)
                {
                    throw new InspectionGroupNotFoundException(
                        $"No inspection group with name {customMissionQuery.InspectionGroupName} in installation {customMissionQuery.InstallationCode} was found"
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
                    var missionDefinitions = await missionDefinitionService.ReadBySourceId(
                        source.SourceId,
                        readOnly: true
                    );
                    if (missionDefinitions.Count > 0)
                    {
                        existingMissionDefinition = missionDefinitions.First();
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
                        Installation = installation,
                        InspectionGroups = [inspectionGroup],
                    };

                customMissionDefinition.Map ??= await mapService.ChooseMapFromPositions(
                    [.. missionTasks.Select(t => t.RobotPose.Position)],
                    customMissionQuery.InstallationCode
                );

                if (existingMissionDefinition == null)
                {
                    await missionDefinitionService.Create(customMissionDefinition);
                }
            }
            catch (SourceException e)
            {
                return StatusCode(StatusCodes.Status502BadGateway, e.Message);
            }
            catch (InspectionGroupNotFoundException)
            {
                return NotFound(
                    $"No group with name {customMissionQuery.InspectionGroupName} in installation {customMissionQuery.InstallationCode} was found"
                );
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
                    Installation = installation,
                    InspectionGroups = [inspectionGroup],
                };

                if (scheduledMission.Tasks.Any())
                {
                    scheduledMission.SetEstimatedTaskDuration();
                }
                // TODO Probably remove
                // else if (
                //     scheduledMission.Robot.CurrentInspectionArea != null
                //     && !await localizationService.RobotIsOnSameInspectionAreaAsMission(
                //         scheduledMission.Robot.Id,
                //         scheduledMission.InspectionArea.Id
                //     )
                // )
                // {
                //     scheduledMission.SetEstimatedTaskDuration();
                // }

                // if (
                //     scheduledMission.Robot.CurrentInspectionArea != null
                //     && !await localizationService.RobotIsOnSameInspectionAreaAsMission(
                //         scheduledMission.Robot.Id,
                //         scheduledMission.InspectionArea.Id
                //     )
                // )
                // {
                //     return Conflict(
                //         $"The robot {scheduledMission.Robot.Name} is assumed to be in a different inspection area so the mission was not scheduled."
                //     );
                // }

                newMissionRun = await missionRunService.Create(scheduledMission);
            }
            catch (Exception e) when (e is UnsupportedRobotCapabilityException)
            {
                return BadRequest(e.Message);
            }
            catch (Exception e) when (e is MissionNotFoundException)
            {
                return NotFound(e.Message);
            }
            catch (Exception e) when (e is RobotNotFoundException)
            {
                return NotFound(e.Message);
            }
            catch (Exception e) when (e is UnsupportedRobotCapabilityException)
            {
                return BadRequest(
                    $"The robot {robot.Name} does not have the necessary sensors to run the mission."
                );
            }

            return CreatedAtAction(nameof(Create), new { id = newMissionRun.Id }, newMissionRun);
        }
    }
}
