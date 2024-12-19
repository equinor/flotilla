using Api.Controllers.Models;
using Api.Database.Models;
using Api.Services;
using Azure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController]
    [Route("inspectionAreas")]
    public class InspectionAreaController(
            ILogger<InspectionAreaController> logger,
            IMapService mapService,
            IInspectionAreaService inspectionAreaService,
            IDefaultLocalizationPoseService defaultLocalizationPoseService,
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
                var inspectionAreaResponses = inspectionAreas.Select(d => new InspectionAreaResponse(d)).ToList();
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
        public async Task<ActionResult<IList<InspectionAreaResponse>>> GetInspectionAreasByInstallationCode([FromRoute] string installationCode)
        {
            try
            {
                var inspectionAreas = await inspectionAreaService.ReadByInstallation(installationCode, readOnly: true);
                var inspectionAreaResponses = inspectionAreas.Select(d => new InspectionAreaResponse(d)).ToList();
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
        public async Task<ActionResult<InspectionAreaResponse>> GetInspectionAreaById([FromRoute] string id)
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
        public async Task<ActionResult<IList<MissionDefinitionResponse>>> GetMissionDefinitionsInInspectionArea([FromRoute] string inspectionAreaId)
        {
            try
            {
                var inspectionArea = await inspectionAreaService.ReadById(inspectionAreaId, readOnly: true);
                if (inspectionArea == null)
                    return NotFound($"Could not find inspection area with id {inspectionAreaId}");

                var missionDefinitions = await missionDefinitionService.ReadByInspectionAreaId(inspectionArea.Id, readOnly: true);
                var missionDefinitionResponses = missionDefinitions.FindAll(m => !m.IsDeprecated).Select(m => new MissionDefinitionResponse(m));
                return Ok(missionDefinitionResponses);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error during GET of inspection area missions from database");
                throw;
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
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<InspectionAreaResponse>> Create([FromBody] CreateInspectionAreaQuery inspectionArea)
        {
            logger.LogInformation("Creating new inspection area");
            try
            {
                var existingInstallation = await installationService.ReadByInstallationCode(inspectionArea.InstallationCode, readOnly: true);
                if (existingInstallation == null)
                {
                    return NotFound($"Could not find installation with name {inspectionArea.InstallationCode}");
                }
                var existingPlant = await plantService.ReadByInstallationAndPlantCode(existingInstallation, inspectionArea.PlantCode, readOnly: true);
                if (existingPlant == null)
                {
                    return NotFound($"Could not find plant with name {inspectionArea.PlantCode}");
                }
                var existingInspectionArea = await inspectionAreaService.ReadByInstallationAndPlantAndName(existingInstallation, existingPlant, inspectionArea.Name, readOnly: true);
                if (existingInspectionArea != null)
                {
                    logger.LogInformation("An inspection area for given name and inspection area already exists");
                    return BadRequest($"InspectionArea already exists");
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
            catch (Exception e)
            {
                logger.LogError(e, "Error while creating new inspection area");
                throw;
            }
        }

        /// <summary>
        /// Updates default localization pose
        /// </summary>
        /// <remarks>
        /// <para> This query updates the default localization pose for a inspection area </para>
        /// </remarks>
        [HttpPut]
        [Authorize(Roles = Role.Admin)]
        [Route("{inspectionAreaId}/update-default-localization-pose")]
        [ProducesResponseType(typeof(InspectionAreaResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<InspectionAreaResponse>> UpdateDefaultLocalizationPose([FromRoute] string inspectionAreaId, [FromBody] CreateDefaultLocalizationPose newDefaultLocalizationPose)
        {
            logger.LogInformation("Updating default localization pose on inspection area '{inspectionAreaId}'", inspectionAreaId);
            try
            {
                var inspectionArea = await inspectionAreaService.ReadById(inspectionAreaId, readOnly: false);
                if (inspectionArea is null)
                {
                    logger.LogInformation("A inspection area with id '{inspectionAreaId}' does not exist", inspectionAreaId);
                    return NotFound("InspectionArea does not exists");
                }

                if (inspectionArea.DefaultLocalizationPose != null)
                {
                    inspectionArea.DefaultLocalizationPose.Pose = newDefaultLocalizationPose.Pose;
                    inspectionArea.DefaultLocalizationPose.DockingEnabled = newDefaultLocalizationPose.IsDockingStation;
                    _ = await defaultLocalizationPoseService.Update(inspectionArea.DefaultLocalizationPose);
                }
                else
                {
                    inspectionArea.DefaultLocalizationPose = new DefaultLocalizationPose(newDefaultLocalizationPose.Pose, newDefaultLocalizationPose.IsDockingStation);
                    inspectionArea = await inspectionAreaService.Update(inspectionArea);
                }

                return Ok(new InspectionAreaResponse(inspectionArea));
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error while updating the default localization pose");
                throw;
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
        public async Task<ActionResult<InspectionAreaResponse>> DeleteInspectionArea([FromRoute] string id)
        {
            var inspectionArea = await inspectionAreaService.Delete(id);
            if (inspectionArea is null)
                return NotFound($"InspectionArea with id {id} not found");
            return Ok(new InspectionAreaResponse(inspectionArea));
        }

        /// <summary>
        /// Gets map metadata for localization poses belonging to inspection area with specified id
        /// </summary>
        [HttpGet]
        [Authorize(Roles = Role.Any)]
        [Route("{id}/map-metadata")]
        [ProducesResponseType(typeof(MapMetadata), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<MapMetadata>> GetMapMetadata([FromRoute] string id)
        {
            var inspectionArea = await inspectionAreaService.ReadById(id, readOnly: true);
            if (inspectionArea is null)
            {
                string errorMessage = $"InspectionArea not found for inspectionArea with ID {id}";
                logger.LogError("{ErrorMessage}", errorMessage);
                return NotFound(errorMessage);
            }
            if (inspectionArea.Installation == null)
            {
                string errorMessage = "Installation missing from inspection area";
                logger.LogWarning(errorMessage);
                return StatusCode(StatusCodes.Status500InternalServerError, errorMessage);
            }

            if (inspectionArea.DefaultLocalizationPose is null)
            {
                string errorMessage = $"InspectionArea with id '{inspectionArea.Id}' does not have a default localization pose";
                logger.LogInformation("{ErrorMessage}", errorMessage);
                return NotFound(errorMessage);
            }

            MapMetadata? mapMetadata;
            var positions = new List<Position>
            {
                inspectionArea.DefaultLocalizationPose.Pose.Position
            };
            try
            {
                mapMetadata = await mapService.ChooseMapFromPositions(positions, inspectionArea.Installation.InstallationCode);
            }
            catch (RequestFailedException e)
            {
                string errorMessage = $"An error occurred while retrieving the map for inspection area {inspectionArea.Id}";
                logger.LogError(e, "{ErrorMessage}", errorMessage);
                return StatusCode(StatusCodes.Status502BadGateway, errorMessage);
            }
            catch (ArgumentOutOfRangeException e)
            {
                string errorMessage = $"Could not find a suitable map for inspection area {inspectionArea.Id}";
                logger.LogError(e, "{ErrorMessage}", errorMessage);
                return NotFound(errorMessage);
            }

            if (mapMetadata == null)
            {
                return NotFound("A map which contained at least half of the points in this mission could not be found");
            }
            return Ok(mapMetadata);
        }
    }
}
