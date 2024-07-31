using System.Text.Json;
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
            IInstallationService installationService,
            IEchoService echoService,
            ILogger<MissionSchedulingController> logger,
            IMapService mapService,
            IStidService stidService,
            ILocalizationService localizationService,
            IRobotService robotService,
            ISourceService sourceService,
            IAreaService areaService
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
            try { robot = await robotService.GetRobotWithPreCheck(scheduledMissionQuery.RobotId, readOnly: true); }
            catch (Exception e) when (e is RobotNotFoundException) { return NotFound(e.Message); }
            catch (Exception e) when (e is RobotPreCheckFailedException) { return BadRequest(e.Message); }

            var missionRun = await missionRunService.ReadByIdAsReadOnly(missionRunId);
            if (missionRun == null) return NotFound("Mission run not found");

            var missionTasks = missionRun.Tasks.Where((t) => t.Status != Database.Models.TaskStatus.Successful && t.Status != Database.Models.TaskStatus.PartiallySuccessful).Select((t) => new MissionTask(t, Database.Models.TaskStatus.NotStarted)).ToList();

            if (missionTasks == null || missionTasks.Count == 0) return NotFound("No unfinished mission tasks were found for the requested mission");

            foreach (var task in missionTasks)
            {
                task.Id = Guid.NewGuid().ToString();
                foreach (var inspection in task.Inspections)
                {
                    inspection.Id = Guid.NewGuid().ToString();
                }
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
                Area = missionRun.Area,
                Map = new MapMetadata(missionRun.Map)
            };

            if (newMissionRun.Tasks.Any())
            {
                newMissionRun.CalculateEstimatedDuration();
            }

            // Compare with GetTasksFromSource

            try
            {
                newMissionRun = await missionRunService.Create(newMissionRun);
            }
            catch (UnsupportedRobotCapabilityException)
            {
                return BadRequest($"The robot {robot.Name} does not have the necessary sensors to run the mission.");
            }

            return CreatedAtAction(nameof(Rerun), new
            {
                id = newMissionRun.Id
            }, newMissionRun);
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
            try { robot = await robotService.GetRobotWithPreCheck(scheduledMissionQuery.RobotId); }
            catch (Exception e) when (e is RobotNotFoundException) { return NotFound(e.Message); }
            catch (Exception e) when (e is RobotPreCheckFailedException) { return BadRequest(e.Message); }

            var missionDefinition = await missionDefinitionService.ReadById(missionDefinitionId);
            if (missionDefinition == null)
            {
                return NotFound("Mission definition not found");
            }

            try { await localizationService.EnsureRobotIsOnSameInstallationAsMission(robot, missionDefinition); }
            catch (InstallationNotFoundException e) { return NotFound(e.Message); }
            catch (RobotNotInSameInstallationAsMissionException e) { return Conflict(e.Message); }

            var missionTasks = await missionDefinitionService.GetTasksFromSource(missionDefinition.Source, missionDefinition.InstallationCode);
            if (missionTasks == null) return NotFound("No mission tasks were found for the requested mission");

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
                Area = missionDefinition.Area,
                Map = missionDefinition.Area?.MapMetadata ?? new MapMetadata()
            };

            await mapService.AssignMapToMission(missionRun);

            if (missionRun.Tasks.Any())
            {
                missionRun.CalculateEstimatedDuration();
            }

            MissionRun newMissionRun;
            try
            {
                newMissionRun = await missionRunService.Create(missionRun);
            }
            catch (UnsupportedRobotCapabilityException)
            {
                return BadRequest($"The robot {robot.Name} does not have the necessary sensors to run the mission.");
            }

            return CreatedAtAction(nameof(Schedule), new
            {
                id = newMissionRun.Id
            }, newMissionRun);
        }

        /// <summary>
        ///     Schedule a new echo mission
        /// </summary>
        /// <remarks>
        ///     <para> This query schedules a new echo mission and adds it to the database </para>
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
            try { robot = await robotService.GetRobotWithPreCheck(scheduledMissionQuery.RobotId); }
            catch (Exception e) when (e is RobotNotFoundException) { return NotFound(e.Message); }
            catch (Exception e) when (e is RobotPreCheckFailedException) { return BadRequest(e.Message); }

            EchoMission? echoMission;
            try
            {
                echoMission = await echoService.GetMissionById(scheduledMissionQuery.EchoMissionId);
            }
            catch (HttpRequestException e)
            {
                if (e.StatusCode.HasValue && (int)e.StatusCode.Value == 404)
                {
                    logger.LogWarning(
                        "Could not find echo mission with id={Id}",
                        scheduledMissionQuery.EchoMissionId
                    );
                    return NotFound("Echo mission not found");
                }

                logger.LogError(e, "Error getting mission from Echo");
                return StatusCode(StatusCodes.Status502BadGateway, $"{e.Message}");
            }
            catch (JsonException e)
            {
                const string Message = "Error deserializing mission from Echo";
                logger.LogError(e, "{Message}", Message);
                return StatusCode(StatusCodes.Status500InternalServerError, Message);
            }
            catch (InvalidDataException e)
            {
                const string Message =
                    "Can not schedule mission because EchoMission is invalid. One or more tasks does not contain a robot pose";
                logger.LogError(e, "Message: {errorMessage}", Message);
                return StatusCode(StatusCodes.Status502BadGateway, Message);
            }

            var missionTasks = echoMission.Tags
                .SelectMany(
                    t =>
                    {
                        return t.Inspections.Select(i => new MissionTask(t)).ToList();
                    }
                )
                .ToList();

            List<Area?> missionAreas;
            missionAreas = echoMission.Tags
                .Select(t => stidService.GetTagArea(t.TagId, scheduledMissionQuery.InstallationCode).Result)
                .ToList();

            var missionDeckNames = missionAreas.Where(a => a != null).Select(a => a!.Deck.Name).Distinct().ToList();
            if (missionDeckNames.Count > 1)
            {
                string joinedMissionDeckNames = string.Join(", ", [.. missionDeckNames]);
                logger.LogWarning($"Mission {echoMission.Name} has tags on more than one deck. The decks are: {joinedMissionDeckNames}.");
            }

            Area? area = null;
            area = missionAreas.GroupBy(i => i).OrderByDescending(grp => grp.Count()).Select(grp => grp.Key).First();

            if (area == null)
            {
                return NotFound($"No area found for echo mission '{echoMission.Name}'.");
            }

            var source = await sourceService.CheckForExistingEchoSource(scheduledMissionQuery.EchoMissionId);
            MissionDefinition? existingMissionDefinition = null;
            if (source == null)
            {
                source = await sourceService.Create(
                    new Source
                    {
                        SourceId = $"{echoMission.Id}",
                        Type = MissionSourceType.Echo
                    }
                );
            }
            else
            {
                var missionDefinitions = await missionDefinitionService.ReadBySourceId(source.SourceId);
                if (missionDefinitions.Count > 0)
                {
                    existingMissionDefinition = missionDefinitions.First();
                }
            }

            var scheduledMissionDefinition = existingMissionDefinition ?? new MissionDefinition
            {
                Id = Guid.NewGuid().ToString(),
                Source = source,
                Name = echoMission.Name,
                InspectionFrequency = scheduledMissionQuery.InspectionFrequency,
                InstallationCode = scheduledMissionQuery.InstallationCode,
                Area = area
            };

            var missionRun = new MissionRun
            {
                Name = echoMission.Name,
                Robot = robot,
                MissionId = scheduledMissionDefinition.Id,
                Status = MissionStatus.Pending,
                MissionRunType = MissionRunType.Normal,
                DesiredStartTime = scheduledMissionQuery.DesiredStartTime ?? DateTime.UtcNow,
                Tasks = missionTasks,
                InstallationCode = scheduledMissionQuery.InstallationCode,
                Area = area,
                Map = new MapMetadata()
            };

            await mapService.AssignMapToMission(missionRun);

            if (missionRun.Tasks.Any())
            {
                missionRun.CalculateEstimatedDuration();
            }

            if (existingMissionDefinition == null)
            {
                await missionDefinitionService.Create(scheduledMissionDefinition);
            }

            if (await localizationService.RobotIsLocalized(missionRun.Robot.Id) && !await localizationService.RobotIsOnSameDeckAsMission(missionRun.Robot.Id, missionRun.Area.Id))
            {
                return Conflict($"The robot {missionRun.Robot.Name} is localized on a different deck so the mission was not scheduled.");
            }

            MissionRun newMissionRun;
            try
            {
                newMissionRun = await missionRunService.Create(missionRun);
            }
            catch (UnsupportedRobotCapabilityException)
            {
                return BadRequest($"The robot {robot.Name} does not have the necessary sensors to run the mission.");
            }

            return CreatedAtAction(nameof(Create), new
            {
                id = newMissionRun.Id
            }, newMissionRun);
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
            Robot robot;
            try { robot = await robotService.GetRobotWithPreCheck(customMissionQuery.RobotId); }
            catch (Exception e) when (e is RobotNotFoundException) { return NotFound(e.Message); }
            catch (Exception e) when (e is RobotPreCheckFailedException) { return BadRequest(e.Message); }

            var installation = await installationService.ReadByName(customMissionQuery.InstallationCode);
            if (installation == null) { return NotFound($"Could not find installation with name {customMissionQuery.InstallationCode}"); }

            var missionTasks = customMissionQuery.Tasks.Select(task => new MissionTask(task)).ToList();

            MissionDefinition? customMissionDefinition;
            try
            {
                Area? area = null;
                if (customMissionQuery.AreaName != null) { area = await areaService.ReadByInstallationAndName(customMissionQuery.InstallationCode, customMissionQuery.AreaName, readOnly: false); }

                if (area == null)
                {
                    throw new AreaNotFoundException($"No area with name {customMissionQuery.AreaName} in installation {customMissionQuery.InstallationCode} was found");
                }

                var source = await sourceService.CheckForExistingCustomSource(missionTasks);

                MissionDefinition? existingMissionDefinition = null;
                if (source == null)
                {
                    source = await sourceService.CreateSourceIfDoesNotExist(missionTasks);
                }
                else
                {
                    var missionDefinitions = await missionDefinitionService.ReadBySourceId(source.SourceId);
                    if (missionDefinitions.Count > 0) { existingMissionDefinition = missionDefinitions.First(); }
                }

                customMissionDefinition = existingMissionDefinition ?? new MissionDefinition
                {
                    Id = Guid.NewGuid().ToString(),
                    Source = source,
                    Name = customMissionQuery.Name,
                    InspectionFrequency = customMissionQuery.InspectionFrequency,
                    InstallationCode = customMissionQuery.InstallationCode,
                    Area = area
                };

                if (existingMissionDefinition == null) { await missionDefinitionService.Create(customMissionDefinition); }
            }
            catch (SourceException e) { return StatusCode(StatusCodes.Status502BadGateway, e.Message); }
            catch (AreaNotFoundException) { return NotFound($"No area with name {customMissionQuery.AreaName} in installation {customMissionQuery.InstallationCode} was found"); }

            try { await localizationService.EnsureRobotIsOnSameInstallationAsMission(robot, customMissionDefinition); }
            catch (InstallationNotFoundException e) { return NotFound(e.Message); }
            catch (RobotNotInSameInstallationAsMissionException e) { return Conflict(e.Message); }

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
                    Area = customMissionDefinition.Area,
                    Map = new MapMetadata()
                };

                await mapService.AssignMapToMission(scheduledMission);

                if (scheduledMission.Tasks.Any()) { scheduledMission.CalculateEstimatedDuration(); }

                newMissionRun = await missionRunService.Create(scheduledMission);
            }
            catch (Exception e) when (e is UnsupportedRobotCapabilityException) { return BadRequest(e.Message); }
            catch (Exception e) when (e is MissionNotFoundException) { return NotFound(e.Message); }
            catch (Exception e) when (e is RobotNotFoundException) { return NotFound(e.Message); }
            catch (Exception e) when (e is UnsupportedRobotCapabilityException) { return BadRequest($"The robot {robot.Name} does not have the necessary sensors to run the mission."); }

            return CreatedAtAction(nameof(Create), new
            {
                id = newMissionRun.Id
            }, newMissionRun);
        }
    }
}
