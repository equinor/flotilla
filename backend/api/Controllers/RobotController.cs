using Microsoft.AspNetCore.Mvc;
using api.Models;
using api.Services;

namespace api.Controllers;

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

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Robot>>> GetRobots()
    {
        IEnumerable<Robot> robots = await _robotService.ReadAll();
        return Ok(robots);
    }

    [HttpGet]
    [Route("{id}")]
    public async Task<ActionResult<Robot>> GetRobotById([FromRoute] string id)
    {
        Robot? robot = await _robotService.Read(id);
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
            Robot newRobot = await _robotService.Create(robot);
            return CreatedAtAction(nameof(GetRobotById), new { id = newRobot.Id }, newRobot);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unable to create robot");
            throw;
        }
    }
}
