using System.Text.Json;
using Api.Controllers.Models;
using Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;
[ApiController]
[Route("echo-plant")]
public class EchoPlantController : ControllerBase
{
    private readonly ILogger<EchoPlantController> _logger;
    private readonly IEchoService _echoService;
    public EchoPlantController(ILogger<EchoPlantController> logger, IEchoService echoService)
    {
        _logger = logger;
        _echoService = echoService;
    }

    /// <summary>
    /// Get selected information on all the plants in Echo
    /// </summary>
    [HttpGet]
    [Route("/all-plants-info")]
    [ProducesResponseType(typeof(List<EchoPlantInfo>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<EchoMission>> GetEchoPlantInfos()
    {
        try
        {
            var echoPlantInfos = await _echoService.GetEchoPlantInfos();
            return Ok(echoPlantInfos);
        }
        catch (HttpRequestException e)
        {
            if (e.StatusCode.HasValue && (int)e.StatusCode.Value == 404)
            {
                _logger.LogWarning("Could not get plant info from Echo");
                return NotFound("Echo plant info not found");
            }

            _logger.LogError(e, "Error getting plant info from Echo");
            return new StatusCodeResult(StatusCodes.Status502BadGateway);
        }
        catch (JsonException e)
        {
            _logger.LogError(e, "Error deserializing plant info response from Echo");
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }
}


