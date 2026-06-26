using Api.Controllers.Models;
using Api.Database.Models;
using Api.Services;
using Api.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController]
    [Route("inspection")]
    public class InspectionController(
        ILogger<InspectionController> logger,
        IInspectionService inspectionService,
        IMissionRunService missionRunService
    ) : ControllerBase
    {
        /// <summary>
        /// Lookup the inspection value for task with specified isarInspectionId
        /// </summary>
        /// <remarks>
        /// Retrieves the inspection value associated with the given ISAR Inspection ID.
        /// </remarks>
        [HttpGet("value/{isarInspectionId}")]
        [Authorize(Roles = Role.Any)]
        [ProducesResponseType(typeof(double?), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<double>> GetInspectionValueByIsarInspectionId(
            [FromRoute] string isarInspectionId
        )
        {
            isarInspectionId = Sanitize.SanitizeUserInput(isarInspectionId);

            var missionRun = await missionRunService.ReadByIsarInspectionId(isarInspectionId);
            if (missionRun is null)
            {
                string errorMessage =
                    $"Could not find mission run for ISAR Inspection ID {isarInspectionId}";
                logger.LogError("{Message}", errorMessage);
                return NotFound(errorMessage);
            }

            var task = missionRun.Tasks.FirstOrDefault(t =>
                t.Inspection != null && t.Inspection.IsarInspectionId.Equals(isarInspectionId)
            );
            if (task is null)
            {
                string errorMessage =
                    $"Could not find task for ISAR Inspection ID {isarInspectionId}";
                logger.LogError("{Message}", errorMessage);
                return NotFound(errorMessage);
            }

            if (task.StartTime is null || task.EndTime is null)
            {
                string errorMessage =
                    $"Start time or end time is null for task with ISAR Inspection ID {isarInspectionId}. Cannot fetch inspection value without time range.";
                logger.LogError("{Message}", errorMessage);
                return NotFound(errorMessage);
            }

            if (task.Inspection?.InspectionType != SensorType.CO2Measurement)
            {
                string errorMessage =
                    $"Inspection with ISAR Inspection ID {isarInspectionId} is of type {task.Inspection?.InspectionType}. Fetching of inspection value is not supported for this inspection type.";
                logger.LogError("{Message}", errorMessage);
                return NotFound(errorMessage);
            }

            string inspectionName = inspectionService.GetInspectionName(
                missionRun.InstallationCode,
                task.RobotPose.Position,
                task.TagId,
                missionRun.Robot.Name,
                task.Description
            );

            var query = new FetchCO2Query
            {
                Facility = missionRun.InstallationCode,
                TaskStartTime = task.StartTime.Value.ToString("o"),
                TaskEndTime = task.EndTime.Value.ToString("o"),
                InspectionName = inspectionName,
            };
            try
            {
                double co2Value = await inspectionService.FetchCO2ConcentrationFromQuery(query);
                return Ok(co2Value);
            }
            catch (InspectionNotFoundException e)
            {
                logger.LogWarning(e.Message);
                return NotFound(e.Message);
            }
        }
    }
}
