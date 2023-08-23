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
        private readonly IMissionDefinitionService _missionDefinitionService;
        private readonly IPlantService _plantService;

        private readonly IMapService _mapService;
        private readonly IAreaService _areaService;

        private readonly ILogger<DeckController> _logger;

        public DeckController(
            ILogger<DeckController> logger,
            IMapService mapService,
            IAreaService areaService,
            IDeckService deckService,
            IInstallationService installationService,
            IPlantService plantService,
            IMissionDefinitionService missionDefinitionService
        )
        {
            _logger = logger;
            _mapService = mapService;
            _areaService = areaService;
            _deckService = deckService;
            _installationService = installationService;
            _plantService = plantService;
            _missionDefinitionService = missionDefinitionService;
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
        public async Task<ActionResult<IList<DeckResponse>>> GetDecks()
        {
            try
            {
                var decks = await _deckService.ReadAll();
                var deckResponses = decks.Select(d => new DeckResponse(d)).ToList();
                return Ok(deckResponses);
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
                return Ok(new DeckResponse(deck));
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error during GET of deck from database");
                throw;
            }
        }

        /// <summary>
        /// Lookup all the mission definitions related to a deck
        /// </summary>
        [HttpGet]
        [Authorize(Roles = Role.Any)]
        [Route("{id}/mission-definitions")]
        [ProducesResponseType(typeof(MissionDefinitionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IList<MissionDefinitionResponse>>> GetMissionDefinitionsInDeck([FromRoute] string id)
        {
            try
            {
                var deck = await _deckService.ReadById(id);
                if (deck == null)
                    return NotFound($"Could not find deck with id {id}");

                var missionDefinitions = await _missionDefinitionService.ReadByDeckId(deck.Id);
                var missionDefinitionResponses = missionDefinitions.FindAll(m => !m.IsDeprecated).Select(m => new CondensedMissionDefinitionResponse(m));
                return Ok(missionDefinitionResponses);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error during GET of deck missions from database");
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
        /// Set default localization area for a deck
        /// </summary>
        /// <remarks>
        /// <para> This query sets the default localization area for a deck </para>
        /// </remarks>
        [HttpPost]
        [Authorize(Roles = Role.Admin)]
        [Route("{deckId}/{areaId}/set-default-localization-area")]
        [ProducesResponseType(typeof(AreaResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<Deck>> SetDefaultLocalizationArea(
            [FromRoute] string deckId,
            [FromRoute] string areaId
        )
        {
            _logger.LogInformation(@"Setting default localization area {AreaId} to deck {DeckId}", areaId, deckId);
            var deck = await _deckService.ReadById(deckId);
            if (deck == null)
                return NotFound($"Could not find deck with id {deckId}");

            var area = await _areaService.ReadById(areaId);
            if (area == null)
                return NotFound($"Could not find area with id {areaId}");

            if (area.Deck == null)
                return NotFound($"Area {areaId} is not linked to any deck");

            if (area.Deck.Id != deckId)
                return NotFound($"Area {areaId} is not linked to deck {deckId}");

            try
            {
                deck.DefaultLocalizationArea = area;
                var updatedDeck = await _deckService.Update(deck);
                return Ok(updatedDeck);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error while setting a default localization area");
                return StatusCode(StatusCodes.Status500InternalServerError);
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
