using Api.Controllers.Models;
using Api.Database.Models;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController]
    [Route("plants")]
    public class PlantController(
            ILogger<PlantController> logger,
            IPlantService plantService,
            IInstallationService installationService
        ) : ControllerBase
    {
        /// <summary>
        /// List all plants in the Flotilla database
        /// </summary>
        /// <remarks>
        /// <para> This query gets all plants </para>
        /// </remarks>
        [HttpGet]
        [Authorize(Roles = Role.Any)]
        [ProducesResponseType(typeof(IList<Plant>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IList<Plant>>> GetPlants()
        {
            try
            {
                var plants = await plantService.ReadAll();
                return Ok(plants);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error during GET of plants from database");
                throw;
            }
        }

        /// <summary>
        /// Lookup plant by specified id.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = Role.Any)]
        [Route("{id}")]
        [ProducesResponseType(typeof(Plant), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<Plant>> GetPlantById([FromRoute] string id)
        {
            try
            {
                var plant = await plantService.ReadById(id);
                if (plant == null)
                    return NotFound($"Could not find plant with id {id}");
                return Ok(plant);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error during GET of plant from database");
                throw;
            }

        }

        /// <summary>
        /// Add a new plant
        /// </summary>
        /// <remarks>
        /// <para> This query adds a new plant to the database </para>
        /// </remarks>
        [HttpPost]
        [Authorize(Roles = Role.Admin)]
        [ProducesResponseType(typeof(Plant), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<Plant>> Create([FromBody] CreatePlantQuery plant)
        {
            logger.LogInformation("Creating new plant");
            try
            {
                var existingInstallation = await installationService.ReadByName(plant.InstallationCode, readOnly: true);
                if (existingInstallation == null)
                {
                    return NotFound($"Installation with installation code {plant.InstallationCode} not found");
                }
                var existingPlant = await plantService.ReadByInstallationAndName(existingInstallation, plant.PlantCode);
                if (existingPlant != null)
                {
                    logger.LogInformation("A plant for given name and plant already exists");
                    return BadRequest($"Plant already exists");
                }

                var newPlant = await plantService.Create(plant);
                logger.LogInformation(
                    "Succesfully created new plant with id '{plantId}'",
                    newPlant.Id
                );
                return CreatedAtAction(
                    nameof(GetPlantById),
                    new { id = newPlant.Id },
                    newPlant
                );
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error while creating new plant");
                throw;
            }
        }

        /// <summary>
        /// Deletes the plant with the specified id from the database.
        /// </summary>
        [HttpDelete]
        [Authorize(Roles = Role.Admin)]
        [Route("{id}")]
        [ProducesResponseType(typeof(Plant), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<Plant>> DeletePlant([FromRoute] string id)
        {
            var plant = await plantService.Delete(id);
            if (plant is null)
                return NotFound($"Plant with id {id} not found");
            return Ok(plant);
        }
    }
}
