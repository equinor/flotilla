using Api.Controllers.Models;
using Api.Database.Models;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController]
    [Route("localization-pose")]
    public class LocalizationPoseController : ControllerBase
    {
        private readonly ILocalizationPoseService _localizationPoseService;
        private readonly IInstallationService _installationService;
        private readonly IPlantService _plantService;

        private readonly IMapService _mapService;

        private readonly ILogger<LocalizationPoseController> _logger;

        public LocalizationPoseController(
            ILogger<LocalizationPoseController> logger,
            IMapService mapService,
            ILocalizationPoseService localizationPoseService,
            IInstallationService installationService,
            IPlantService plantService
        )
        {
            _logger = logger;
            _mapService = mapService;
            _localizationPoseService = localizationPoseService;
            _installationService = installationService;
            _plantService = plantService;
        }

        /// <summary>
        /// List all decks in the Flotilla database
        /// </summary>
        /// <remarks>
        /// <para> This query gets all decks </para>
        /// </remarks>
        [HttpGet]
        [Authorize(Roles = Role.Any)]
        [ProducesResponseType(typeof(IList<LocalizationPose>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IList<LocalizationPose>>> GetLocalizationPoses()
        {
            try
            {
                var decks = await _localizationPoseService.ReadAll();
                return Ok(decks);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error during GET of decks from database");
                throw;
            }
        }

        /// <summary>
        /// Lookup localization pose by specified id.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = Role.Any)]
        [Route("{id}")]
        [ProducesResponseType(typeof(LocalizationPose), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<LocalizationPose>> GetLocalizationPoseById([FromRoute] string id)
        {
            try
            {
                var localizationPose = await _localizationPoseService.ReadById(id);
                if (localizationPose == null)
                    return NotFound($"Could not find localization pose with id {id}");
                return Ok(localizationPose);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error during GET of localization pose from database");
                throw;
            }

        }

        /// <summary>
        /// Add a new localization pose
        /// </summary>
        /// <remarks>
        /// <para> This query adds a new localization pose to the database </para>
        /// </remarks>
        [HttpPost]
        [Authorize(Roles = Role.Admin)]
        [ProducesResponseType(typeof(LocalizationPose), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<LocalizationPose>> Create([FromBody] CreateLocalizationPoseQuery localizationPoseQuery)
        {
            _logger.LogInformation("Creating new localization pose");
            try
            {
                var newLocalizationPose = await _localizationPoseService.Create(localizationPoseQuery.Pose);
                _logger.LogInformation(
                    "Succesfully created new localization pose with id '{deckId}'",
                    newLocalizationPose.Id
                );
                return CreatedAtAction(
                    nameof(GetLocalizationPoseById),
                    new { id = newLocalizationPose.Id },
                    newLocalizationPose
                );
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error while creating new localization pose");
                throw;
            }
        }

        /// <summary>
        /// Deletes the localization pose with the specified id from the database.
        /// </summary>
        [HttpDelete]
        [Authorize(Roles = Role.Admin)]
        [Route("{id}")]
        [ProducesResponseType(typeof(LocalizationPose), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<LocalizationPose>> DeleteLocalizationPose([FromRoute] string id)
        {
            var localizationPose = await _localizationPoseService.Delete(id);
            if (localizationPose is null)
                return NotFound($"LocalizationPose with id {id} not found");
            return Ok(localizationPose);
        }
    }
}
