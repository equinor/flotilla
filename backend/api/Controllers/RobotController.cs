using System.Text.Json;
using Api.Controllers.Models;
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
    private readonly IsarService _isarService;

    public RobotController(
        ILogger<RobotController> logger,
        RobotService robotService,
        IsarService isarService
    )
    {
        _logger = logger;
        _robotService = robotService;
        _isarService = isarService;
    }

    /// <summary>
    /// List all robots on the asset.
    /// </summary>
    /// <remarks>
    /// <para> This query gets all robots (paginated) </para>
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(IList<Robot>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IList<Robot>>> GetRobots()
    {
        var robots = await _robotService.ReadAll();
        return Ok(robots);
    }

    /// <summary>
    /// Gets the robot with the specified id
    /// </summary>
    /// <remarks>
    /// <para> This query gets the robot with the specified id </para>
    /// </remarks>
    [HttpGet]
    [Route("{id}")]
    [ProducesResponseType(typeof(Robot), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Robot>> GetRobotById([FromRoute] string id)
    {
        var robot = await _robotService.Read(id);
        if (robot == null)
            return NotFound($"Could not find robot with id {id}");
        return Ok(robot);
    }

    /// <summary>
    /// Create robot and add to database
    /// </summary>
    /// <remarks>
    /// <para> This query creates a robot and adds it to the database </para>
    /// </remarks>
    /// <returns> Robot </returns>
    [HttpPost]
    [ProducesResponseType(typeof(Robot), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Robot>> PostRobot([FromBody] CreateRobotQuery robot)
    {
        var newRobot = await _robotService.Create(robot);
        return CreatedAtAction(nameof(GetRobotById), new { id = newRobot.Id }, newRobot);
    }

    /// <summary>
    /// Start a mission for a given robot
    /// </summary>
    /// <remarks>
    /// <para> This query starts a mission for a given robot and creates a report </para>
    /// </remarks>
    [HttpPost]
    [Route("{robotId}/start/{missionId}")]
    [ProducesResponseType(typeof(Report), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Report>> StartMission(
        [FromRoute] string robotId,
        [FromRoute] string missionId
    )
    {
        var robot = await _robotService.Read(robotId);
        if (robot == null)
            return NotFound($"Could not find robot with robot id {robotId}");
        var report = await _isarService.StartMission(robot, missionId);
        return Ok(report);
    }

    /// <summary>
    /// Stop robot
    /// </summary>
    /// <remarks>
    /// <para> This query stops a robot based on id </para>
    /// </remarks>
    [HttpPost]
    [Route("{robotId}/stop/")]
    [ProducesResponseType(typeof(IsarStopMissionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IsarStopMissionResponse>> StopMission([FromRoute] string robotId)
    {
        var response = await _isarService.StopMission();
        if (!response.IsSuccessStatusCode)
            _logger.LogError("Could not stop mission with id {robotId}", robotId);
        if (response.Content != null)
        {
            string? responseContent = await response.Content.ReadAsStringAsync();
            var isarResponse = JsonSerializer.Deserialize<IsarStopMissionResponse>(responseContent);
            return Ok(isarResponse);
        }
        return NotFound($"Could not stop mission on robot: {robotId}");
    }
}
