using Api.Controllers.Models;
using Api.Database.Models;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController]
    [Route("inspection-findings")]
    public class InspectionFindingsController : ControllerBase
    {
        private readonly IInspectionService _inspectionService;
        private readonly ILogger<InspectionFindingsController> _logger;
        public InspectionFindingsController(
            ILogger<InspectionFindingsController> logger,
            IInspectionService inspectionService
        )
        {
            _logger = logger;
            _inspectionService = inspectionService;

        }

        /// <summary>
        /// Add a new inspection finding - need isarStepId to run this
        /// </summary>
        /// <remarks>
        /// <para> This query adds a new area to the database </para>
        /// </remarks>
        [HttpPost("add-findings")]
        [Authorize(Roles = Role.Admin)]
        [ProducesResponseType(typeof(AreaResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<InspectionFindings>> AddFindings([FromBody] InspectionFindingsQuery inspectionFinding)
        {
            _logger.LogInformation("Updating inspection findings for inspection with isarStepId '{inspectionFinding.IsarStepId}'", inspectionFinding.IsarStepId);
            try
            {
                var inspection = await _inspectionService.ReadById(inspectionFinding.IsarStepId);
                if (inspection != null)
                {
                    inspection = await _inspectionService.AddFindings(inspectionFinding);
                    return Ok(inspection);
                }

            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error while adding findings to inspection with IsarStepId '{inspectionFinding.IsarStepId}'", inspectionFinding.IsarStepId);
                throw;
            }
            return NotFound();
        }

        /// <summary>
        /// Get the full inspection against an isarStepId
        /// </summary>
        /// <remarks>
        /// <para> This query adds a new area to the database </para>
        /// </remarks>
        [HttpGet]
        [Authorize(Roles = Role.Admin)]
        [Route("{id}")]
        [ProducesResponseType(typeof(AreaResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<InspectionFindings>> GetInspections([FromRoute] string id)
        {
            _logger.LogInformation("Get inspection by ID '{inspectionFinding.InspectionId}'", id);
            try
            {
                var inspection = await _inspectionService.ReadById(id);
                if (inspection != null)
                {
                    return Ok(inspection);
                }

            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error while finding an inspection with inspection id '{id}'", id);
                throw;
            }
            return NotFound();
        }

    }

}
