using System.Text.Json;
using Api.Controllers.Models;
using Api.Services;
using Api.Services.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController]
    [Route("pointilla")]
    public class PointillaController(
        ILogger<PointillaController> logger,
        IPointillaService pointillaService
    ) : ControllerBase
    {
        /// <summary>
        ///     Get all available pointilla map floors for mission with specified id.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = Role.Any)]
        [Route("map/{plantCode}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status502BadGateway)]
        public async Task<ActionResult<List<PointillaMapResponse>>> GetPointillaImages(
            [FromRoute] string plantCode
        )
        {
            var pointillaResponse = await pointillaService.GetMap(plantCode);
            if (pointillaResponse == null)
                return NotFound($"Could not retrieve images for plant {plantCode} from Pointilla");

            return Ok(pointillaResponse);
        }

        /// <summary>
        ///     Get map for mission with specified id.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = Role.Any)]
        [Route("map/{plantCode}/{floorId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status502BadGateway)]
        public async Task<ActionResult<PointillaMapResponse>> GetFloorMapInfo(
            [FromRoute] string plantCode,
            [FromRoute] string floorId
        )
        {
            try
            {
                var pointillaResponse = await pointillaService.GetFloorMap(plantCode, floorId);
                if (pointillaResponse == null)
                    return NotFound(
                        $"Could not retrieve map for plant {plantCode} floor {floorId} from Pointilla"
                    );
                return Ok(pointillaResponse);
            }
            catch (JsonException e)
            {
                logger.LogError(e, "Invalid JSON response from Pointilla");
                return BadRequest($"Invalid JSON response from Pointilla");
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error during GET of map from Pointilla");
                return BadRequest(
                    $"Could not retrieve map for plant {plantCode} floor {floorId} from Pointilla"
                );
            }
        }

        /// <summary>
        ///     Get map tile for mission with specified id.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = Role.Any)]
        [Route("map/tiles/{plantCode}/{floorId}/{zoomLevel}/{x}/{y}")]
        [Produces("image/png")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status502BadGateway)]
        public async Task<ActionResult> GetMapTiles(
            [FromRoute] string plantCode,
            [FromRoute] string floorId,
            [FromRoute] int zoomLevel,
            [FromRoute] int x,
            [FromRoute] int y
        )
        {
            try
            {
                var pointillaResponse = await pointillaService.GetMapTiles(
                    plantCode,
                    floorId,
                    zoomLevel,
                    x,
                    y
                );
                if (pointillaResponse == null)
                    return NotFound(
                        $"Could not retrieve map for plant {plantCode} floor {floorId} from Pointilla"
                    );
                return File(pointillaResponse, "image/png", enableRangeProcessing: true);
            }
            catch (JsonException)
            {
                return BadRequest($"Invalid JSON response from Pointilla");
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error during GET of map from Pointilla");
                return BadRequest(
                    $"Could not retrieve map for plant {plantCode} floor {floorId} from Pointilla"
                );
            }
        }
    }
}
