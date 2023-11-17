using Api.Controllers.Models;
using Api.Database.Models;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController]
    [Route("timeseries")]
    [Authorize(Roles = Role.Any)]
    public class TimeseriesController(
            ILogger<TimeseriesController> logger,
            ITimeseriesService timeseriesService
        ) : ControllerBase
    {
        /// <summary>
        /// Get pressure timeseries
        /// </summary>
        /// <remarks>
        /// <para> This query gets a paginated response of entries of the pressure timeseries </para>
        /// </remarks>
        [HttpGet]
        [Route("pressure")]
        [Authorize(Roles = Role.Any)]
        [ProducesResponseType(typeof(IList<RobotPressureTimeseries>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IList<RobotPressureTimeseries>>> GetPressureTimeseries(
            [FromQuery] TimeseriesQueryStringParameters queryStringParameters
        )
        {
            try
            {
                var robotPressureTimeseries =
                    await timeseriesService.ReadAll<RobotPressureTimeseries>(
                        queryStringParameters
                    );
                return Ok(robotPressureTimeseries);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error during GET of robot pressure timeseries from database");
                throw;
            }
        }

        /// <summary>
        /// Get battery timeseries
        /// </summary>
        /// <remarks>
        /// <para> This query gets a paginated response of entries of the battery timeseries </para>
        /// </remarks>
        [HttpGet]
        [Route("battery")]
        [Authorize(Roles = Role.Any)]
        [ProducesResponseType(typeof(IList<RobotBatteryTimeseries>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IList<RobotBatteryTimeseries>>> GetBatteryTimeseries(
            [FromQuery] TimeseriesQueryStringParameters queryStringParameters
        )
        {
            try
            {
                var robotBatteryTimeseries =
                    await timeseriesService.ReadAll<RobotBatteryTimeseries>(queryStringParameters);
                return Ok(robotBatteryTimeseries);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error during GET of robot battery timeseries from database");
                throw;
            }
        }

        /// <summary>
        /// Get pose timeseries
        /// </summary>
        /// <remarks>
        /// <para> This query gets a paginated response of entries of the pose timeseries </para>
        /// </remarks>
        [HttpGet]
        [Route("pose")]
        [Authorize(Roles = Role.Any)]
        [ProducesResponseType(typeof(IList<RobotPoseTimeseries>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IList<RobotPoseTimeseries>>> GetPoseTimeseries(
            [FromQuery] TimeseriesQueryStringParameters queryStringParameters
        )
        {
            try
            {
                var robotPoseTimeseries = await timeseriesService.ReadAll<RobotPoseTimeseries>(
                    queryStringParameters
                );
                return Ok(robotPoseTimeseries);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error during GET of robot pose timeseries from database");
                throw;
            }
        }
    }
}
