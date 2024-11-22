﻿using System.Globalization;
using Api.Controllers.Models;
using Api.Database.Models;

using Api.Services;
using Api.Services.MissionLoaders;
using Api.Services.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController]
    [Route("inspection")]
    public class InspectionController(
            ILogger<InspectionController> logger,
            IEchoService echoService,
            IInspectionService inspectionService
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
        public async Task<ActionResult<TagInspectionMetadata>> Create([FromRoute] string tagId, [FromBody] IsarZoomDescription zoom)
        {
            logger.LogInformation($"Updating zoom value for tag with ID {tagId}");

            var newMetadata = new TagInspectionMetadata
            {
                TagId = tagId,
                ZoomDescription = zoom
            };

            try
            {
                var metadata = await echoService.CreateOrUpdateTagInspectionMetadata(newMetadata);

                return metadata;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error while creating or updating inspection tag metadata");
                throw;
            }
        }

        /// <summary>
        /// Lookup the inspection image for task with specified isarTaskId
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

            if (inspection.Id == null) return NotFound($"Could not find Id for Inspection with task ID {taskId}.");

            var inspectionData = await inspectionService.GetInspectionStorageInfo(inspection.Id);

            if (inspectionData == null) return NotFound($"Could not find inspection data for inspection with Id {inspection.Id}.");

            if (!inspectionData.BlobContainer.ToLower(CultureInfo.CurrentCulture).Equals(installationCode.ToLower(CultureInfo.CurrentCulture), StringComparison.Ordinal))
            {
                return NotFound($"Could not find inspection data for inspection with Id {inspection.Id} because blob name {inspectionData.BlobName} does not match installation {installationCode}.");
            }

            try
            {
                byte[] inspectionStream = await inspectionService.FetchInpectionImage(inspectionData.BlobName, inspectionData.BlobContainer, inspectionData.StorageAccount);
                return File(inspectionStream, "image/png");
            }
            catch (Azure.RequestFailedException)
            {
                return NotFound($"Could not find inspection blob {inspectionData.BlobName} in container {inspectionData.BlobContainer} and storage account {inspectionData.StorageAccount}.");
            }
        }
    }
}
