using System.Text.Json;
using Api.Controllers.Models;
using Api.Database.Models;
using Api.Services;
using Api.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("sources")]
public class SourceController : ControllerBase
{
    private readonly ICustomMissionService _customMissionService;
    private readonly IEchoService _echoService;
    private readonly ISourceService _sourceService;
    private readonly ILogger<SourceController> _logger;

    public SourceController(
        ICustomMissionService customMissionService,
        IEchoService echoService,
        ISourceService sourceService,
        ILogger<SourceController> logger
    )
    {
        _customMissionService = customMissionService;
        _echoService = echoService;
        _sourceService = sourceService;
        _logger = logger;
    }

    /// <summary>
    /// List all sources in the Flotilla database
    /// </summary>
    /// <remarks>
    /// <para> This query gets all sources </para>
    /// </remarks>
    [HttpGet]
    [Authorize(Roles = Role.Any)]
    [ProducesResponseType(typeof(IList<Source>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IList<Source>>> GetAllSources(
        [FromQuery] SourceQueryStringParameters? parameters
    )
    {
        PagedList<Source> sources;
        try
        {
            sources = await _sourceService.ReadAll(parameters);
        }
        catch (InvalidDataException e)
        {
            _logger.LogError(e.Message);
            return BadRequest(e.Message);
        }

        var metadata = new
        {
            sources.TotalCount,
            sources.PageSize,
            sources.CurrentPage,
            sources.TotalPages,
            sources.HasNext,
            sources.HasPrevious
        };

        Response.Headers.Add(
            QueryStringParameters.PaginationHeader,
            JsonSerializer.Serialize(metadata)
        );

        return Ok(sources);
    }

    /// <summary>
    /// Lookup a custom source by specified id.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = Role.Any)]
    [Route("custom/{id}")]
    [ProducesResponseType(typeof(SourceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SourceResponse>> GetCustomSourceById([FromRoute] string id)
    {
        var source = await _sourceService.ReadByIdWithTasks(id);
        if (source == null)
            return NotFound($"Could not find mission definition with id {id}");
        return Ok(source);
    }

    /// <summary>
    /// Lookup an echo source by specified id.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = Role.Any)]
    [Route("echo/{id}/{installationCode}")]
    [ProducesResponseType(typeof(SourceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SourceResponse>> GetEchoSourceById([FromRoute] string id, [FromRoute] string installationCode)
    {
        var source = await _sourceService.ReadByIdAndInstallationWithTasks(id, installationCode);
        if (source == null)
            return NotFound($"Could not find mission definition with id {id}");
        return Ok(source);
    }
}
