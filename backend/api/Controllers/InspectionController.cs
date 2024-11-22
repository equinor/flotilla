using Api.Controllers.Models;
using Api.Database.Models;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController]
    [Route("inspection")]
    public class InspectionController(
            ILogger<InspectionController> logger,
            IInspectionService inspectionService
        ) : ControllerBase
    {

        /// <summary>
        /// Get the inspection image against an isarTaskId
        /// </summary>
        /// <remarks>
        /// </remarks>
        [HttpGet]
        [Authorize(Roles = Role.User)]
        [Route("{installationCode}/{taskId}/taskId")]
        [ProducesResponseType(typeof(Inspection), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<Inspection>> GetInspectionImageById([FromRoute] string installationCode, string taskId)
        {
            Inspection? inspection;
            try
            {
                inspection = await inspectionService.ReadByIsarTaskId(taskId, readOnly: true);
                if (inspection == null) return NotFound($"Could not find inspection for task with Id {taskId}.");

            }
            catch (Exception e)
            {
                logger.LogError(e, $"Error while finding an inspection with task Id {taskId}");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            // Make API call to IDA using inspection.Id
            string inspectionBlobName = "apprentices.jpg";
            installationCode = "kaa";

            try
            {
                byte[] inspectionStream = await inspectionService.FetchInpectionImage(inspectionBlobName, installationCode);
                return File(inspectionStream, "image/png");
            }
            catch (Azure.RequestFailedException)
            {
                return NotFound($"Could not find inspection blob with name {inspectionBlobName}.");
            }
        }
    }
}
