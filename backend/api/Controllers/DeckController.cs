using Api.Controllers.Models;
using Api.Database.Models;
using Api.Services;
using Azure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController]
    [Route("decks")]
    public class DeckController(
            ILogger<DeckController> logger,
            IMapService mapService,
            IDeckService deckService,
            IDefaultLocalizationPoseService defaultLocalizationPoseService,
            IInstallationService installationService,
            IPlantService plantService,
            IMissionDefinitionService missionDefinitionService
        ) : ControllerBase
    {
        /// <summary>
        /// List all decks in the Flotilla database
        /// </summary>
        /// <remarks>
        /// <para> This query gets all decks </para>
        /// </remarks>
        [HttpGet]
        [Authorize(Roles = Role.Any)]
        [ProducesResponseType(typeof(IList<DeckResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IList<DeckResponse>>> GetDecks()
        {
            try
            {
                var decks = await deckService.ReadAll();
                var deckResponses = decks.Select(d => new DeckResponse(d)).ToList();
                return Ok(deckResponses);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error during GET of decks from database");
                throw;
            }
        }

        /// <summary>
        /// List all decks in the Flotilla database
        /// </summary>
        /// <remarks>
        /// <para> This query gets all decks </para>
        /// </remarks>
        [HttpGet("installation/{installationCode}")]
        [Authorize(Roles = Role.Any)]
        [ProducesResponseType(typeof(IList<DeckResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IList<DeckResponse>>> GetDecksByInstallationCode([FromRoute] string installationCode)
        {
            try
            {
                var decks = await deckService.ReadByInstallation(installationCode);
                var deckResponses = decks.Select(d => new DeckResponse(d)).ToList();
                return Ok(deckResponses);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error during GET of decks from database");
                throw;
            }
        }

        /// <summary>
        /// Lookup deck by specified id.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = Role.Any)]
        [Route("{id}")]
        [ProducesResponseType(typeof(DeckResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<DeckResponse>> GetDeckById([FromRoute] string id)
        {
            try
            {
                var deck = await deckService.ReadById(id);
                if (deck == null)
                    return NotFound($"Could not find deck with id {id}");
                return Ok(new DeckResponse(deck));
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error during GET of deck from database");
                throw;
            }
        }

        /// <summary>
        /// Lookup all the mission definitions related to a deck
        /// </summary>
        [HttpGet]
        [Authorize(Roles = Role.Any)]
        [Route("{id}/mission-definitions")]
        [ProducesResponseType(typeof(CondensedMissionDefinitionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IList<CondensedMissionDefinitionResponse>>> GetMissionDefinitionsInDeck([FromRoute] string id)
        {
            try
            {
                var deck = await deckService.ReadById(id);
                if (deck == null)
                    return NotFound($"Could not find deck with id {id}");

                var missionDefinitions = await missionDefinitionService.ReadByDeckId(deck.Id);
                var missionDefinitionResponses = missionDefinitions.FindAll(m => !m.IsDeprecated).Select(m => new CondensedMissionDefinitionResponse(m));
                return Ok(missionDefinitionResponses);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error during GET of deck missions from database");
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
        [ProducesResponseType(typeof(DeckResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<DeckResponse>> Create([FromBody] CreateDeckQuery deck)
        {
            logger.LogInformation("Creating new deck");
            try
            {
                var existingInstallation = await installationService.ReadByName(deck.InstallationCode, readOnly: true);
                if (existingInstallation == null)
                {
                    return NotFound($"Could not find installation with name {deck.InstallationCode}");
                }
                var existingPlant = await plantService.ReadByInstallationAndName(existingInstallation, deck.PlantCode);
                if (existingPlant == null)
                {
                    return NotFound($"Could not find plant with name {deck.PlantCode}");
                }
                var existingDeck = await deckService.ReadByInstallationAndPlantAndName(existingInstallation, existingPlant, deck.Name);
                if (existingDeck != null)
                {
                    logger.LogInformation("An deck for given name and deck already exists");
                    return BadRequest($"Deck already exists");
                }

                var newDeck = await deckService.Create(deck);
                logger.LogInformation(
                    "Succesfully created new deck with id '{deckId}'",
                    newDeck.Id
                );
                return CreatedAtAction(
                    nameof(GetDeckById),
                    new { id = newDeck.Id },
                    new DeckResponse(newDeck)
                );
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error while creating new deck");
                throw;
            }
        }

        /// <summary>
        /// Updates default localization pose
        /// </summary>
        /// <remarks>
        /// <para> This query updates the default localization pose for a deck </para>
        /// </remarks>
        [HttpPut]
        [Authorize(Roles = Role.Admin)]
        [Route("{deckId}/update-default-localization-pose")]
        [ProducesResponseType(typeof(DeckResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<DeckResponse>> UpdateDefaultLocalizationPose([FromRoute] string deckId, [FromBody] CreateDefaultLocalizationPose newDefaultLocalizationPose)
        {
            logger.LogInformation("Updating default localization pose on deck '{deckId}'", deckId);
            try
            {
                var deck = await deckService.ReadById(deckId);
                if (deck is null)
                {
                    logger.LogInformation("A deck with id '{deckId}' does not exist", deckId);
                    return NotFound("Deck does not exists");
                }

                if (deck.DefaultLocalizationPose != null)
                {
                    deck.DefaultLocalizationPose.Pose = newDefaultLocalizationPose.Pose;
                    deck.DefaultLocalizationPose.DockingEnabled = newDefaultLocalizationPose.IsDockingStation;
                    _ = await defaultLocalizationPoseService.Update(deck.DefaultLocalizationPose);
                }
                else
                {
                    deck.DefaultLocalizationPose = new DefaultLocalizationPose(newDefaultLocalizationPose.Pose, newDefaultLocalizationPose.IsDockingStation);
                    deck = await deckService.Update(deck);
                }

                return Ok(new DeckResponse(deck));
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error while updating the default localization pose");
                throw;
            }
        }

        /// <summary>
        /// Deletes the deck with the specified id from the database.
        /// </summary>
        [HttpDelete]
        [Authorize(Roles = Role.Admin)]
        [Route("{id}")]
        [ProducesResponseType(typeof(DeckResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<DeckResponse>> DeleteDeck([FromRoute] string id)
        {
            var deck = await deckService.Delete(id);
            if (deck is null)
                return NotFound($"Deck with id {id} not found");
            return Ok(new DeckResponse(deck));
        }

        /// <summary>
        /// Gets map metadata for localization poses belonging to deck with specified id
        /// </summary>
        [HttpGet]
        [Authorize(Roles = Role.Any)]
        [Route("{id}/map-metadata")]
        [ProducesResponseType(typeof(MapMetadata), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<MapMetadata>> GetMapMetadata([FromRoute] string id)
        {
            var deck = await deckService.ReadById(id);
            if (deck is null)
            {
                string errorMessage = $"Deck not found for deck with ID {id}";
                logger.LogError("{ErrorMessage}", errorMessage);
                return NotFound(errorMessage);
            }
            if (deck.Installation == null)
            {
                string errorMessage = "Installation missing from deck";
                logger.LogWarning(errorMessage);
                return StatusCode(StatusCodes.Status500InternalServerError, errorMessage);
            }

            if (deck.DefaultLocalizationPose is null)
            {
                string errorMessage = $"Deck with id '{deck.Id}' does not have a default localization pose";
                logger.LogInformation("{ErrorMessage}", errorMessage);
                return NotFound(errorMessage);
            }

            MapMetadata? mapMetadata;
            var positions = new List<Position>
            {
                deck.DefaultLocalizationPose.Pose.Position
            };
            try
            {
                mapMetadata = await mapService.ChooseMapFromPositions(positions, deck.Installation.InstallationCode);
            }
            catch (RequestFailedException e)
            {
                string errorMessage = $"An error occurred while retrieving the map for deck {deck.Id}";
                logger.LogError(e, "{ErrorMessage}", errorMessage);
                return StatusCode(StatusCodes.Status502BadGateway, errorMessage);
            }
            catch (ArgumentOutOfRangeException e)
            {
                string errorMessage = $"Could not find a suitable map for deck {deck.Id}";
                logger.LogError(e, "{ErrorMessage}", errorMessage);
                return NotFound(errorMessage);
            }

            if (mapMetadata == null)
            {
                return NotFound("A map which contained at least half of the points in this mission could not be found");
            }
            return Ok(mapMetadata);
        }
    }
}
