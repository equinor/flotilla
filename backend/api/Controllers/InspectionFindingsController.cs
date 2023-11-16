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
        /// Associate a new inspection finding with the inspection corresponding to isarStepId
        /// </summary>
        /// <remarks>
        /// </remarks>
        [HttpPost]
        [Authorize(Roles = Role.Admin)]
        [Route("{isarStepId}")]
        [ProducesResponseType(typeof(InspectionFinding), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<InspectionFinding>> AddFindings([FromBody] InspectionFindingsQuery inspectionFinding, [FromRoute] string isarStepId)
        {
            _logger.LogInformation("Updating inspection findings for inspection with isarStepId '{Id}'", isarStepId);
            try
            {
                var inspection = await _inspectionService.AddFindings(inspectionFinding, isarStepId);

                if (inspection != null)
                {
                    return Ok(inspection.InspectionFindings);
                }

            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error while adding findings to inspection with IsarStepId '{Id}'", isarStepId);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
            return NotFound($"Could not find any inspection with the provided '{isarStepId}'");
        }

        /// <summary>
        /// Get the full inspection against an isarStepId
        /// </summary>
        /// <remarks>
        /// </remarks>
        [HttpGet]
        [Authorize(Roles = Role.Admin)]
        [Route("{id}")]
        [ProducesResponseType(typeof(Inspection), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<Inspection>> GetInspections([FromRoute] string id)
        {
            _logger.LogInformation("Get inspection by ID '{id}'", id);
            try
            {
                var inspection = await _inspectionService.ReadByIsarStepId(id);
                if (inspection != null)
                {
                    return Ok(inspection);
                }

            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error while finding an inspection with inspection id '{id}'", id);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
            return NotFound("Could not find any inspection with the provided '{id}'");
        }

    }


}
