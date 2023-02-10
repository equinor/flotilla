﻿using System.Text.Json;
using Api.Controllers.Models;
using Api.Services;
using Api.Services.Models;
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
    /// List all available Echo missions for the asset
    /// </summary>
    /// <remarks>
    /// These missions are created in the Echo mission planner
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(List<EchoMission>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<IList<EchoMission>>> GetEchoMissions(string? installationCode)
    {
        try
        {
            var missions = await _echoService.GetMissions(installationCode);
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
    /// Lookup Echo mission by Id
    /// </summary>
    /// <remarks>
    /// This mission is created in the Echo mission planner
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
        catch (InvalidDataException e)
        {
            string message = "EchoMission invalid: One or more tags are missing associated robot poses.";
            _logger.LogError(e, message);
            return StatusCode(StatusCodes.Status502BadGateway, message);
        }
    }
    [HttpPost]
    [Route("robot-pose/{poseId}")]
    [ProducesResponseType(typeof(EchoPoseResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<EchoPoseResponse>> GetPositionsFromPoseId([FromRoute] int poseId)
    {
        try
        {
            var poses = await _echoService.GetRobotPoseFromPoseId(poseId);
            return Ok(poses);
        }
        catch (HttpRequestException e)
        {
            if (e.StatusCode.HasValue && (int)e.StatusCode.Value == 404)
            {
                _logger.LogWarning("Error in echopose");
                return NotFound("Echo pose not found");
            }

            _logger.LogError(e, "Error getting position from Echo");
            return new StatusCodeResult(StatusCodes.Status502BadGateway);
        }
        catch (JsonException e)
        {
            _logger.LogError(e, "Error deserializing position from Echo");
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }
}
