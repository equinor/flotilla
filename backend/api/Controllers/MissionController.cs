﻿using System.Text.Json;
using Api.Controllers.Models;
using Api.Database.Models;
using Api.Services;
using Api.Utilities;
using Azure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Api.Controllers
{
    [ApiController]
    [Route("missions")]
    public class MissionController : ControllerBase
    {
        private readonly IAreaService _areaService;
        private readonly ICustomMissionService _customMissionService;
        private readonly IEchoService _echoService;
        private readonly ILogger<MissionController> _logger;
        private readonly IMapService _mapService;
        private readonly IMissionDefinitionService _missionDefinitionService;
        private readonly IMissionRunService _missionRunService;
        private readonly IRobotService _robotService;
        private readonly ISourceService _sourceService;
        private readonly IStidService _stidService;

        public MissionController(
            IMissionDefinitionService missionDefinitionService,
            IMissionRunService missionRunService,
            IAreaService areaService,
            IRobotService robotService,
            IEchoService echoService,
            ICustomMissionService customMissionService,
            ILogger<MissionController> logger,
            IMapService mapService,
            IStidService stidService,
            ISourceService sourceService
        )
        {
            _missionDefinitionService = missionDefinitionService;
            _missionRunService = missionRunService;
            _areaService = areaService;
            _robotService = robotService;
            _echoService = echoService;
            _customMissionService = customMissionService;
            _mapService = mapService;
            _stidService = stidService;
            _sourceService = sourceService;
            _logger = logger;
        }

        /// <summary>
        ///     List all mission runs in the Flotilla database
        /// </summary>
        /// <remarks>
        ///     <para> This query gets all mission runs </para>
        /// </remarks>
        [HttpGet("runs")]
        [Authorize(Roles = Role.Any)]
        [ProducesResponseType(typeof(IList<MissionRun>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IList<MissionRun>>> GetMissionRuns(
            [FromQuery] MissionRunQueryStringParameters parameters
        )
        {
            if (parameters.MaxDesiredStartTime < parameters.MinDesiredStartTime)
            {
                return BadRequest("Max DesiredStartTime cannot be less than min DesiredStartTime");
            }
            if (parameters.MaxStartTime < parameters.MinStartTime)
            {
                return BadRequest("Max StartTime cannot be less than min StartTime");
            }
            if (parameters.MaxEndTime < parameters.MinEndTime)
            {
                return BadRequest("Max EndTime cannot be less than min EndTime");
            }

            PagedList<MissionRun> missionRuns;
            try
            {
                missionRuns = await _missionRunService.ReadAll(parameters);
            }
            catch (InvalidDataException e)
            {
                _logger.LogError(e.Message);
                return BadRequest(e.Message);
            }

            var metadata = new
            {
                missionRuns.TotalCount,
                missionRuns.PageSize,
                missionRuns.CurrentPage,
                missionRuns.TotalPages,
                missionRuns.HasNext,
                missionRuns.HasPrevious
            };

            Response.Headers.Add(
                QueryStringParameters.PaginationHeader,
                JsonSerializer.Serialize(metadata)
            );

            return Ok(missionRuns);
        }

        /// <summary>
        /// List all mission definitions in the Flotilla database
        /// </summary>
        /// <remarks>
        /// <para> This query gets all mission definitions </para>
        /// </remarks>
        [HttpGet("definitions")]
        [Authorize(Roles = Role.Any)]
        [ProducesResponseType(typeof(IList<MissionDefinitionResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IList<MissionDefinitionResponse>>> GetMissionDefinitions(
            [FromQuery] MissionDefinitionQueryStringParameters parameters
        )
        {
            PagedList<MissionDefinition> missionDefinitions;
            try
            {
                missionDefinitions = await _missionDefinitionService.ReadAll(parameters);
            }
            catch (InvalidDataException e)
            {
                _logger.LogError(e.Message);
                return BadRequest(e.Message);
            }

            var metadata = new
            {
                missionDefinitions.TotalCount,
                missionDefinitions.PageSize,
                missionDefinitions.CurrentPage,
                missionDefinitions.TotalPages,
                missionDefinitions.HasNext,
                missionDefinitions.HasPrevious
            };

            Response.Headers.Add(
                QueryStringParameters.PaginationHeader,
                JsonSerializer.Serialize(metadata)
            );

            var missionDefinitionResponses = missionDefinitions.Select(m => new MissionDefinitionResponse(_missionDefinitionService, m));
            return Ok(missionDefinitionResponses);
        }

        /// <summary>
        ///     Lookup mission run by specified id.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = Role.Any)]
        [Route("runs/{id}")]
        [ProducesResponseType(typeof(MissionRun), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<MissionRun>> GetMissionRunById([FromRoute] string id)
        {
            var missioRun = await _missionRunService.ReadById(id);
            if (missioRun == null)
            {
                return NotFound($"Could not find mission run with id {id}");
            }
            return Ok(missioRun);
        }

        /// <summary>
        /// Lookup mission definition by specified id.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = Role.Any)]
        [Route("definitions/{id}/condensed")]
        [ProducesResponseType(typeof(CondensedMissionDefinitionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<CondensedMissionDefinitionResponse>> GetCondensedMissionDefinitionById([FromRoute] string id)
        {
            var missionDefinition = await _missionDefinitionService.ReadById(id);
            if (missionDefinition == null)
                return NotFound($"Could not find mission definition with id {id}");
            var missionDefinitionResponse = new CondensedMissionDefinitionResponse(missionDefinition);
            return Ok(missionDefinitionResponse);
        }

        /// <summary>
        /// Lookup mission definition by specified id.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = Role.Any)]
        [Route("definitions/{id}")]
        [ProducesResponseType(typeof(MissionDefinitionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<MissionDefinitionResponse>> GetMissionDefinitionById([FromRoute] string id)
        {
            var missionDefinition = await _missionDefinitionService.ReadById(id);
            if (missionDefinition == null)
                return NotFound($"Could not find mission definition with id {id}");
            var missionDefinitionResponse = new MissionDefinitionResponse(_missionDefinitionService, missionDefinition);
            return Ok(missionDefinitionResponse);
        }

        /// <summary>
        ///     Lookup which mission run is scheduled next for the given mission definition
        /// </summary>
        [HttpGet]
        [Authorize(Roles = Role.Any)]
        [Route("definitions/{id}/next-run")]
        [ProducesResponseType(typeof(MissionRun), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<MissionRun>> GetNextMissionRun([FromRoute] string id)
        {
            var missionDefinition = await _missionDefinitionService.ReadById(id);
            if (missionDefinition == null)
            {
                return NotFound($"Could not find mission definition with id {id}");
            }
            var nextRun = await _missionRunService.ReadNextScheduledRunByMissionId(id);
            return Ok(nextRun);
        }

        /// <summary>
        ///     Get map for mission with specified id.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = Role.Any)]
        [Route("{installationCode}/{mapName}/map")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status502BadGateway)]
        public async Task<ActionResult<byte[]>> GetMap([FromRoute] string installationCode, string mapName)
        {
            try
            {
                byte[] mapStream = await _mapService.FetchMapImage(mapName, installationCode);
                return File(mapStream, "image/png");
            }
            catch (RequestFailedException)
            {
                return NotFound("Could not find map for this area.");
            }
        }

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
            var robot = await _robotService.ReadById(scheduledMissionQuery.RobotId);
            if (robot is null)
            {
                return NotFound($"Could not find robot with id {scheduledMissionQuery.RobotId}");
            }

            var missionDefinition = await _missionDefinitionService.ReadById(scheduledMissionQuery.MissionDefinitionId);
            if (missionDefinition == null)
            {
                return NotFound("Mission definition not found");
            }

            List<MissionTask>? missionTasks;
            missionTasks = await _missionDefinitionService.GetTasksFromSource(missionDefinition.Source, missionDefinition.InstallationCode);

            if (missionTasks == null)
            {
                return NotFound("No mission tasks were found for the requested mission");
            }

            var missionRun = new MissionRun
            {
                Name = missionDefinition.Name,
                Robot = robot,
                MissionId = missionDefinition.Id,
                Status = MissionStatus.Pending,
                DesiredStartTime = scheduledMissionQuery.DesiredStartTime,
                Tasks = missionTasks,
                InstallationCode = missionDefinition.InstallationCode,
                Area = missionDefinition.Area,
                Map = new MapMetadata()
            };

            await _mapService.AssignMapToMission(missionRun);

            if (missionRun.Tasks.Any())
            {
                missionRun.CalculateEstimatedDuration();
            }

            var newMissionRun = await _missionRunService.Create(missionRun);

            return CreatedAtAction(nameof(GetMissionRunById), new
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
            var robot = await _robotService.ReadById(scheduledMissionQuery.RobotId);
            if (robot is null)
            {
                return NotFound($"Could not find robot with id {scheduledMissionQuery.RobotId}");
            }

            EchoMission? echoMission;
            try
            {
                echoMission = await _echoService.GetMissionById(scheduledMissionQuery.EchoMissionId);
            }
            catch (HttpRequestException e)
            {
                if (e.StatusCode.HasValue && (int)e.StatusCode.Value == 404)
                {
                    _logger.LogWarning(
                        "Could not find echo mission with id={id}",
                        scheduledMissionQuery.EchoMissionId
                    );
                    return NotFound("Echo mission not found");
                }

                _logger.LogError(e, "Error getting mission from Echo");
                return StatusCode(StatusCodes.Status502BadGateway, $"{e.Message}");
            }
            catch (JsonException e)
            {
                string message = "Error deserializing mission from Echo";
                _logger.LogError(e, "{message}", message);
                return StatusCode(StatusCodes.Status500InternalServerError, message);
            }
            catch (InvalidDataException e)
            {
                string message =
                    "Can not schedule mission because EchoMission is invalid. One or more tasks does not contain a robot pose.";
                _logger.LogError(e, message);
                return StatusCode(StatusCodes.Status502BadGateway, message);
            }

            var missionTasks = echoMission.Tags
                .Select(
                    t =>
                    {
                        var tagPosition = _stidService
                            .GetTagPosition(t.TagId, scheduledMissionQuery.InstallationCode)
                            .Result;
                        return new MissionTask(t, tagPosition);
                    }
                )
                .ToList();

            Area? area = null;
            if (scheduledMissionQuery.AreaName != null)
            {
                area = await _areaService.ReadByInstallationAndName(scheduledMissionQuery.InstallationCode, scheduledMissionQuery.AreaName);
            }

            var source = await _sourceService.CheckForExistingEchoSource(scheduledMissionQuery.EchoMissionId);
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
                var missionDefinitions = await _missionDefinitionService.ReadBySourceId(source.SourceId);
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
                DesiredStartTime = scheduledMissionQuery.DesiredStartTime,
                Tasks = missionTasks,
                InstallationCode = scheduledMissionQuery.InstallationCode,
                Area = area,
                Map = new MapMetadata()
            };

            await _mapService.AssignMapToMission(missionRun);

            if (missionRun.Tasks.Any())
            {
                missionRun.CalculateEstimatedDuration();
            }

            if (existingMissionDefinition == null)
            {
                await _missionDefinitionService.Create(scheduledMissionDefinition);
            }

            var newMissionRun = await _missionRunService.Create(missionRun);

            return CreatedAtAction(nameof(GetMissionRunById), new
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
            var robot = await _robotService.ReadById(customMissionQuery.RobotId);
            if (robot is null)
            {
                return NotFound($"Could not find robot with id {customMissionQuery.RobotId}");
            }

            var installationResults = await _echoService.GetEchoPlantInfos();
            if (installationResults == null)
            {
                return NotFound("Unable to retrieve plant information from Echo");
            }

            var installationResult = installationResults
                .Where(
                    installation => installation.PlantCode.ToUpperInvariant() == customMissionQuery.InstallationCode.ToUpperInvariant()
                ).FirstOrDefault();
            if (installationResult == null)
            {
                return NotFound($"Could not find installation with id {customMissionQuery.InstallationCode}");
            }

            var missionTasks = customMissionQuery.Tasks.Select(task => new MissionTask(task)).ToList();

            Area? area = null;
            if (customMissionQuery.AreaName != null)
            {
                area = await _areaService.ReadByInstallationAndName(customMissionQuery.InstallationCode, customMissionQuery.AreaName);
            }

            var source = await _sourceService.CheckForExistingCustomSource(missionTasks);
            MissionDefinition? existingMissionDefinition = null;
            if (source == null)
            {
                string sourceURL = _customMissionService.UploadSource(missionTasks);
                source = new Source
                {
                    SourceId = sourceURL,
                    Type = MissionSourceType.Custom
                };
            }
            else
            {
                var missionDefinitions = await _missionDefinitionService.ReadBySourceId(source.SourceId);
                if (missionDefinitions.Count > 0)
                {
                    existingMissionDefinition = missionDefinitions.First();
                }
            }

            var customMissionDefinition = existingMissionDefinition ?? new MissionDefinition
            {
                Id = Guid.NewGuid().ToString(),
                Source = source,
                Name = customMissionQuery.Name,
                InspectionFrequency = customMissionQuery.InspectionFrequency,
                InstallationCode = customMissionQuery.InstallationCode,
                Area = area
            };

            var scheduledMission = new MissionRun
            {
                Name = customMissionQuery.Name,
                Description = customMissionQuery.Description,
                MissionId = customMissionDefinition.Id,
                Comment = customMissionQuery.Comment,
                Robot = robot,
                Status = MissionStatus.Pending,
                DesiredStartTime = customMissionQuery.DesiredStartTime ?? DateTimeOffset.UtcNow,
                Tasks = missionTasks,
                InstallationCode = customMissionQuery.InstallationCode,
                Area = area,
                Map = new MapMetadata()
            };

            await _mapService.AssignMapToMission(scheduledMission);

            if (scheduledMission.Tasks.Any())
            {
                scheduledMission.CalculateEstimatedDuration();
            }

            if (existingMissionDefinition == null)
            {
                await _missionDefinitionService.Create(customMissionDefinition);
            }

            var newMissionRun = await _missionRunService.Create(scheduledMission);

            return CreatedAtAction(nameof(GetMissionRunById), new
            {
                id = newMissionRun.Id
            }, newMissionRun);
        }

        /// <summary>
        /// Updates a mission definition in the database based on id
        /// </summary>
        /// <response code="200"> The mission definition was successfully updated </response>
        /// <response code="400"> The mission definition data is invalid </response>
        /// <response code="404"> There was no mission definition with the given ID in the database </response>
        [HttpPut]
        [Authorize(Roles = Role.Admin)]
        [Route("definitions/{id}")]
        [ProducesResponseType(typeof(CondensedMissionDefinitionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<CondensedMissionDefinitionResponse>> UpdateMissionDefinitionById(
            [FromRoute] string id,
            [FromBody] UpdateMissionDefinitionQuery missionDefinitionQuery
        )
        {
            _logger.LogInformation("Updating mission definition with id '{id}'", id);

            if (!ModelState.IsValid)
                return BadRequest("Invalid data.");

            var missionDefinition = await _missionDefinitionService.ReadById(id);
            if (missionDefinition == null)
                return NotFound($"Could not find mission definition with id '{id}'");

            if (missionDefinitionQuery.Name == null)
                return BadRequest("Name cannot be null.");

            missionDefinition.Name = missionDefinitionQuery.Name;
            missionDefinition.Comment = missionDefinitionQuery.Comment;
            missionDefinition.InspectionFrequency = missionDefinitionQuery.InspectionFrequency;

            var newMissionDefinition = await _missionDefinitionService.Update(missionDefinition);
            return new CondensedMissionDefinitionResponse(newMissionDefinition);
        }

        /// <summary>
        /// Deletes the mission definition with the specified id from the database.
        /// </summary>
        [HttpDelete]
        [Authorize(Roles = Role.Admin)]
        [Route("definitions/{id}")]
        [ProducesResponseType(typeof(MissionDefinitionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<MissionDefinitionResponse>> DeleteMissionDefinition([FromRoute] string id)
        {
            var missionDefinition = await _missionDefinitionService.Delete(id);
            if (missionDefinition is null)
                return NotFound($"Mission definition with id {id} not found");
            var missionDefinitionResponse = new MissionDefinitionResponse(_missionDefinitionService, missionDefinition);
            return Ok(missionDefinitionResponse);
        }

        /// <summary>
        ///     Deletes the mission run with the specified id from the database.
        /// </summary>
        [HttpDelete]
        [Authorize(Roles = Role.Admin)]
        [Route("runs/{id}")]
        [ProducesResponseType(typeof(MissionRun), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<MissionRun>> DeleteMissionRun([FromRoute] string id)
        {
            var missionRun = await _missionRunService.Delete(id);
            if (missionRun is null)
            {
                return NotFound($"Mission run with id {id} not found");
            }
            return Ok(missionRun);
        }
    }
}
