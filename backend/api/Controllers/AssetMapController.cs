using System.Text.Json;
using Api.Controllers.Models;
using Api.Database.Models;
using Api.Services;
using Api.Services.Models;
using Api.Utilities;
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
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<String>> GetMap()
    {
        try
        {
            var map = await _mapService.GetMap();
            return Ok(map);
        }
        catch (Azure.RequestFailedException e)
        {
            _logger.LogError(e, "Error getting map for this area");
            return new StatusCodeResult(StatusCodes.Status502BadGateway);
        }
    }
}