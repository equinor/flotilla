using System.Text.Json;
using Api.Controllers.Models;
using Api.Database.Models;
using Api.Services;
using Api.Services.ActionServices;
using Api.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Api.Controllers
{
    [ApiController]
    [Route("missions")]
    public class MissionSchedulingController(
            IMissionDefinitionService missionDefinitionService,
            ICustomMissionSchedulingService customMissionSchedulingService,
            IMissionRunService missionRunService,
            IInstallationService installationService,
            IRobotService robotService,
            IEchoService echoService,
            ILogger<MissionSchedulingController> logger,
            IMapService mapService,
            IStidService stidService,
            ILocalizationService localizationService,
            ISourceService sourceService
        ) : ControllerBase
    {

        /// <summary>
        ///     Schedule an existing mission definition
        /// </summary>
        /// <remarks>
        ///     <para> This query schedules an existing mission and adds it to the database </para>
        /// </remarks>
        [HttpPost("schedule")]
        [Authorize(Roles = Role.User)]
        [ProducesResponseType(typeof(MissionRun), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<MissionRun>> Schedule(
            [FromBody] ScheduleMissionQuery scheduledMissionQuery
        )
        {
            var robot = await robotService.ReadById(scheduledMissionQuery.RobotId);
            if (robot is null) { return NotFound($"Could not find robot with id {scheduledMissionQuery.RobotId}"); }

            var missionDefinition = await missionDefinitionService.ReadById(scheduledMissionQuery.MissionDefinitionId);
            if (missionDefinition == null) { return NotFound("Mission definition not found"); }

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
                MissionRunPriority = MissionRunPriority.Normal,
                DesiredStartTime = scheduledMissionQuery.DesiredStartTime,
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

            var newMissionRun = await missionRunService.Create(missionRun);

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
        public async Task<ActionResult<MissionRun>> Create(
            [FromBody] ScheduledMissionQuery scheduledMissionQuery
        )
        {
            var robot = await robotService.ReadById(scheduledMissionQuery.RobotId);
            if (robot is null)
            {
                return NotFound($"Could not find robot with id {scheduledMissionQuery.RobotId}");
            }

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
                        return t.Inspections.Select(i => new MissionTask(t, i.InspectionPoint)).ToList();
                    }
                )
                .ToList();

            List<Area>? missionAreas;
            try
            {
                missionAreas = echoMission.Tags
                    .Select(t => stidService.GetTagArea(t.TagId, scheduledMissionQuery.InstallationCode).Result)
                    .ToList();
            }
            catch (AreaNotFoundException) { return NotFound("Area not found"); }

            Deck? missionDeck = null;
            foreach (var missionArea in missionAreas)
            {
                missionDeck ??= missionArea.Deck;

                if (missionDeck != missionArea.Deck) { return BadRequest("The mission spans multiple decks"); }
            }

            Area? area = null;
            area = missionAreas.GroupBy(i => i).OrderByDescending(grp => grp.Count()).Select(grp => grp.Key).First();

            if (area == null)
            {
                return NotFound($"No area with name {scheduledMissionQuery.AreaName} in installation {scheduledMissionQuery.InstallationCode} was found");
            }

            var source = await sourceService.CheckForExistingEchoSource(scheduledMissionQuery.EchoMissionId);
            MissionDefinition? existingMissionDefinition = null;
            if (source == null)
            {
                source = new Source
                {
                    SourceId = $"{echoMission.Id}",
                    Type = MissionSourceType.Echo
                };
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
                MissionRunPriority = MissionRunPriority.Normal,
                DesiredStartTime = scheduledMissionQuery.DesiredStartTime,
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

            var newMissionRun = await missionRunService.Create(missionRun);

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
            var robot = await robotService.ReadById(customMissionQuery.RobotId);
            if (robot is null) { return NotFound($"Could not find robot with id {customMissionQuery.RobotId}"); }

            var installation = await installationService.ReadByName(customMissionQuery.InstallationCode);
            if (installation == null) { return NotFound($"Could not find installation with name {customMissionQuery.InstallationCode}"); }

            var missionTasks = customMissionQuery.Tasks.Select(task => new MissionTask(task)).ToList();

            MissionDefinition? customMissionDefinition;
            try { customMissionDefinition = await customMissionSchedulingService.FindExistingOrCreateCustomMissionDefinition(customMissionQuery, missionTasks); }
            catch (SourceException e) { return StatusCode(StatusCodes.Status502BadGateway, e.Message); }
            catch (AreaNotFoundException) { return NotFound($"No area with name {customMissionQuery.AreaName} in installation {customMissionQuery.InstallationCode} was found"); }

            try { await localizationService.EnsureRobotIsOnSameInstallationAsMission(robot, customMissionDefinition); }
            catch (InstallationNotFoundException e) { return NotFound(e.Message); }
            catch (RobotNotInSameInstallationAsMissionException e) { return Conflict(e.Message); }

            MissionRun? newMissionRun;
            try { newMissionRun = await customMissionSchedulingService.QueueCustomMissionRun(customMissionQuery, customMissionDefinition.Id, robot.Id, missionTasks); }
            catch (Exception e) when (e is RobotNotFoundException or MissionNotFoundException) { return NotFound(e.Message); }

            return CreatedAtAction(nameof(Create), new
            {
                id = newMissionRun.Id
            }, newMissionRun);
        }
    }
}
