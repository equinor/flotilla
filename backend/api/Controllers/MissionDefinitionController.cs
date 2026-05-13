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
    [Route("missions/definitions")]
    public class MissionDefinitionController(
        ILogger<MissionDefinitionController> logger,
        IMissionDefinitionService missionDefinitionService,
        IInstallationService installationService,
        IAutoScheduleService autoScheduleService,
        IInspectionAreaService inspectionAreaService
    ) : ControllerBase
    {
        /// <summary>
        ///     List all mission definitions in the Flotilla database
        /// </summary>
        /// <remarks>
        ///     <para> This query gets all mission definitions </para>
        /// </remarks>
        [HttpGet("")]
        [Authorize(Roles = Role.Any)]
        [ProducesResponseType(
            typeof(IEnumerable<MissionDefinitionResponse>),
            StatusCodes.Status200OK
        )]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<
            ActionResult<IEnumerable<MissionDefinitionResponse>>
        > GetMissionDefinitions([FromQuery] MissionDefinitionQueryStringParameters parameters)
        {
            PagedList<MissionDefinition> missionDefinitions;
            try
            {
                missionDefinitions = await missionDefinitionService.ReadAll(
                    parameters,
                    readOnly: true
                );
            }
            catch (InvalidDataException e)
            {
                logger.LogError(e, "{ErrorMessage}", e.Message);
                return BadRequest(e.Message);
            }

            var metadata = new
            {
                missionDefinitions.TotalCount,
                missionDefinitions.PageSize,
                missionDefinitions.CurrentPage,
                missionDefinitions.TotalPages,
                missionDefinitions.HasNext,
                missionDefinitions.HasPrevious,
            };

            Response.Headers.Append(
                QueryStringParameters.PaginationHeader,
                JsonSerializer.Serialize(metadata)
            );

            var missionDefinitionResponses = missionDefinitions.Select(
                m => new MissionDefinitionResponse(m)
            );
            return Ok(missionDefinitionResponses);
        }

        /// <summary>
        ///     Lookup mission definition by specified id.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = Role.Any)]
        [Route("{id}")]
        [ProducesResponseType(typeof(MissionDefinitionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<
            ActionResult<MissionDefinitionResponse>
        > GetMissionDefinitionWithTasksById([FromRoute] string id)
        {
            var missionDefinition = await missionDefinitionService.ReadById(id, readOnly: true);
            if (missionDefinition == null)
            {
                return NotFound($"Could not find mission definition with id {id}");
            }
            var missionDefinitionResponse = new MissionDefinitionResponse(missionDefinition);
            return Ok(missionDefinitionResponse);
        }

        /// <summary>
        ///     List all available missions for the installation
        /// </summary>
        /// <remarks>
        ///     These missions are fetched based on your mission loader
        /// </remarks>
        [HttpGet]
        [Route("installation/{installationCode}")]
        [Authorize(Roles = Role.Any)]
        [ProducesResponseType(typeof(IList<MissionDefinitionResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status502BadGateway)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<ActionResult<IList<MissionDefinitionResponse>>> GetAvailableMissions(
            [FromRoute] string installationCode
        )
        {
            IQueryable<MissionDefinition> missionDefinitions;
            try
            {
                missionDefinitions = await missionDefinitionService.ReadByInstallationCode(
                    installationCode
                );
            }
            catch (InvalidDataException e)
            {
                logger.LogError(e, "{ErrorMessage}", e.Message);
                return BadRequest(e.Message);
            }
            catch (HttpRequestException e)
            {
                logger.LogError(e, "Error retrieving missions from Mission Loader");
                return StatusCode(StatusCodes.Status502BadGateway);
            }
            catch (JsonException e)
            {
                logger.LogError(e, "Error retrieving missions from database");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            return Ok(missionDefinitions.Select((m) => new MissionDefinitionResponse(m)));
        }

        /// <summary>
        ///     Create a new mission definition
        /// </summary>
        /// <remarks>
        ///     <para> This query creates a new mission definition </para>
        /// </remarks>
        [HttpPost]
        [Authorize(Roles = Role.User)]
        [ProducesResponseType(typeof(MissionDefinition), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<MissionDefinition>> Create(
            [FromBody] MissionQuery customMissionQuery
        )
        {
            customMissionQuery = Sanitize.SanitizeUserInput(customMissionQuery);

            customMissionQuery.InstallationCode = customMissionQuery.InstallationCode.ToUpper();

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
                .Tasks.Select((task, index) => new TaskDefinition(task, index))
                .ToList();

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

                var newMissionDefinition = new MissionDefinition
                {
                    Id = Guid.NewGuid().ToString(),
                    Tasks = missionTasks,
                    Name = customMissionQuery.Name,
                    InstallationCode = customMissionQuery.InstallationCode,
                    InspectionArea = inspectionAreaForMission,
                };
                await missionDefinitionService.Create(newMissionDefinition);
                return Ok(newMissionDefinition);
            }
            catch (MultipleInspectionAreasFoundException e)
            {
                return BadRequest(e.Message);
            }
        }

        /// <summary>
        ///     Updates a mission definition in the database based on id
        /// </summary>
        /// <response code="200"> The mission definition was successfully updated </response>
        /// <response code="400"> The mission definition data is invalid </response>
        /// <response code="404"> There was no mission definition with the given ID in the database </response>
        [HttpPut]
        [Authorize(Roles = Role.User)]
        [Route("{id}")]
        [ProducesResponseType(typeof(MissionDefinitionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<MissionDefinitionResponse>> UpdateMissionDefinitionById(
            [FromRoute] string id,
            [FromBody] UpdateMissionDefinitionQuery missionDefinitionQuery
        )
        {
            id = Sanitize.SanitizeUserInput(id);

            logger.LogInformation("Updating mission definition with id '{Id}'", id);

            if (!ModelState.IsValid)
            {
                return BadRequest("Invalid data.");
            }

            var missionDefinition = await missionDefinitionService.ReadById(id, readOnly: false);
            if (missionDefinition == null)
            {
                return NotFound($"Could not find mission definition with id '{id}'");
            }

            if (missionDefinitionQuery.Name == null)
            {
                return BadRequest("Name cannot be null.");
            }

            missionDefinition.Name = missionDefinitionQuery.Name;
            missionDefinition.Comment = missionDefinitionQuery.Comment;
            missionDefinition.InspectionFrequency = missionDefinitionQuery.InspectionFrequency;
            if (missionDefinitionQuery.SchedulingTimesCETperWeek != null)
            {
                var schedulingTimesCETperWeek = missionDefinitionQuery
                    .SchedulingTimesCETperWeek.Select(s => new TimeAndDay(s.DayOfWeek, s.TimeOfDay))
                    .ToList();
                missionDefinition.AutoScheduleFrequency =
                    await autoScheduleService.UpdateAutoScheduleFrequency(
                        missionDefinition,
                        schedulingTimesCETperWeek
                    );
            }

            var newMissionDefinition = await missionDefinitionService.Update(missionDefinition);
            return new MissionDefinitionResponse(newMissionDefinition);
        }

        /// <summary>
        ///     Updates a mission definition's IsDeprecated in the database based on id
        /// </summary>
        /// <response code="200"> The mission definition was successfully updated </response>
        /// <response code="400"> The mission definition data is invalid </response>
        /// <response code="404"> There was no mission definition with the given ID in the database </response>
        [HttpPut]
        [Authorize(Roles = Role.Admin)]
        [Route("{id}/is-deprecated")]
        [ProducesResponseType(typeof(MissionDefinitionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<
            ActionResult<MissionDefinitionResponse>
        > UpdateMissionDefinitionIsDeprecatedById(
            [FromRoute] string id,
            [FromBody] UpdateMissionDefinitionIsDeprecatedQuery missionDefinitionIsDeprecatedQuery
        )
        {
            id = Sanitize.SanitizeUserInput(id);

            logger.LogInformation(
                "Updating mission definition IsDeprected value for id '{Id}'",
                id
            );

            if (!ModelState.IsValid)
            {
                return BadRequest("Invalid data.");
            }

            var missionDefinition = await missionDefinitionService.ReadById(id, readOnly: true);
            if (missionDefinition == null)
            {
                return NotFound($"Could not find mission definition with id '{id}'");
            }
            missionDefinition.IsDeprecated = missionDefinitionIsDeprecatedQuery.IsDeprecated;

            var newMissionDefinition = await missionDefinitionService.Update(missionDefinition);
            return new MissionDefinitionResponse(newMissionDefinition);
        }

        /// <summary>
        ///     Deprecates the mission definition with the specified id.
        /// </summary>
        [HttpDelete]
        [Authorize(Roles = Role.Admin)]
        [Route("{id}")]
        [ProducesResponseType(typeof(MissionDefinitionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<MissionDefinitionResponse>> DeleteMissionDefinition(
            [FromRoute] string id
        )
        {
            id = Sanitize.SanitizeUserInput(id);

            logger.LogInformation("Deprecating mission definition with id '{Id}'", id);
            var missionDefinition = await missionDefinitionService.Delete(id);
            if (missionDefinition is null)
            {
                return NotFound($"Mission definition with id {id} not found");
            }
            var missionDefinitionResponse = new MissionDefinitionResponse(missionDefinition);
            return Ok(missionDefinitionResponse);
        }

        /// <summary>
        ///     Skip auto mission run for the given mission definition at the given time of day
        /// </summary>
        [HttpPut]
        [Authorize(Roles = Role.User)]
        [Route("{missionDefinitionId}/skip-auto-mission")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> SkipAutoScheduledMissionRun(
            [FromRoute] string missionDefinitionId,
            [FromBody] SkipAutoMissionQuery skipAutoMissionQuery
        )
        {
            missionDefinitionId = Sanitize.SanitizeUserInput(missionDefinitionId);
            skipAutoMissionQuery = Sanitize.SanitizeUserInput(skipAutoMissionQuery);

            var missionDefinition = await missionDefinitionService.ReadById(
                missionDefinitionId,
                readOnly: true
            );
            if (missionDefinition == null)
            {
                return NotFound(
                    $"Could not find mission definition with id '{missionDefinitionId}'"
                );
            }

            try
            {
                await autoScheduleService.SkipAutoMissionScheduledJob(
                    missionDefinition,
                    skipAutoMissionQuery.TimeOfDay
                );
            }
            catch (ArgumentException e)
            {
                return BadRequest(e.Message);
            }
            return NoContent();
        }
    }
}
