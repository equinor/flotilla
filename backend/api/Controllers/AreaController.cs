﻿using Api.Controllers.Models;
using Api.Database.Models;
using Api.Services;
using Api.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController]
    [Route("areas")]
    public class AreaController(
        ILogger<AreaController> logger,
        IAreaService areaService,
        IMissionDefinitionService missionDefinitionService
    ) : ControllerBase
    {
        /// <summary>
        /// Add a new area
        /// </summary>
        /// <remarks>
        /// <para> This query adds a new area to the database </para>
        /// </remarks>
        [HttpPost]
        [Authorize(Roles = Role.Admin)]
        [ProducesResponseType(typeof(AreaResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<AreaResponse>> Create([FromBody] CreateAreaQuery area)
        {
            logger.LogInformation("Creating new area");
            try
            {
                var existingArea = await areaService.ReadByInstallationAndName(
                    area.InstallationCode,
                    area.AreaName,
                    readOnly: true
                );
                if (existingArea != null)
                {
                    logger.LogWarning("An area for given name and installation already exists");
                    return Conflict($"Area already exists");
                }

                var newArea = await areaService.Create(area);
                logger.LogInformation(
                    "Succesfully created new area with id '{areaId}'",
                    newArea.Id
                );
                var response = new AreaResponse(newArea);
                return CreatedAtAction(nameof(GetAreaById), new { id = newArea.Id }, response);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error while creating new area");
                throw;
            }
        }

        /// <summary>
        /// Deletes the area with the specified id from the database.
        /// </summary>
        [HttpDelete]
        [Authorize(Roles = Role.Admin)]
        [Route("{id}")]
        [ProducesResponseType(typeof(AreaResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<AreaResponse>> DeleteArea([FromRoute] string id)
        {
            var area = await areaService.Delete(id);
            if (area is null)
                return NotFound($"Area with id {id} not found");

            if (area.InspectionArea == null || area.Plant == null || area.Installation == null)
            {
                string errorMessage = "Inspection area, plant or installation missing from area";
                logger.LogWarning(errorMessage);
                return StatusCode(StatusCodes.Status500InternalServerError, errorMessage);
            }

            var response = new AreaResponse(area);
            return Ok(response);
        }

        /// <summary>
        /// List all installation areas in the Flotilla database
        /// </summary>
        /// <remarks>
        /// <para> This query gets all installation areas </para>
        /// </remarks>
        [HttpGet]
        [Authorize(Roles = Role.Any)]
        [ProducesResponseType(typeof(IList<AreaResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IList<AreaResponse>>> GetAreas(
            [FromQuery] AreaQueryStringParameters parameters
        )
        {
            PagedList<Area> areas;
            try
            {
                areas = await areaService.ReadAll(parameters, readOnly: true);
                var response = areas.Select(area => new AreaResponse(area));
                return Ok(response);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error during GET of areas from database");
                throw;
            }
        }

        /// <summary>
        /// Lookup area by specified id.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = Role.Any)]
        [Route("{id}")]
        [ProducesResponseType(typeof(AreaResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<AreaResponse>> GetAreaById([FromRoute] string id)
        {
            try
            {
                var area = await areaService.ReadById(id, readOnly: true);
                if (area == null)
                    return NotFound($"Could not find area with id {id}");

                if (area.InspectionArea == null || area.Plant == null || area.Installation == null)
                {
                    string errorMessage =
                        "Inspection area, plant or installation missing from area";
                    logger.LogWarning(errorMessage);
                    return StatusCode(StatusCodes.Status500InternalServerError, errorMessage);
                }

                var response = new AreaResponse(area);
                return Ok(response);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error during GET of areas from database");
                throw;
            }
        }

        /// <summary>
        /// Lookup area by specified inspection area id.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = Role.Any)]
        [Route("inspection-area/{inspectionAreaId}")]
        [ProducesResponseType(typeof(AreaResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IList<AreaResponse>>> GetAreaByInspectionAreaId(
            [FromRoute] string inspectionAreaId
        )
        {
            try
            {
                var areas = await areaService.ReadByInspectionAreaId(
                    inspectionAreaId,
                    readOnly: true
                );
                if (!areas.Any())
                    return NotFound(
                        $"Could not find area for inspection area with id {inspectionAreaId}"
                    );

                var response = areas.Select(area => new AreaResponse(area!));
                return Ok(response);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error during GET of areas from database");
                throw;
            }
        }

        /// <summary>
        /// Lookup all the mission definitions related to an area
        /// </summary>
        [HttpGet]
        [Authorize(Roles = Role.Any)]
        [Route("{id}/mission-definitions")]
        [ProducesResponseType(typeof(MissionDefinitionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<
            ActionResult<IList<MissionDefinitionResponse>>
        > GetMissionDefinitionsInArea([FromRoute] string id)
        {
            try
            {
                var area = await areaService.ReadById(id, readOnly: true);
                if (area == null)
                    return NotFound($"Could not find area with id {id}");

                var missionDefinitions = await missionDefinitionService.ReadByInspectionAreaId(
                    area.InspectionArea.Id,
                    readOnly: true
                );
                var missionDefinitionResponses = missionDefinitions
                    .FindAll(m => !m.IsDeprecated)
                    .Select(m => new MissionDefinitionResponse(m));
                return Ok(missionDefinitionResponses);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error during GET of area missions from database");
                throw;
            }
        }
    }
}
