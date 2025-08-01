using System.Text.Json;
using Api.Controllers.Models;
using Api.Database.Models;
using Api.Services;
using Api.Services.Models;
using Api.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController]
    [Route("inspectionAreas")]
    public class InspectionAreaController(
        ILogger<InspectionAreaController> logger,
        IInspectionAreaService inspectionAreaService,
        IInstallationService installationService,
        IPlantService plantService,
        IMissionDefinitionService missionDefinitionService
    ) : ControllerBase
    {
        /// <summary>
        /// List all inspection areas in the Flotilla database
        /// </summary>
        /// <remarks>
        /// <para> This query gets all inspection areas </para>
        /// </remarks>
        [HttpGet]
        [Authorize(Roles = Role.Any)]
        [ProducesResponseType(typeof(IList<InspectionAreaResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IList<InspectionAreaResponse>>> GetInspectionAreas()
        {
            try
            {
                var inspectionAreas = await inspectionAreaService.ReadAll(readOnly: true);
                var inspectionAreaResponses = inspectionAreas
                    .Select(d => new InspectionAreaResponse(d))
                    .ToList();
                return Ok(inspectionAreaResponses);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error during GET of inspection areas from database");
                throw;
            }
        }

        /// <summary>
        /// List all inspection areas in the specified installation
        /// </summary>
        /// <remarks>
        /// <para> This query gets all inspection areas in specified installation</para>
        /// </remarks>
        [HttpGet("installation/{installationCode}")]
        [Authorize(Roles = Role.Any)]
        [ProducesResponseType(typeof(IList<InspectionAreaResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<
            ActionResult<IList<InspectionAreaResponse>>
        > GetInspectionAreasByInstallationCode([FromRoute] string installationCode)
        {
            try
            {
                var inspectionAreas = await inspectionAreaService.ReadByInstallation(
                    installationCode,
                    readOnly: true
                );
                var inspectionAreaResponses = inspectionAreas
                    .Select(d => new InspectionAreaResponse(d))
                    .ToList();
                return Ok(inspectionAreaResponses);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error during GET of inspection areas from database");
                throw;
            }
        }

        /// <summary>
        /// Lookup inspection area by specified id.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = Role.Any)]
        [Route("{id}")]
        [ProducesResponseType(typeof(InspectionAreaResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<InspectionAreaResponse>> GetInspectionAreaById(
            [FromRoute] string id
        )
        {
            try
            {
                var inspectionArea = await inspectionAreaService.ReadById(id, readOnly: true);
                if (inspectionArea == null)
                    return NotFound($"Could not find inspection area with id {id}");
                return Ok(new InspectionAreaResponse(inspectionArea));
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error during GET of inspection area from database");
                throw;
            }
        }

        /// <summary>
        /// Lookup all the mission definitions related to a inspection area
        /// </summary>
        [HttpGet]
        [Authorize(Roles = Role.Any)]
        [Route("{inspectionAreaId}/mission-definitions")]
        [ProducesResponseType(typeof(MissionDefinitionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<
            ActionResult<IList<MissionDefinitionResponse>>
        > GetMissionDefinitionsInInspectionArea([FromRoute] string inspectionAreaId)
        {
            try
            {
                var inspectionArea = await inspectionAreaService.ReadById(
                    inspectionAreaId,
                    readOnly: true
                );
                if (inspectionArea == null)
                    return NotFound($"Could not find inspection area with id {inspectionAreaId}");

                var missionDefinitions = await missionDefinitionService.ReadByInspectionAreaId(
                    inspectionArea.Id,
                    readOnly: true
                );
                var missionDefinitionResponses = missionDefinitions
                    .FindAll(m => !m.IsDeprecated)
                    .Select(m => new MissionDefinitionResponse(m));
                return Ok(missionDefinitionResponses);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error during GET of inspection area missions from database");
                throw;
            }
        }

        /// <summary>
        /// Update the inspection area json polygon
        /// </summary>
        [HttpPatch]
        [Authorize(Roles = Role.Any)]
        [Route("{inspectionAreaId}/area-polygon")]
        [ProducesResponseType(typeof(ActionResult<InspectionArea>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<InspectionArea>> UpdateInspectionAreaJsonPolygon(
            [FromRoute] string inspectionAreaId,
            [FromBody] AreaPolygon areaPolygonJson
        )
        {
            try
            {
                var inspectionArea = await inspectionAreaService.ReadById(
                    inspectionAreaId,
                    readOnly: true
                );
                if (inspectionArea == null)
                    return NotFound($"Could not find inspection area with id {inspectionAreaId}");

                inspectionArea.AreaPolygon = areaPolygonJson;
                var updatedInspectionArea = await inspectionAreaService.Update(inspectionArea);
                return Ok(inspectionArea);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error during updating inspection area polygon");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Add a new inspection area
        /// </summary>
        /// <remarks>
        /// <para> This query adds a new inspection area to the database </para>
        /// </remarks>
        [HttpPost]
        [Authorize(Roles = Role.Admin)]
        [ProducesResponseType(typeof(InspectionAreaResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<InspectionAreaResponse>> Create(
            [FromBody] CreateInspectionAreaQuery inspectionArea
        )
        {
            logger.LogInformation("Creating new inspection area");
            try
            {
                var existingInstallation = await installationService.ReadByInstallationCode(
                    inspectionArea.InstallationCode,
                    readOnly: true
                );
                if (existingInstallation == null)
                {
                    return NotFound(
                        $"Could not find installation with name {inspectionArea.InstallationCode}"
                    );
                }
                var existingPlant = await plantService.ReadByInstallationAndPlantCode(
                    existingInstallation,
                    inspectionArea.PlantCode,
                    readOnly: true
                );
                if (existingPlant == null)
                {
                    return NotFound($"Could not find plant with name {inspectionArea.PlantCode}");
                }
                var existingInspectionArea =
                    await inspectionAreaService.ReadByInstallationAndPlantAndName(
                        existingInstallation,
                        existingPlant,
                        inspectionArea.Name,
                        readOnly: true
                    );
                if (existingInspectionArea != null)
                {
                    logger.LogInformation(
                        "An inspection area for given name and inspection area already exists"
                    );
                    return BadRequest("InspectionArea already exists");
                }

                var newInspectionArea = await inspectionAreaService.Create(inspectionArea);
                logger.LogInformation(
                    "Succesfully created new inspection area with id '{inspectionAreaId}'",
                    newInspectionArea.Id
                );
                return CreatedAtAction(
                    nameof(GetInspectionAreaById),
                    new { id = newInspectionArea.Id },
                    new InspectionAreaResponse(newInspectionArea)
                );
            }
            catch (InvalidPolygonException e)
            {
                logger.LogError(e, "Invalid polygon");
                return BadRequest("Invalid polygon");
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error while creating new inspection area");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Deletes the inspection area with the specified id from the database.
        /// </summary>
        [HttpDelete]
        [Authorize(Roles = Role.Admin)]
        [Route("{id}")]
        [ProducesResponseType(typeof(InspectionAreaResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<InspectionAreaResponse>> DeleteInspectionArea(
            [FromRoute] string id
        )
        {
            var inspectionArea = await inspectionAreaService.Delete(id);
            if (inspectionArea is null)
                return NotFound($"InspectionArea with id {id} not found");
            return Ok(new InspectionAreaResponse(inspectionArea));
        }
    }
}
