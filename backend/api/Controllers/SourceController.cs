using Api.Controllers.Models;
using Api.Database.Models;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("sources")]
public class SourceController(ISourceService sourceService, ILogger<SourceController> logger)
    : ControllerBase
{
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
    public async Task<ActionResult<IList<Source>>> GetAllSources()
    {
        List<Source> sources;
        try
        {
            sources = await sourceService.ReadAll(readOnly: true);
        }
        catch (InvalidDataException e)
        {
            logger.LogError(e, "{Message}", e.Message);
            return BadRequest(e.Message);
        }

        return Ok(sources);
    }

    /// <summary>
    /// Lookup a custom source by specified id.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = Role.Any)]
    [Route("{id}")]
    [ProducesResponseType(typeof(SourceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SourceResponse>> GetSourceById([FromRoute] string id)
    {
        var source = await sourceService.ReadById(id);
        if (source == null)
            return NotFound($"Could not find source with id {id}");
        return Ok(source);
    }
}
