﻿using System.Text.Json;
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
    public class MissionDefinitionController(ILogger<MissionDefinitionController> logger, IMissionDefinitionService missionDefinitionService, IMissionRunService missionRunService) : ControllerBase
    {
        /// <summary>
        ///     List all mission definitions in the Flotilla database
        /// </summary>
        /// <remarks>
        ///     <para> This query gets all mission definitions </para>
        /// </remarks>
        [HttpGet("")]
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
                missionDefinitions = await missionDefinitionService.ReadAll(parameters, readOnly: true);
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
                missionDefinitions.HasPrevious
            };

            Response.Headers.Append(
                QueryStringParameters.PaginationHeader,
                JsonSerializer.Serialize(metadata)
            );

            var missionDefinitionResponses = missionDefinitions.Select(m => new MissionDefinitionResponse(m));
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
        public async Task<ActionResult<MissionDefinitionResponse>> GetMissionDefinitionWithTasksById([FromRoute] string id)
        {
            var missionDefinition = await missionDefinitionService.ReadById(id, readOnly: true);
            if (missionDefinition == null)
            {
                return NotFound($"Could not find mission definition with id {id}");
            }
            var missionDefinitionResponse = new MissionDefinitionWithTasksResponse(missionDefinitionService, missionDefinition);
            return Ok(missionDefinitionResponse);
        }

        /// <summary>
        ///     Lookup which mission run is scheduled next for the given mission definition
        /// </summary>
        [HttpGet]
        [Authorize(Roles = Role.Any)]
        [Route("{id}/next-run")]
        [ProducesResponseType(typeof(MissionRun), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<MissionRun>> GetNextMissionRun([FromRoute] string id)
        {
            var missionDefinition = await missionDefinitionService.ReadById(id, readOnly: true);
            if (missionDefinition == null)
            {
                return NotFound($"Could not find mission definition with id {id}");
            }
            var nextRun = await missionRunService.ReadNextScheduledRunByMissionId(id, readOnly: true);
            return Ok(nextRun);
        }

        /// <summary>
        ///     Updates a mission definition in the database based on id
        /// </summary>
        /// <response code="200"> The mission definition was successfully updated </response>
        /// <response code="400"> The mission definition data is invalid </response>
        /// <response code="404"> There was no mission definition with the given ID in the database </response>
        [HttpPut]
        [Authorize(Roles = Role.Admin)]
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
        public async Task<ActionResult<MissionDefinitionResponse>> UpdateMissionDefinitionIsDeprecatedById(
            [FromRoute] string id,
            [FromBody] UpdateMissionDefinitionIsDeprecatedQuery missionDefinitionIsDeprecatedQuery
        )
        {
            logger.LogInformation("Updating mission definition IsDeprected value for id '{Id}'", id);

            if (!ModelState.IsValid)
            {
                return BadRequest("Invalid data.");
            }

            var missionDefinition = await missionDefinitionService.ReadById(id, readOnly: false);
            if (missionDefinition == null)
            {
                return NotFound($"Could not find mission definition with id '{id}'");
            }
            missionDefinition.IsDeprecated = missionDefinitionIsDeprecatedQuery.IsDeprecated;

            var newMissionDefinition = await missionDefinitionService.Update(missionDefinition);
            return new MissionDefinitionResponse(newMissionDefinition);
        }

        /// <summary>
        ///     Deletes the mission definition with the specified id from the database.
        /// </summary>
        [HttpDelete]
        [Authorize(Roles = Role.Admin)]
        [Route("{id}")]
        [ProducesResponseType(typeof(MissionDefinitionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<MissionDefinitionResponse>> DeleteMissionDefinitionWithTasks([FromRoute] string id)
        {
            var missionDefinition = await missionDefinitionService.Delete(id);
            if (missionDefinition is null)
            {
                return NotFound($"Mission definition with id {id} not found");
            }
            var missionDefinitionResponse = new MissionDefinitionWithTasksResponse(missionDefinitionService, missionDefinition);
            return Ok(missionDefinitionResponse);
        }
    }
}
