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
    public class MissionRunController : ControllerBase
    {
        private readonly ILogger<MissionRunController> _logger;
        private readonly IMissionRunService _missionRunService;

        public MissionRunController(ILogger<MissionRunController> logger, IMissionRunService missionRunService)
        {
            _logger = logger;
            _missionRunService = missionRunService;
        }

        /// <summary>
        ///     List all mission runs in the Flotilla database
        /// </summary>
        /// <remarks>
        ///     <para> This query gets all mission runs </para>
        /// </remarks>
        [HttpGet("")]
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
                _logger.LogError(e, "Message: {errorMessage}", e.Message);
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
        ///     Lookup mission run by specified id.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = Role.Any)]
        [Route("{id}")]
        [ProducesResponseType(typeof(MissionRun), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<MissionRun>> GetMissionRunById([FromRoute] string id)
        {
            var missionRun = await _missionRunService.ReadById(id);
            if (missionRun == null)
            {
                return NotFound($"Could not find mission run with id {id}");
            }
            return Ok(missionRun);
        }
        /// <summary>
        ///     Deletes the mission run with the specified id from the database.
        /// </summary>
        [HttpDelete]
        [Authorize(Roles = Role.Admin)]
        [Route("{id}")]
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
