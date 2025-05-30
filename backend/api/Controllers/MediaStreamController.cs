using Api.Controllers.Models;
using Api.Services;
using Api.Services.Models;
using Api.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController]
    [Route("media-stream")]
    public class MediaStreamController(
        ILogger<MediaStreamController> logger,
        IIsarService isarService,
        IRobotService robotService
    ) : ControllerBase
    {
        /// <summary>
        /// Request the config for a new media stream connection from ISAR
        /// </summary>
        /// <remarks>
        /// <para> This query gets a new media stream connection config from ISAR </para>
        /// </remarks>
        [HttpGet]
        [Authorize(Roles = Role.Any)]
        [Route("{id}")]
        [ProducesResponseType(typeof(MediaConfig), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<MediaConfig?>> GetMediaStreamConfig([FromRoute] string id)
        {
            id = Sanitize.SanitizeUserInput(id);

            try
            {
                var robot = await robotService.ReadById(id);
                if (robot == null)
                {
                    return NotFound($"Could not find robot with ID {id}");
                }

                var config = await isarService.GetMediaStreamConfig(robot);
                return Ok(config);
            }
            catch (Exception)
            {
                logger.LogWarning("No ISAR media config retrieved from robot with ID {id}", id);
                return NotFound($"No media config retrieved for robot with ID {id}");
            }
        }
    }
}
