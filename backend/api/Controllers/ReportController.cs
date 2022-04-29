using Api.Models;
using Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("reports")]
public class ReportController : ControllerBase
{
    private readonly ReportService _reportService;

    public ReportController(ReportService reportService)
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
    public async Task<ActionResult<IEnumerable<Report>>> GetReports()
    {
        var reports = await _reportService.ReadAll();
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
        if (report == null) return NotFound($"Could not find report with id {id}");
        return Ok(report);
    }

    /// <summary>
    /// Register a new report.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Report), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Report>> PostReport([FromBody] Report report)
    {
        var newReport = await _reportService.Create(report);
        return CreatedAtAction(nameof(GetReportById), new { id = newReport.Id }, newReport);
    }
}
