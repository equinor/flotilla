﻿using System.Globalization;
using System.Text.Json;
using Api.Controllers.Models;
using Api.Database.Models;
using Api.Services;
using Api.Services.MissionLoaders;
using Api.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Api.Controllers
{
    [ApiController]
    [Route("mission-loader")]
    [Authorize(Roles = Role.Any)]
    public class MissionLoaderController(ILogger<MissionLoaderController> logger, IMissionLoader missionLoader, IRobotService robotService) : ControllerBase
    {
        /// <summary>
        ///     List all available missions for the installation
        /// </summary>
        /// <remarks>
        ///     These missions are fetched based on your mission loader
        /// </remarks>
        [HttpGet]
        [Route("available-missions/{installationCode}")]
        [ProducesResponseType(typeof(IList<MissionDefinitionResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status502BadGateway)]
        public async Task<ActionResult<IList<MissionDefinitionResponse>>> GetAvailableMissions(
            [FromRoute] string? installationCode)
        {
            IQueryable<MissionDefinition> missionDefinitions;
            try
            {
                missionDefinitions = await missionLoader.GetAvailableMissions(installationCode);
            }
            catch (InvalidDataException e)
            {
                logger.LogError(e, "{ErrorMessage}", e.Message);
                return BadRequest(e.Message);
            }
            catch (HttpRequestException e)
            {
                logger.LogError(e, "Error retrieving missions from Mission Loader");
                return new StatusCodeResult(StatusCodes.Status502BadGateway);
            }
            catch (JsonException e)
            {
                logger.LogError(e, "Error retrieving missions from MissionLoader");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }

            var missionDefinitionResponses = missionDefinitions.Select(m => new MissionDefinitionResponse(m)).ToList();
            return Ok(missionDefinitionResponses);
        }

        /// <summary>
        ///     Lookup mission by Id
        /// </summary>
        /// <remarks>
        ///     This mission is loaded from the mission loader
        /// </remarks>
        [HttpGet]
        [Route("missions/{missionId}")]
        [ProducesResponseType(typeof(MissionDefinitionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status502BadGateway)]
        public async Task<ActionResult<MissionDefinitionResponse>> GetMissionDefinition([FromRoute] string missionId)
        {
            try
            {
                var mission = await missionLoader.GetMissionById(missionId);
                return Ok(mission);
            }
            catch (HttpRequestException e)
            {
                if (e.StatusCode.HasValue && (int)e.StatusCode.Value == 404)
                {
                    logger.LogWarning("Could not find mission with id={id}", missionId);
                    return NotFound("Mission not found");
                }

                logger.LogError(e, "Error getting mission from mission loader");
                return new StatusCodeResult(StatusCodes.Status502BadGateway);
            }
            catch (JsonException e)
            {
                logger.LogError(e, "Error deserializing mission from mission loader");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
            catch (InvalidDataException e)
            {
                string message =
                    "Mission invalid: One or more tags are missing associated robot poses.";
                logger.LogError(e, message);
                return StatusCode(StatusCodes.Status502BadGateway, message);
            }
        }

        /// <summary>
        ///     Get selected information on all the plants
        /// </summary>
        [HttpGet]
        [Route("plants")]
        [ProducesResponseType(typeof(List<PlantInfo>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status502BadGateway)]
        public async Task<ActionResult<PlantInfo>> GetPlantInfos()
        {
            try
            {
                var plantInfos = await missionLoader.GetPlantInfos();
                return Ok(plantInfos);
            }
            catch (HttpRequestException e)
            {
                logger.LogError(e, "Error getting plant info");
                return new StatusCodeResult(StatusCodes.Status502BadGateway);
            }
            catch (JsonException e)
            {
                logger.LogError(e, "Error deserializing plant info response");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        ///     Get all plants associated with an active robot.
        /// </summary>
        /// <remarks>
        ///     <para> Retrieves the plants that have an active robot </para>
        /// </remarks>
        [HttpGet]
        [Authorize(Roles = Role.User)]
        [Route("active-plants")]
        [ProducesResponseType(typeof(IList<PlantInfo>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IList<PlantInfo>>> GetActivePlants()
        {
            var plants = await robotService.ReadAllActivePlants(readOnly: true);

            if (plants == null)
            {
                logger.LogWarning("Could not retrieve robot plants information");
                throw new RobotInformationNotAvailableException("Could not retrieve robot plants information");
            }

            plants = plants.Select(p => p.ToLower(CultureInfo.CurrentCulture));

            try
            {
                var plantInfos = await missionLoader.GetPlantInfos();

                plantInfos = plantInfos.Where(p => plants.Contains(p.PlantCode.ToLower(CultureInfo.CurrentCulture))).ToList();
                return Ok(plantInfos);
            }
            catch (HttpRequestException e)
            {
                logger.LogError(e, "Error getting plant info");
                return new StatusCodeResult(StatusCodes.Status502BadGateway);
            }
            catch (JsonException e)
            {
                logger.LogError(e, "Error deserializing plant info response");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
