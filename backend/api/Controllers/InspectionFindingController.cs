using Api.Controllers.Models;
using Api.Database.Models;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController]
    [Route("inspection-findings")]
    public class InspectionFindingController(
            ILogger<InspectionFindingController> logger,
            IInspectionService inspectionService
        ) : ControllerBase
    {
        /// <summary>
        /// Associate a new inspection finding with the inspection corresponding to isarTaskId
        /// </summary>
        /// <remarks>
        /// </remarks>
        [HttpPost]
        [Authorize(Roles = Role.Admin)]
        [Route("{isarTaskId}")]
        [ProducesResponseType(typeof(InspectionFinding), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<InspectionFinding>> AddFinding([FromBody] InspectionFindingQuery inspectionFinding, [FromRoute] string isarTaskId)
        {
            logger.LogInformation("Add inspection finding for inspection with isarTaskId '{Id}'", isarTaskId);
            try
            {
                var inspection = await inspectionService.AddFinding(inspectionFinding, isarTaskId);

                if (inspection != null)
                {
                    return Ok(inspection.InspectionFindings);
                }

            }
            catch (Exception e)
            {
                logger.LogError(e, "Error while adding inspection finding to inspection with IsarTaskId '{Id}'", isarTaskId);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
            return NotFound($"Could not find any inspection with the provided '{isarTaskId}'");
        }

        /// <summary>
        /// Get the full inspection against an isarTaskId
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
            logger.LogInformation("Get inspection by ID '{id}'", id);
            try
            {
                var inspection = await inspectionService.ReadByIsarTaskId(id, readOnly: true);
                if (inspection != null)
                {
                    return Ok(inspection);
                }

            }
            catch (Exception e)
            {
                logger.LogError(e, "Error while finding an inspection with inspection id '{id}'", id);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
            return NotFound("Could not find any inspection with the provided '{id}'");
        }

    }


}
