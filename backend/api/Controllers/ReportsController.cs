using Api.Database.Models;
using Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("reports")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    /// <summary>
    /// List all reports on the asset.
    /// </summary>
    /// <remarks>
    /// <para> This query gets all reports (paginated) </para>
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(IList<Report>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IList<Report>>> GetReports([FromQuery] string? assetCode)
    {
        IList<Report> reports;
        if (assetCode != null)
            reports = await _reportService.ReadAll(assetCode);
        else
            reports = await _reportService.ReadAll();
        if (reports == null)
            return NotFound($"Could not find any reports matching the query");
        return Ok(reports);
    }

    /// <summary>
    /// Lookup report by specified id.
    /// </summary>
    [HttpGet]
    [Route("{id}")]
    [ProducesResponseType(typeof(Report), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Report>> GetReportById([FromRoute] string id)
    {
        var report = await _reportService.Read(id);
        if (report == null)
            return NotFound($"Could not find report with id {id}");
        return Ok(report);
    }
}
