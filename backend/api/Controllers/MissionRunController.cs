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
    [Route("missions/runs")]
    public class MissionRunController(
        ILogger<MissionRunController> logger,
        IMissionRunService missionRunService
    ) : ControllerBase
    {
        /// <summary>
        ///     List all mission runs in the Flotilla database
        /// </summary>
        /// <remarks>
        ///     <para> This query gets all mission runs </para>
        /// </remarks>
        [HttpGet("")]
        [Authorize(Roles = Role.Any)]
        [ProducesResponseType(typeof(IList<MissionRunResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IList<MissionRunResponse>>> GetMissionRuns(
            [FromQuery] MissionRunQueryStringParameters parameters
        )
        {
            if (parameters.MaxCreationTime < parameters.MinCreationTime)
            {
                return BadRequest("Max CreationTime cannot be less than min CreationTime");
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
                missionRuns = await missionRunService.ReadAll(parameters, readOnly: true);
            }
            catch (InvalidDataException e)
            {
                logger.LogError(e, "Message: {errorMessage}", e.Message);
                return BadRequest(e.Message);
            }

            var metadata = new
            {
                missionRuns.TotalCount,
                missionRuns.PageSize,
                missionRuns.CurrentPage,
                missionRuns.TotalPages,
                missionRuns.HasNext,
                missionRuns.HasPrevious,
            };

            Response.Headers.Append(
                QueryStringParameters.PaginationHeader,
                JsonSerializer.Serialize(metadata)
            );

            var missionRunResponses = missionRuns.Select(mission => new MissionRunResponse(
                mission
            ));
            return Ok(missionRunResponses);
        }

        /// <summary>
        ///     Lookup mission run by specified id.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = Role.Any)]
        [Route("{id}")]
        [ProducesResponseType(typeof(MissionRunResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<MissionRunResponse>> GetMissionRunById([FromRoute] string id)
        {
            var missionRun = await missionRunService.ReadById(id, readOnly: true);
            if (missionRun == null)
            {
                return NotFound($"Could not find mission run with id {id}");
            }

            var missionRunResponse = new MissionRunResponse(missionRun);
            return Ok(missionRunResponse);
        }

        /// <summary>
        ///     Lookup mission run by specified id.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = Role.Any)]
        [Route("inspection/{id}")]
        [ProducesResponseType(typeof(MissionRunResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<MissionRunResponse>> GetMissionRunByInspectionId(
            [FromRoute] string id
        )
        {
            var missionRun = await missionRunService.ReadByInspectionId(id, readOnly: true);
            if (missionRun == null)
            {
                return NotFound($"Could not find mission run with inspection with id {id}");
            }

            var missionRunResponse = new MissionRunResponse(missionRun);
            return Ok(missionRunResponse);
        }

        /// <summary>
        ///     Deletes the mission run with the specified id from the database.
        /// </summary>
        [HttpDelete]
        [Authorize(Roles = Role.User)]
        [Route("{id}")]
        [ProducesResponseType(typeof(MissionRunResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<MissionRunResponse>> DeleteMissionRun([FromRoute] string id)
        {
            var missionRun = await missionRunService.Delete(id);
            if (missionRun is null)
            {
                return NotFound($"Mission run with id {id} not found");
            }

            var missionRunResponse = new MissionRunResponse(missionRun);
            return Ok(missionRunResponse);
        }

        /// <summary>
        ///     Deletes all queued mission runs from the database.
        /// </summary>
        [HttpDelete]
        [Authorize(Roles = Role.User)]
        [Route("queued-missions")]
        [ProducesResponseType(typeof(IList<MissionRunResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IList<MissionRunResponse>>> DeleteQueuedMissionRuns()
        {
            var queuedMissionRuns = await missionRunService.ReadAll(
                new MissionRunQueryStringParameters { Statuses = [MissionStatus.Queued] }
            );

            var missionRunResponse = new List<MissionRunResponse>();

            foreach (var missionRun in queuedMissionRuns)
            {
                var deletedMissionRun = await missionRunService.Delete(missionRun.Id);
                if (deletedMissionRun is null)
                {
                    return NotFound($"Mission run with id {missionRun.Id} not found");
                }

                missionRunResponse.Add(new MissionRunResponse(missionRun));
            }

            return Ok(missionRunResponse);
        }
    }
}
