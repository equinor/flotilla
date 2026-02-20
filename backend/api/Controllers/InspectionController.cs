using Api.Controllers.Models;
using Api.Database.Models;
using Api.Services;
using Api.Services.MissionLoaders;
using Api.Services.Models;
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
        /// Updates the Flotilla metadata for an inspection tag
        /// </summary>
        /// <remarks>
        /// <para> This query updates the Flotilla metadata for an inpection tag </para>
        /// </remarks>
        [HttpPost("{tagId}/tag-zoom")]
        [Authorize(Roles = Role.Admin)]
        [ProducesResponseType(typeof(TagInspectionMetadata), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<TagInspectionMetadata>> Create(
            [FromRoute] string tagId,
            [FromBody] IsarZoomDescription zoom
        )
        {
            tagId = Sanitize.SanitizeUserInput(tagId);

            logger.LogInformation($"Updating zoom value for tag with ID {tagId}");

            var newMetadata = new TagInspectionMetadata { TagId = tagId, ZoomDescription = zoom };

            try
            {
                var metadata = await inspectionService.CreateOrUpdateTagInspectionMetadata(
                    newMetadata
                );

                return metadata;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error while creating or updating inspection tag metadata");
                throw;
            }
        }

        /// <summary>
        /// Lookup the inspection image for task with specified isarInspectionId
        /// </summary>
        /// <remarks>
        /// Retrieves the inspection image associated with the given ISAR Inspection ID.
        /// </remarks>
        [HttpGet("image/{isarInspectionId}")]
        [Authorize(Roles = Role.Any)]
        [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> GetInspectionImageByIsarInspectionId(
            [FromRoute] string isarInspectionId
        )
        {
            isarInspectionId = Sanitize.SanitizeUserInput(isarInspectionId);

            try
            {
                byte[]? inspectionStream =
                    await inspectionService.FetchInspectionImageFromIsarInspectionId(
                        isarInspectionId
                    );

                if (inspectionStream == null)
                {
                    logger.LogError(
                        "Could not fetch inspection with ISAR Inspection ID {isarInspectionId}",
                        isarInspectionId
                    );
                    return NotFound(
                        $"Could not fetch inspection with ISAR Inspection ID {isarInspectionId}"
                    );
                }

                return File(inspectionStream, "image/png");
            }
            catch (InspectionNotAvailableYetException)
            {
                logger.LogInformation(
                    "Inspection not available yet for ISAR Inspection ID {IsarInspectionId}",
                    isarInspectionId
                );
                return NotFound(
                    $"Inspection not available yet for ISAR Inspection ID {isarInspectionId}"
                );
            }
            catch (InspectionNotFoundException)
            {
                logger.LogWarning(
                    "Could not find inspection image with ISAR Inspection ID {IsarInspectionId}",
                    isarInspectionId
                );
                return NotFound(
                    $"Could not find inspection image with ISAR Inspection ID{isarInspectionId}"
                );
            }
            catch (Exception e)
            {
                logger.LogError(
                    e,
                    "Could not find inspection image with ISAR Inspection ID {IsarInspectionId}",
                    isarInspectionId
                );
                return NotFound(
                    $"Could not find inspection image with ISAR Inspection ID{isarInspectionId}."
                );
            }
        }

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

            if (task.Inspection?.InspectionType != InspectionType.CO2Measurement)
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

        /// <summary>
        /// Lookup the visualized image for task with specified isarInspectionId
        /// </summary>
        /// <remarks>
        /// Retrieves the visualized image associated with the given ISAR Inspection ID.
        /// </remarks>
        [HttpGet("analysis/{isarInspectionId}")]
        [Authorize(Roles = Role.User)]
        [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetAnalysisImageByIsarInspectionId(
            [FromRoute] string isarInspectionId
        )
        {
            isarInspectionId = Sanitize.SanitizeUserInput(isarInspectionId);

            try
            {
                byte[]? inspectionStream =
                    await inspectionService.FetchAnalysisFromIsarInspectionId(isarInspectionId);

                if (inspectionStream == null)
                {
                    logger.LogError(
                        "Could not fetch analysis for inspection with ISAR Inspection ID {isarInspectionId}",
                        isarInspectionId
                    );
                    return NotFound(
                        $"Could not fetch analysis for inspection with ISAR Inspection ID {isarInspectionId}"
                    );
                }

                return File(inspectionStream, "image/png");
            }
            catch (Exception e)
            {
                logger.LogError(
                    e,
                    "Could not find visualized image with ISAR Inspection ID {IsarInspectionId}",
                    isarInspectionId
                );
                return NotFound(
                    $"Could not find visualized image with ISAR Inspection ID {isarInspectionId}."
                );
            }
        }
    }
}
