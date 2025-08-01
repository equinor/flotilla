using Api.Controllers.Models;
using Api.Database.Models;
using Api.Services;
using Api.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController]
    [Route("exclusionAreas")]
    public class ExclusionAreaController(
        ILogger<ExclusionAreaController> logger,
        IExclusionAreaService exclusionAreaService,
        IInstallationService installationService,
        IPlantService plantService
    ) : ControllerBase
    {
        /// <summary>
        /// List all exclusion areas in the Flotilla database
        /// </summary>
        /// <remarks>
        /// <para> This query gets all exclusion areas </para>
        /// </remarks>
        [HttpGet]
        [Authorize(Roles = Role.User)]
        [ProducesResponseType(typeof(IList<ExclusionAreaResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IList<ExclusionAreaResponse>>> GetExclusionAreas()
        {
            try
            {
                var exclusionAreas = await exclusionAreaService.ReadAll(readOnly: true);
                var exclusionAreaResponses = exclusionAreas
                    .Select(d => new ExclusionAreaResponse(d))
                    .ToList();
                return Ok(exclusionAreaResponses);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error during GET of exclusion areas from database");
                throw;
            }
        }

        /// <summary>
        /// List all exclusion areas in the specified installation
        /// </summary>
        /// <remarks>
        /// <para> This query gets all exclusion areas in specified installation</para>
        /// </remarks>
        [HttpGet("installation/{installationCode}")]
        [Authorize(Roles = Role.User)]
        [ProducesResponseType(typeof(IList<ExclusionAreaResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<
            ActionResult<IList<ExclusionAreaResponse>>
        > GetExclusionAreasByInstallationCode([FromRoute] string installationCode)
        {
            try
            {
                var exclusionAreas = await exclusionAreaService.ReadByInstallationCode(
                    installationCode,
                    readOnly: true
                );
                var exclusionAreaResponses = exclusionAreas
                    .Select(d => new ExclusionAreaResponse(d))
                    .ToList();
                return Ok(exclusionAreaResponses);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error during GET of exclusion areas from database");
                throw;
            }
        }

        /// <summary>
        /// Lookup exclusion area by specified id.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = Role.User)]
        [Route("{id}")]
        [ProducesResponseType(typeof(ExclusionAreaResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ExclusionAreaResponse>> GetExclusionAreaById(
            [FromRoute] string id
        )
        {
            try
            {
                var exclusionArea = await exclusionAreaService.ReadById(id, readOnly: true);
                if (exclusionArea == null)
                    return NotFound($"Could not find exclusion area with id {id}");
                return Ok(new ExclusionAreaResponse(exclusionArea));
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error during GET of exclusion area from database");
                throw;
            }
        }

        /// <summary>
        /// Update the exclusion area json polygon
        /// </summary>
        [HttpPatch]
        [Authorize(Roles = Role.User)]
        [Route("{exclusionAreaId}/area-polygon")]
        [ProducesResponseType(typeof(ActionResult<ExclusionArea>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ExclusionArea>> UpdateExclusionAreaPolygon(
            [FromRoute] string exclusionAreaId,
            [FromBody] AreaPolygon areaPolygon
        )
        {
            try
            {
                var exclusionArea = await exclusionAreaService.ReadById(
                    exclusionAreaId,
                    readOnly: true
                );
                if (exclusionArea == null)
                    return NotFound($"Could not find exclusion area with id {exclusionAreaId}");

                exclusionArea.AreaPolygon = areaPolygon;
                var updatedExclusionArea = await exclusionAreaService.Update(exclusionArea);
                return Ok(exclusionArea);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error during updating exclusion area polygon");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Add a new exclusion area
        /// </summary>
        /// <remarks>
        /// <para> This query adds a new exclusion area to the database </para>
        /// </remarks>
        [HttpPost]
        [Authorize(Roles = Role.Admin)]
        [ProducesResponseType(typeof(ExclusionAreaResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<ExclusionAreaResponse>> Create(
            [FromBody] CreateExclusionAreaQuery exclusionArea
        )
        {
            logger.LogInformation("Creating new exclusion area");
            try
            {
                var existingInstallation = await installationService.ReadByInstallationCode(
                    exclusionArea.InstallationCode,
                    readOnly: true
                );
                if (existingInstallation == null)
                {
                    return NotFound(
                        $"Could not find installation with name {exclusionArea.InstallationCode}"
                    );
                }
                var existingPlant = await plantService.ReadByInstallationAndPlantCode(
                    existingInstallation,
                    exclusionArea.PlantCode,
                    readOnly: true
                );
                if (existingPlant == null)
                {
                    return NotFound($"Could not find plant with name {exclusionArea.PlantCode}");
                }

                if (exclusionArea.Name != null)
                {
                    var existingExclusionArea =
                        await exclusionAreaService.ReadByInstallationAndPlantAndName(
                            existingInstallation,
                            existingPlant,
                            exclusionArea.Name,
                            readOnly: true
                        );
                    if (existingExclusionArea != null)
                    {
                        logger.LogInformation(
                            "An exclusion area for given name and exclusion area already exists"
                        );
                        return BadRequest(
                            $"ExclusionArea with name {exclusionArea.Name} already exists"
                        );
                    }
                }

                var newExclusionArea = await exclusionAreaService.Create(exclusionArea);
                logger.LogInformation(
                    "Succesfully created new exclusion area with id '{exclusionAreaId}'",
                    newExclusionArea.Id
                );
                return CreatedAtAction(
                    nameof(GetExclusionAreaById),
                    new { id = newExclusionArea.Id },
                    new ExclusionAreaResponse(newExclusionArea)
                );
            }
            catch (InvalidPolygonException e)
            {
                logger.LogError(e, "Invalid polygon");
                return BadRequest("Invalid polygon");
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error while creating new exclusion area");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Deletes the exclusion area with the specified id from the database.
        /// </summary>
        [HttpDelete]
        [Authorize(Roles = Role.Admin)]
        [Route("{id}")]
        [ProducesResponseType(typeof(ExclusionAreaResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ExclusionAreaResponse>> DeleteExclusionArea(
            [FromRoute] string id
        )
        {
            var exclusionArea = await exclusionAreaService.Delete(id);
            if (exclusionArea is null)
                return NotFound($"ExclusionArea with id {id} not found");
            return Ok(new ExclusionAreaResponse(exclusionArea));
        }
    }
}
