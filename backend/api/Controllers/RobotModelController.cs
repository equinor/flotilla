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

    /// <summary>
    /// Updates a robot model in the database based on id
    /// </summary>
    /// <response code="200"> The robot model was successfully updated </response>
    /// <response code="400"> The robot model data is invalid </response>
    /// <response code="404"> There was no robot model with the given ID in the database </response>
    [HttpPut]
    [Authorize(Roles = Role.Admin)]
    [Route("{id}")]
    [ProducesResponseType(typeof(RobotModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Robot>> UpdateRobotModelById(
        [FromRoute] string id,
        [FromBody] UpdateRobotModelQuery robotModelQuery
    )
    {
        logger.LogInformation("Updating robot model with id '{id}'", id);

        if (!ModelState.IsValid)
            return BadRequest("Invalid data.");

        var robotModel = await robotModelService.ReadById(id);
        if (robotModel == null)
            return NotFound($"Could not find robot model with id '{id}'");

        return await UpdateModel(robotModel, robotModelQuery);
    }

    /// <summary>
    /// Updates a robot model in the database based on robot type
    /// </summary>
    /// <response code="200"> The robot model was successfully updated </response>
    /// <response code="400"> The robot model data is invalid </response>
    /// <response code="404"> There was no robot model with the specified robot type in the database </response>
    [HttpPut]
    [Authorize(Roles = Role.Admin)]
    [Route("type/{robotType}")]
    [ProducesResponseType(typeof(RobotModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Robot>> UpdateRobotModelByRobotType(
        [FromRoute] RobotType robotType,
        [FromBody] UpdateRobotModelQuery robotModelQuery
    )
    {
        logger.LogInformation("Updating robot model with robot type '{robotType}'", robotType);

        if (!ModelState.IsValid)
            return BadRequest("Invalid data.");

        var robotModel = await robotModelService.ReadByRobotType(robotType);
        if (robotModel == null)
            return NotFound($"Could not find robot model with robot type '{robotType}'");

        return await UpdateModel(robotModel, robotModelQuery);
    }

    /// <summary>
    /// Deletes the robot model with the specified id from the database.
    /// </summary>
    [HttpDelete]
    [Authorize(Roles = Role.Admin)]
    [Route("{id}")]
    [ProducesResponseType(typeof(RobotModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<RobotModel>> DeleteRobotModel([FromRoute] string id)
    {
        var robotModel = await robotModelService.Delete(id);
        if (robotModel is null)
            return NotFound($"Area with id {id} not found");
        return Ok(robotModel);
    }

    private async Task<ActionResult<Robot>> UpdateModel(
        RobotModel robotModel,
        UpdateRobotModelQuery robotModelQuery
    )
    {
        robotModel.Update(robotModelQuery);
        try
        {
            var updatedRobotModel = await robotModelService.Update(robotModel);

            logger.LogInformation("Successful PUT of robot model to database");

            return Ok(updatedRobotModel);
        }
        catch (Exception e)
        {
            logger.LogError(
                e,
                "Error while updating robot model with id '{id}' and robot type '{robotType}'",
                robotModel.Id,
                robotModel.Type
            );
            throw;
        }
    }
}
