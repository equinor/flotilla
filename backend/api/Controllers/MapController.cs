using Api.Controllers.Models;
using Api.Services;
using Azure;
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
        public async Task<ActionResult<byte[]>> GetMap([FromRoute] string installationCode, string mapName)
        {
            try
            {
                byte[] mapStream = await mapService.FetchMapImage(mapName, installationCode);
                return File(mapStream, "image/png");
            }
            catch (RequestFailedException)
            {
                return NotFound("Could not find map for this area");
            }
        }
    }
}
