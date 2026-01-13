using Api.Controllers.Models;
using Api.Database.Models;
using Api.Services;
using Api.Services.MissionLoaders;
using Api.Services.Models;
using Api.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController]
    [Route("inspection")]
    public class InspectionController(
        ILogger<InspectionController> logger,
        IEchoService echoService,
        IInspectionService inspectionService
    ) : ControllerBase
    {
        /// <summary>
        /// Updates the Flotilla metadata for an inspection tag
        /// </summary>
        /// <remarks>
        /// <para> This query updates the Flotilla metadata for an inpection tag </para>
        /// </remarks>
        [HttpPost("{tagId}/tag-zoom")]
        [Authorize(Roles = Role.Admin)]
        [ProducesResponseType(typeof(TagInspectionMetadata), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<TagInspectionMetadata>> Create(
            [FromRoute] string tagId,
            [FromBody] IsarZoomDescription zoom
        )
        {
            tagId = Sanitize.SanitizeUserInput(tagId);

            logger.LogInformation($"Updating zoom value for tag with ID {tagId}");

            var newMetadata = new TagInspectionMetadata { TagId = tagId, ZoomDescription = zoom };

            try
            {
                var metadata = await echoService.CreateOrUpdateTagInspectionMetadata(newMetadata);

                return metadata;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error while creating or updating inspection tag metadata");
                throw;
            }
        }

        /// <summary>
        /// Lookup the inspection image for task with specified isarInspectionId
        /// </summary>
        /// <remarks>
        /// Retrieves the inspection image associated with the given ISAR Inspection ID.
        /// </remarks>
        [HttpGet("{isarInspectionId}")]
        [Authorize(Roles = Role.Any)]
        [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> GetInspectionImageByIsarInspectionId(
            [FromRoute] string isarInspectionId
        )
        {
            isarInspectionId = Sanitize.SanitizeUserInput(isarInspectionId);

            try
            {
                byte[]? inspectionStream =
                    await inspectionService.FetchInspectionImageFromIsarInspectionId(
                        isarInspectionId
                    );

                if (inspectionStream == null)
                {
                    logger.LogError(
                        "Could not fetch inspection with ISAR Inspection ID {isarInspectionId}",
                        isarInspectionId
                    );
                    return NotFound(
                        $"Could not fetch inspection with ISAR Inspection ID {isarInspectionId}"
                    );
                }

                return File(inspectionStream, "image/png");
            }
            catch (InspectionNotAvailableYetException)
            {
                logger.LogInformation(
                    "Inspection not available yet for ISAR Inspection ID {IsarInspectionId}",
                    isarInspectionId
                );
                return NotFound(
                    $"Inspection not available yet for ISAR Inspection ID {isarInspectionId}"
                );
            }
            catch (InspectionNotFoundException)
            {
                logger.LogWarning(
                    "Could not find inspection image with ISAR Inspection ID {IsarInspectionId}",
                    isarInspectionId
                );
                return NotFound(
                    $"Could not find inspection image with ISAR Inspection ID{isarInspectionId}"
                );
            }
            catch (Exception e)
            {
                logger.LogError(
                    e,
                    "Could not find inspection image with ISAR Inspection ID {IsarInspectionId}",
                    isarInspectionId
                );
                return NotFound(
                    $"Could not find inspection image with ISAR Inspection ID{isarInspectionId}."
                );
            }
        }
    }
}
