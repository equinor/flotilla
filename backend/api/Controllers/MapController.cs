using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("maps")]
public class AssetMapController : ControllerBase
{
    private readonly ILogger<AssetMapController> _logger;
    private readonly IMapService _mapService;
    

    public AssetMapController(ILogger<AssetMapController> logger, IMapService mapService)
    {
        _logger = logger;
        _mapService = mapService;
    }

    [HttpGet]
    [Route("{missionId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<Byte[]>> GetMap([FromRoute] string missionId)
    { 
        try
        {
            string filePath = await _mapService.FetchMapImage(missionId);
            var returnFile = PhysicalFile(filePath, "image/png");
            return returnFile;
        }
        catch (Azure.RequestFailedException)
        {
            return NotFound("Could not find map for this area.");
        }
        catch (DirectoryNotFoundException)
        {
            return NotFound("Could not find mission with this mission ID");
        }
    }
}