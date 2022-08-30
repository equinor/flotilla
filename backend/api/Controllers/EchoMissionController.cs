using System.Text.Json;
using Api.Controllers.Models;
using Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("echo-missions")]
public class EchoMissionController : ControllerBase
{
    private readonly ILogger<EchoMissionController> _logger;

    private readonly IEchoService _echoService;

    public EchoMissionController(ILogger<EchoMissionController> logger, IEchoService echoService)
    {
        _logger = logger;
        _echoService = echoService;
    }

    /// <summary>
    /// List all available missions on the asset
    /// </summary>
    /// <remarks>
    /// Overview
    ///
    /// List all available missions on the asset in the Echo mission planner
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(List<EchoMission>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<IList<EchoMission>>> GetEchoMissions()
    {
        try
        {
            var missions = await _echoService.GetMissions();
            return Ok(missions);
        }
        catch (HttpRequestException e)
        {
            _logger.LogError(e, "Error retrieving missions from Echo");
            return new StatusCodeResult(StatusCodes.Status502BadGateway);
        }
        catch (JsonException e)
        {
            _logger.LogError(e, "Error retrieving missions from Echo");
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Lookup mission by specified missionId
    /// </summary>
    /// <remarks>
    /// Overview
    ///
    /// Lookup mission by specified missionId
    /// <para>Returns a mission corresponding to the specified missionId</para>
    /// </remarks>
    [HttpGet]
    [Route("{missionId}")]
    [ProducesResponseType(typeof(EchoMission), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<EchoMission>> GetEchoMission([FromRoute] int missionId)
    {
        try
        {
            var mission = await _echoService.GetMissionById(missionId);
            return Ok(mission);
        }
        catch (HttpRequestException e)
        {
            if (e.StatusCode.HasValue && (int)e.StatusCode.Value == 404)
            {
                _logger.LogWarning("Could not find echo mission with id={id}", missionId);
                return NotFound("Echo mission not found");
            }

            _logger.LogError(e, "Error getting mission from Echo");
            return new StatusCodeResult(StatusCodes.Status502BadGateway);
        }
        catch (JsonException e)
        {
            _logger.LogError(e, "Error deserializing mission from Echo");
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }
    [HttpGet]
    [Route("installation/{installationCode}")]
    [ProducesResponseType(typeof(EchoMission), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<EchoMission>> GetEchoMissionsFromInstallation([FromRoute] string installationCode)
    {
        try
        {
            var mission = await _echoService.GetMissionsByInstallation(installationCode);
            return Ok(mission);
        }
        catch (HttpRequestException e)
        {
            if (e.StatusCode.HasValue && (int)e.StatusCode.Value == 404)
            {
                _logger.LogWarning("Could not find echo mission from installation with installationCode={installationCode}", installationCode);
                return NotFound("Echo mission not found");
            }

            _logger.LogError(e, "Error getting mission from Echo");
            return new StatusCodeResult(StatusCodes.Status502BadGateway);
        }
        catch (JsonException e)
        {
            _logger.LogError(e, "Error deserializing mission from Echo");
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }
}
