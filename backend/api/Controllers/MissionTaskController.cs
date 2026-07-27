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
    [Route("missions/tasks")]
    public class MissionTaskController(
        ILogger<MissionTaskController> logger,
        IMissionTaskService missionTaskService
    ) : ControllerBase
    {
        /// <summary>
        ///     List all mission tasks in the Flotilla database
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         This query gets all mission tasks, filtered to installations the user has access to.
        ///         Results are flattened from mission runs using SelectMany.
        ///     </para>
        /// </remarks>
        [HttpGet("")]
        [Authorize(Roles = Role.Any)]
        [ProducesResponseType(typeof(IList<MissionTaskResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IList<MissionTaskResponse>>> GetMissionTasks(
            [FromQuery] MissionTaskQueryStringParameters parameters
        )
        {
            if (parameters.MaxStartTime < parameters.MinStartTime)
            {
                return BadRequest("Max StartTime cannot be less than min StartTime");
            }
            if (parameters.MaxEndTime < parameters.MinEndTime)
            {
                return BadRequest("Max EndTime cannot be less than min EndTime");
            }

            PagedList<MissionTask> missionTasks;
            try
            {
                missionTasks = await missionTaskService.ReadAll(parameters);
            }
            catch (InvalidDataException e)
            {
                logger.LogError(e, "Message: {errorMessage}", e.Message);
                return BadRequest(e.Message);
            }

            var metadata = new
            {
                missionTasks.TotalCount,
                missionTasks.PageSize,
                missionTasks.CurrentPage,
                missionTasks.TotalPages,
                missionTasks.HasNext,
                missionTasks.HasPrevious,
            };

            Response.Headers.Append(
                QueryStringParameters.PaginationHeader,
                JsonSerializer.Serialize(metadata)
            );

            var missionTaskResponses = missionTasks.Select(task => new MissionTaskResponse(task));
            return Ok(missionTaskResponses);
        }
    }
}
