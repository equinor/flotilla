using Api.Models;
using Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("[controller]")]
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
    /// Gets a list of the robots in the database.
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

    [HttpGet]
    [Route("{id}")]
    public async Task<ActionResult<Robot>> GetRobotById([FromRoute] string id)
    {
        var robot = await _robotService.Read(id);
        if (robot == null)
        {
            return NotFound();
        }
        return Ok(robot);
    }

    [HttpPost]
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
