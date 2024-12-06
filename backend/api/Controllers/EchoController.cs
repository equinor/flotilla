using Api.Controllers.Models;
using Api.Services;
using Api.Services.MissionLoaders;
using Api.Services.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController]
    [Route("echo")]
    public class EchoController(
            ILogger<EchoController> logger,
            IEchoService echoService
        ) : ControllerBase
    {
        /// <summary>
        /// Updates the Flotilla metadata for an Echo tag
        /// </summary>
        /// <remarks>
        /// <para> This query updates the Flotilla metadata for an Echo tag </para>
        /// </remarks>
        [HttpPost("{tagId}/tag-zoom")]
        [Authorize(Roles = Role.Admin)]
        [ProducesResponseType(typeof(EchoTagInspectionMetadata), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<EchoTagInspectionMetadata>> Create([FromRoute] string tagId, [FromBody] IsarZoomDescription zoom)
        {
            logger.LogInformation($"Updating zoom value for tag with ID {tagId}");

            var newMetadata = new EchoTagInspectionMetadata
            {
                TagId = tagId,
                ZoomDescription = zoom
            };

            try
            {
                var metadata = await echoService.CreateOrUpdateEchoTagInspectionMetadata(newMetadata);

                return metadata;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error while creating or updating Echo tag inspection metadata");
                throw;
            }
        }
    }
}
