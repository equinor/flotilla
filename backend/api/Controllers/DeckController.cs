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
        private readonly IAssetService _assetService;
        private readonly IInstallationService _installationService;

        private readonly IMapService _mapService;

        private readonly ILogger<DeckController> _logger;

        public DeckController(
            ILogger<DeckController> logger,
            IMapService mapService,
            IDeckService deckService,
            IAssetService assetService,
            IInstallationService installationService
        )
        {
            _logger = logger;
            _mapService = mapService;
            _deckService = deckService;
            _assetService = assetService;
            _installationService = installationService;
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
                var existingAsset = await _assetService.ReadByName(deck.AssetCode);
                if (existingAsset == null)
                {
                    return NotFound($"Could not find asset with name {deck.AssetCode}");
                }
                var existingInstallation = await _installationService.ReadByAssetAndName(existingAsset, deck.InstallationCode);
                if (existingInstallation == null)
                {
                    return NotFound($"Could not find installation with name {deck.InstallationCode}");
                }
                var existingDeck = await _deckService.ReadByAssetAndInstallationAndName(existingAsset, existingInstallation, deck.Name);
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
