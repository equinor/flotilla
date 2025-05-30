using Api.Controllers.Models;
using Api.Services;
using Api.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController]
    [Route("missions")]
    public class MapController(IMapService mapService) : ControllerBase
    {
        /// <summary>
        ///     Get map for mission with specified id.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = Role.Any)]
        [Route("{installationCode}/{mapName}/map")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status502BadGateway)]
        public async Task<ActionResult<byte[]>> GetMap(
            [FromRoute] string installationCode,
            string mapName
        )
        {
            installationCode = Sanitize.SanitizeUserInput(installationCode);
            mapName = Sanitize.SanitizeUserInput(mapName);

            byte[]? mapStream = await mapService.FetchMapImage(mapName, installationCode);

            if (mapStream == null)
                return NotFound(
                    $"Could not retrieve map '{mapName}' in installation {installationCode}"
                );

            return File(mapStream, "image/png");
        }
    }
}
