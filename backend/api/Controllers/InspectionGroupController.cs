using Api.Controllers.Models;
using Api.Database.Models;
using Api.Services;
using Azure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController]
    [Route("inspectionGroups")]
    public class InspectionGroupController(
        ILogger<InspectionGroupController> logger,
        IMapService mapService,
        IInspectionGroupService inspectionGroupService,
        IDefaultLocalizationPoseService defaultLocalizationPoseService,
        IInstallationService installationService,
        IMissionDefinitionService missionDefinitionService
    ) : ControllerBase
    {
        /// <summary>
        /// List all inspection groups in the Flotilla database
        /// </summary>
        /// <remarks>
        /// <para> This query gets all inspection groups </para>
        /// </remarks>
        [HttpGet]
        [Authorize(Roles = Role.Any)]
        [ProducesResponseType(typeof(IList<InspectionGroupResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IList<InspectionGroupResponse>>> GetInspectionGroups()
        {
            try
            {
                var inspectionGroups = await inspectionGroupService.ReadAll(readOnly: true);
                var inspectionGroupResponses = inspectionGroups
                    .Select(d => new InspectionGroupResponse(d))
                    .ToList();
                return Ok(inspectionGroupResponses);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error during GET of inspection groups from database");
                throw;
            }
        }

        /// <summary>
        /// List all inspection groups in the specified installation
        /// </summary>
        /// <remarks>
        /// <para> This query gets all inspection groups in specified installation</para>
        /// </remarks>
        [HttpGet("installation/{installationCode}")]
        [Authorize(Roles = Role.Any)]
        [ProducesResponseType(typeof(IList<InspectionGroupResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<
            ActionResult<IList<InspectionGroupResponse>>
        > GetInspectionGroupsByInstallationCode([FromRoute] string installationCode)
        {
            try
            {
                var inspectionGroups = await inspectionGroupService.ReadByInstallation(
                    installationCode,
                    readOnly: true
                );
                var inspectionGroupResponses = inspectionGroups
                    .Select(d => new InspectionGroupResponse(d))
                    .ToList();
                return Ok(inspectionGroupResponses);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error during GET of inspection groups from database");
                throw;
            }
        }

        /// <summary>
        /// Lookup inspection group by specified id.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = Role.Any)]
        [Route("{id}")]
        [ProducesResponseType(typeof(InspectionGroupResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<InspectionGroupResponse>> GetInspectionGroupById(
            [FromRoute] string id
        )
        {
            try
            {
                var inspectionGroup = await inspectionGroupService.ReadById(id, readOnly: true);
                if (inspectionGroup == null)
                    return NotFound($"Could not find inspection group with id {id}");
                return Ok(new InspectionGroupResponse(inspectionGroup));
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error during GET of inspection group from database");
                throw;
            }
        }

        /// <summary>
        /// Lookup all the mission definitions related to a inspection group
        /// </summary>
        [HttpGet]
        [Authorize(Roles = Role.Any)]
        [Route("{inspectionGroupId}/mission-definitions")]
        [ProducesResponseType(typeof(MissionDefinitionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<
            ActionResult<IList<MissionDefinitionResponse>>
        > GetMissionDefinitionsInInspectionGroup([FromRoute] string inspectionGroupId)
        {
            try
            {
                var inspectionGroup = await inspectionGroupService.ReadById(
                    inspectionGroupId,
                    readOnly: true
                );
                if (inspectionGroup == null)
                    return NotFound($"Could not find inspection group with id {inspectionGroupId}");

                var missionDefinitions = await missionDefinitionService.ReadByInspectionGroupId(
                    inspectionGroup.Id,
                    readOnly: true
                );
                var missionDefinitionResponses = missionDefinitions
                    .FindAll(m => !m.IsDeprecated)
                    .Select(m => new MissionDefinitionResponse(m));
                return Ok(missionDefinitionResponses);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error during GET of inspection group missions from database");
                throw;
            }
        }

        /// <summary>
        /// Add a new inspection Group
        /// </summary>
        /// <remarks>
        /// <para> This query adds a new inspection Group to the database </para>
        /// </remarks>
        [HttpPost]
        [Authorize(Roles = Role.Admin)]
        [ProducesResponseType(typeof(InspectionGroupResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<InspectionGroupResponse>> Create(
            [FromBody] CreateInspectionGroupQuery inspectionGroup
        )
        {
            logger.LogInformation("Creating new inspection group");
            try
            {
                var existingInstallation = await installationService.ReadByInstallationCode(
                    inspectionGroup.InstallationCode,
                    readOnly: true
                );
                if (existingInstallation == null)
                {
                    return NotFound(
                        $"Could not find installation with name {inspectionGroup.InstallationCode}"
                    );
                }
                var existingInspectionGroup =
                    await inspectionGroupService.ReadByInstallationAndName(
                        existingInstallation.InstallationCode,
                        inspectionGroup.Name,
                        readOnly: true
                    );
                if (existingInspectionGroup != null)
                {
                    logger.LogInformation(
                        "An inspection group for given name and inspection group already exists"
                    );
                    return BadRequest($"InspectionGroup already exists");
                }

                var newInspectionGroup = await inspectionGroupService.Create(inspectionGroup);
                logger.LogInformation(
                    "Succesfully created new inspection group with id '{inspectionGroupId}'",
                    newInspectionGroup.Id
                );
                return CreatedAtAction(
                    nameof(GetInspectionGroupById),
                    new { id = newInspectionGroup.Id },
                    new InspectionGroupResponse(newInspectionGroup)
                );
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error while creating new inspection group");
                throw;
            }
        }

        /// <summary>
        /// Updates default localization pose
        /// </summary>
        /// <remarks>
        /// <para> This query updates the default localization pose for a inspection group </para>
        /// </remarks>
        [HttpPut]
        [Authorize(Roles = Role.Admin)]
        [Route("{inspectionGroupId}/update-default-localization-pose")]
        [ProducesResponseType(typeof(InspectionGroupResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<InspectionGroupResponse>> UpdateDefaultLocalizationPose(
            [FromRoute] string inspectionGroupId,
            [FromBody] CreateDefaultLocalizationPose newDefaultLocalizationPose
        )
        {
            logger.LogInformation(
                "Updating default localization pose on inspection group '{inspectionGroupId}'",
                inspectionGroupId
            );
            try
            {
                var inspectionGroup = await inspectionGroupService.ReadById(
                    inspectionGroupId,
                    readOnly: true
                );
                if (inspectionGroup is null)
                {
                    logger.LogInformation(
                        "A inspection group with id '{inspectionGroupId}' does not exist",
                        inspectionGroupId
                    );
                    return NotFound("InspectionGroup does not exists");
                }

                if (inspectionGroup.DefaultLocalizationPose != null)
                {
                    inspectionGroup.DefaultLocalizationPose.Pose = newDefaultLocalizationPose.Pose;
                    inspectionGroup.DefaultLocalizationPose.DockingEnabled =
                        newDefaultLocalizationPose.IsDockingStation;
                    _ = await defaultLocalizationPoseService.Update(
                        inspectionGroup.DefaultLocalizationPose
                    );
                }
                else
                {
                    inspectionGroup.DefaultLocalizationPose = new DefaultLocalizationPose(
                        newDefaultLocalizationPose.Pose,
                        newDefaultLocalizationPose.IsDockingStation
                    );
                    inspectionGroup = await inspectionGroupService.Update(inspectionGroup);
                }

                return Ok(new InspectionGroupResponse(inspectionGroup));
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error while updating the default localization pose");
                throw;
            }
        }

        /// <summary>
        /// Deletes the inspection group with the specified id from the database.
        /// </summary>
        [HttpDelete]
        [Authorize(Roles = Role.Admin)]
        [Route("{id}")]
        [ProducesResponseType(typeof(InspectionGroupResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<InspectionGroupResponse>> DeleteInspectionGroup(
            [FromRoute] string id
        )
        {
            var inspectionGroup = await inspectionGroupService.Delete(id);
            if (inspectionGroup is null)
                return NotFound($"InspectionGroup with id {id} not found");
            return Ok(new InspectionGroupResponse(inspectionGroup));
        }

        /// <summary>
        /// Gets map metadata for localization poses belonging to inspection group with specified id
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
            var inspectionGroup = await inspectionGroupService.ReadById(id, readOnly: true);
            if (inspectionGroup is null)
            {
                string errorMessage = $"InspectionGroup not found for inspectionGroup with ID {id}";
                logger.LogError("{ErrorMessage}", errorMessage);
                return NotFound(errorMessage);
            }
            if (inspectionGroup.Installation == null)
            {
                string errorMessage = "Installation missing from inspection group";
                logger.LogWarning("{errorMessage}", errorMessage);
                return StatusCode(StatusCodes.Status500InternalServerError, errorMessage);
            }

            if (inspectionGroup.DefaultLocalizationPose is null)
            {
                string errorMessage =
                    $"InspectionGroup with id '{inspectionGroup.Id}' does not have a default localization pose";
                logger.LogInformation("{ErrorMessage}", errorMessage);
                return NotFound(errorMessage);
            }

            MapMetadata? mapMetadata;
            var positions = new List<Position>
            {
                inspectionGroup.DefaultLocalizationPose.Pose.Position,
            };
            try
            {
                mapMetadata = await mapService.ChooseMapFromPositions(
                    positions,
                    inspectionGroup.Installation.InstallationCode
                );
            }
            catch (RequestFailedException e)
            {
                string errorMessage =
                    $"An error occurred while retrieving the map for inspection group {inspectionGroup.Id}";
                logger.LogError(e, "{ErrorMessage}", errorMessage);
                return StatusCode(StatusCodes.Status502BadGateway, errorMessage);
            }
            catch (ArgumentOutOfRangeException e)
            {
                string errorMessage =
                    $"Could not find a suitable map for inspection group {inspectionGroup.Id}";
                logger.LogError(e, "{ErrorMessage}", errorMessage);
                return NotFound(errorMessage);
            }

            if (mapMetadata == null)
            {
                return NotFound(
                    "A map which contained at least half of the points in this mission could not be found"
                );
            }
            return Ok(mapMetadata);
        }
    }
}
