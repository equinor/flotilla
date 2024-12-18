using Api.Controllers.Models;
using Api.Database.Models;

using Api.Services;
using Api.Services.MissionLoaders;
using Api.Services.Models;
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
        public async Task<ActionResult<TagInspectionMetadata>> Create([FromRoute] string tagId, [FromBody] IsarZoomDescription zoom)
        {
            logger.LogInformation($"Updating zoom value for tag with ID {tagId}");

            var newMetadata = new TagInspectionMetadata
            {
                TagId = tagId,
                ZoomDescription = zoom
            };

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
        /// Lookup the inspection image for task with specified isarTaskId
        /// </summary>
        /// <remarks>
        /// </remarks>
        [HttpGet]
        [Authorize(Roles = Role.User)]
        [Route("{installationCode}/{isarInspectionId}/isarInspectionId")]
        [ProducesResponseType(typeof(Inspection), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<Inspection>> GetInspectionImageByIsarInspectionId([FromRoute] string installationCode, string isarInspectionId)
        {
            byte[]? inspectionStream = await inspectionService.GetInspectionData(installationCode, isarInspectionId);
            if (inspectionStream == null) { return NotFound($"Could not find inspection data for inspection with isar Id {isarInspectionId}."); }

            return File(inspectionStream, "image/png");
        }
    }
}
