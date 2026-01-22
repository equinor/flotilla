using Api.Controllers.Models;
using Api.Database.Models;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("robot-models")]
public class RobotModelController(
    ILogger<RobotModelController> logger,
    IRobotModelService robotModelService
) : ControllerBase
{
    /// <summary>
    /// List all robot models in the Flotilla database
    /// </summary>
    /// <remarks>
    /// <para> This query gets all robot models </para>
    /// </remarks>
    [HttpGet]
    [Authorize(Roles = Role.Any)]
    [ProducesResponseType(typeof(IList<RobotModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IList<RobotModel>>> GetRobotModels()
    {
        try
        {
            var robotModels = await robotModelService.ReadAll(readOnly: true);
            return Ok(robotModels);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error during GET of robot models from database");
            throw;
        }
    }

    /// <summary>
    /// Lookup robot model by the robot type
    /// </summary>
    [HttpGet]
    [Authorize(Roles = Role.Any)]
    [Route("type/{robotType}")]
    [ProducesResponseType(typeof(RobotModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<RobotModel>> GetRobotModelByRobotType(
        [FromRoute] RobotType robotType
    )
    {
        var robotModel = await robotModelService.ReadByRobotType(robotType, readOnly: true);
        if (robotModel == null)
            return NotFound($"Could not find robotModel with robot type '{robotType}'");
        return Ok(robotModel);
    }

    /// <summary>
    /// Lookup robot model by specified id.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = Role.Any)]
    [Route("{id}")]
    [ProducesResponseType(typeof(RobotModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<RobotModel>> GetRobotModelById([FromRoute] string id)
    {
        var robotModel = await robotModelService.ReadById(id, readOnly: true);
        if (robotModel == null)
            return NotFound($"Could not find robotModel with id '{id}'");
        return Ok(robotModel);
    }

    /// <summary>
    /// Add a new robot model
    /// </summary>
    /// <remarks>
    /// <para> This query adds a new robot model to the database </para>
    /// </remarks>
    [HttpPost]
    [Authorize(Roles = Role.Admin)]
    [ProducesResponseType(typeof(RobotModel), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<RobotModel>> Create(
        [FromBody] CreateRobotModelQuery robotModelQuery
    )
    {
        logger.LogInformation("Creating new robot model");

        RobotModel robotModel = new(robotModelQuery);

        if (robotModelService.ReadByRobotType(robotModel.Type, readOnly: true).Result != null)
            return BadRequest($"A robot already exists with the robot type '{robotModel.Type}");

        try
        {
            var newRobotModel = await robotModelService.Create(robotModel);
            logger.LogInformation(
                "Successfully created new robot model with id '{robotModelId}'",
                newRobotModel.Id
            );
            return CreatedAtAction(
                nameof(GetRobotModelById),
                new { id = newRobotModel.Id },
                newRobotModel
            );
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error while creating new robot model");
            throw;
        }
    }
}
