using Api.Controllers.Models;
using Api.Services;
using Api.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController]
    [Route("statistics")]
    public class StatisticsController(IStatisticsService statisticsService) : ControllerBase
    {
        /// <summary>
        ///     Get aggregated mission and task statistics for a single robot.
        /// </summary>
        /// <remarks>
        ///     Counts only completed mission runs whose creation time is in the
        ///     [minCreationTime, maxCreationTime) window and returns per-week
        ///     mission counts for the full weeks within that window. Results are
        ///     limited to installations the requesting user may read.
        /// </remarks>
        [HttpGet("robots/{robotId}/missions")]
        [Authorize(Roles = Role.Any)]
        [ProducesResponseType(typeof(RobotStatisticsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<RobotStatisticsResponse>> GetRobotMissionStatistics(
            [FromRoute] string robotId,
            [FromQuery] long? minCreationTime,
            [FromQuery] long? maxCreationTime
        )
        {
            if (minCreationTime is null || maxCreationTime is null)
            {
                return BadRequest("Both minCreationTime and maxCreationTime must be provided");
            }
            if (maxCreationTime < minCreationTime)
            {
                return BadRequest("Max CreationTime cannot be less than min CreationTime");
            }

            var fromTime = DateTimeUtilities.UnixTimeStampToDateTime(minCreationTime.Value);
            var toTime = DateTimeUtilities.UnixTimeStampToDateTime(maxCreationTime.Value);

            var statistics = await statisticsService.GetRobotStatistics(robotId, fromTime, toTime);
            return Ok(statistics);
        }
    }
}
