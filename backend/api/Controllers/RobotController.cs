using Api.Models;
using Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("robots")]
public class RobotController : ControllerBase
{
    private readonly ILogger<RobotController> _logger;
    private readonly RobotService _robotService;

    public RobotController(ILogger<RobotController> logger, RobotService robotService)
    {
        _logger = logger;
        _robotService = robotService;
    }

    /// <summary>
    /// List all robots on the asset.
    /// </summary>
    /// <remarks>
    /// Responses are paginated.
    ///
    /// Example query:
    ///
    ///     /robots?
    ///
    /// <para> This query gets all robots (paginated) </para>
    /// </remarks>
    /// <returns> List of robots </returns>
    /// <response code="200"> The list of robots was successfully returned </response>
    /// <response code="400"> The query is invalid </response>
    [HttpGet]
    [ProducesResponseType(typeof(IList<Robot>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IList<Robot>>> GetRobots()
    {
        var robots = await _robotService.ReadAll();
        return Ok(robots);
    }

    /// <summary>
    /// Lookup robot by specified id.
    /// </summary>
    /// <remarks>
    /// </remarks>
    /// <returns> The robot, if it exists </returns>
    /// <response code="200"> Request successful and robot returned </response>
    /// <response code="404"> The requested resource was not found </response>
    [HttpGet]
    [Route("{id}")]
    [ProducesResponseType(typeof(IList<Robot>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Robot>> GetRobotById([FromRoute] string id)
    {
        var robot = await _robotService.Read(id);
        if (robot == null)
        {
            return NotFound();
        }
        return Ok(robot);
    }

    /// <summary>
    /// Register a new robot.
    /// </summary>
    /// <remarks>
    /// </remarks>
    /// <returns> A new robot </returns>
    /// <response code="201"> Request successful and the new robot </response>
    /// <response code="400"> The robot data is invalid </response>
    [HttpPost]
    [ProducesResponseType(typeof(IList<Robot>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Robot>> PostRobot([FromBody] Robot robot)
    {
        try
        {
            var newRobot = await _robotService.Create(robot);
            return CreatedAtAction(nameof(GetRobotById), new { id = newRobot.Id }, newRobot);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unable to create robot");
            throw;
        }
    }
}
