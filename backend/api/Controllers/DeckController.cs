using Api.Controllers.Models;
using Api.Database.Models;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController]
    [Route("decks")]
    public class DeckController : ControllerBase
    {
        private readonly IDeckService _deckService;
        private readonly IInstallationService _installationService;
        private readonly IPlantService _plantService;
        private readonly ILocalizationPoseService _localizationPoseService;

        private readonly IMapService _mapService;

        private readonly ILogger<DeckController> _logger;

        public DeckController(
            ILogger<DeckController> logger,
            IMapService mapService,
            IDeckService deckService,
            IInstallationService installationService,
            IPlantService plantService,
            ILocalizationPoseService localizationPoseService
        )
        {
            _logger = logger;
            _mapService = mapService;
            _deckService = deckService;
            _installationService = installationService;
            _plantService = plantService;
            _localizationPoseService = localizationPoseService;
        }

        /// <summary>
        /// List all decks in the Flotilla database
        /// </summary>
        /// <remarks>
        /// <para> This query gets all decks </para>
        /// </remarks>
        [HttpGet]
        [Authorize(Roles = Role.Any)]
        [ProducesResponseType(typeof(IList<Deck>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IList<Deck>>> GetDecks()
        {
            try
            {
                var decks = await _deckService.ReadAll();
                return Ok(decks);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error during GET of decks from database");
                throw;
            }
        }

        /// <summary>
        /// Lookup deck by specified id.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = Role.Any)]
        [Route("{id}")]
        [ProducesResponseType(typeof(Deck), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<Deck>> GetDeckById([FromRoute] string id)
        {
            try
            {
                var deck = await _deckService.ReadById(id);
                if (deck == null)
                    return NotFound($"Could not find deck with id {id}");
                return Ok(deck);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error during GET of deck from database");
                throw;
            }

        }

        /// <summary>
        /// Add or update the localization pose to a deck
        /// </summary>
        /// <remarks>
        /// <para> This query updates an existing deck with a new localization pose </para>
        /// </remarks>
        [HttpPut]
        [Route("add-localization-pose/{deckId}")]
        [Authorize(Roles = Role.Admin)]
        [ProducesResponseType(typeof(Deck), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<Deck>> AddLocalizationPoseToDeck([FromRoute] string deckId, [FromBody] Pose localizationPose)
        {
            _logger.LogInformation("Updating localization pose to an existing deck");
            try
            {

                var existingDeck = await _deckService.ReadById(deckId);
                if (existingDeck == null)
                {
                    _logger.LogInformation("Could not find the deck");
                    return BadRequest($"Deck already exists");
                }


                if (existingDeck.LocalizationPose != null)
                {
                    _logger.LogInformation("Removing old localization pose");
                    LocalizationPose? oldLocalizationPose = await _localizationPoseService.Delete(existingDeck.LocalizationPose.Id);
                }

                var newLocalizationPose = await _localizationPoseService.Create(localizationPose);
                existingDeck.LocalizationPose = newLocalizationPose;
                var updateDeck = await _deckService.Update(existingDeck);
                _logger.LogInformation(
                    "Succesfully created new deck with id '{deckId}'",
                    updateDeck.Id
                );
                return CreatedAtAction(
                    nameof(GetDeckById),
                    new { id = updateDeck.Id },
                    updateDeck
                );
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error while adding a localization pose to deck");
                throw;
            }
        }

        /// <summary>
        /// Add a new deck
        /// </summary>
        /// <remarks>
        /// <para> This query adds a new deck to the database </para>
        /// </remarks>
        [HttpPost]
        [Authorize(Roles = Role.Admin)]
        [ProducesResponseType(typeof(Deck), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<Deck>> Create([FromBody] CreateDeckQuery deck)
        {
            _logger.LogInformation("Creating new deck");
            try
            {
                var existingInstallation = await _installationService.ReadByName(deck.InstallationCode);
                if (existingInstallation == null)
                {
                    return NotFound($"Could not find installation with name {deck.InstallationCode}");
                }
                var existingPlant = await _plantService.ReadByInstallationAndName(existingInstallation, deck.PlantCode);
                if (existingPlant == null)
                {
                    return NotFound($"Could not find plant with name {deck.PlantCode}");
                }
                var existingDeck = await _deckService.ReadByInstallationAndPlantAndName(existingInstallation, existingPlant, deck.Name);
                if (existingDeck != null)
                {
                    _logger.LogInformation("An deck for given name and deck already exists");
                    return BadRequest($"Deck already exists");
                }

                var newDeck = await _deckService.Create(deck);
                _logger.LogInformation(
                    "Succesfully created new deck with id '{deckId}'",
                    newDeck.Id
                );
                return CreatedAtAction(
                    nameof(GetDeckById),
                    new { id = newDeck.Id },
                    newDeck
                );
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error while creating new deck");
                throw;
            }
        }

        /// <summary>
        /// Deletes the deck with the specified id from the database.
        /// </summary>
        [HttpDelete]
        [Authorize(Roles = Role.Admin)]
        [Route("{id}")]
        [ProducesResponseType(typeof(Deck), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<Deck>> DeleteDeck([FromRoute] string id)
        {
            var deck = await _deckService.Delete(id);
            if (deck is null)
                return NotFound($"Deck with id {id} not found");
            return Ok(deck);
        }
    }
}
