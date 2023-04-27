using Api.Controllers.Models;
using Api.Database.Models;
using Api.Services;
using Api.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("asset-decks")]
public class AssetDeckController : ControllerBase
{
    private readonly IAssetDeckService _assetDeckService;

    private readonly ILogger<AssetDeckController> _logger;

    public AssetDeckController(
        ILogger<AssetDeckController> logger,
        IAssetDeckService assetDeckService
    )
    {
        _logger = logger;
        _assetDeckService = assetDeckService;
    }

    /// <summary>
    /// List all asset decks in the Flotilla database
    /// </summary>
    /// <remarks>
    /// <para> This query gets all asset decks </para>
    /// </remarks>
    [HttpGet]
    [Authorize(Roles = Role.Any)]
    [ProducesResponseType(typeof(IList<AssetDeck>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IList<AssetDeck>>> GetAssetDecks()
    {
        try
        {
            var assetDecks = await _assetDeckService.ReadAll();
            return Ok(assetDecks);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error during GET of asset decks from database");
            throw;
        }
    }

    /// <summary>
    /// Lookup asset deck by specified id.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = Role.Any)]
    [Route("{id}")]
    [ProducesResponseType(typeof(AssetDeck), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AssetDeck>> GetAssetDeckById([FromRoute] string id)
    {
        var assetDeck = await _assetDeckService.ReadById(id);
        if (assetDeck == null)
            return NotFound($"Could not find assetDeck with id {id}");
        return Ok(assetDeck);
    }

    /// <summary>
    /// Add a new asset deck
    /// </summary>
    /// <remarks>
    /// <para> This query adds a new asset deck to the database </para>
    /// </remarks>
    [HttpPost]
    [Authorize(Roles = Role.User)]
    [ProducesResponseType(typeof(AssetDeck), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AssetDeck>> Create([FromBody] CreateAssetDeckQuery assetDeck)
    {
        _logger.LogInformation("Creating new asset deck");
        try
        {
            var existingAssetDeck = await _assetDeckService.ReadByAssetAndDeck(assetDeck.AssetCode, assetDeck.DeckName);
            if (existingAssetDeck != null)
            {
                _logger.LogInformation("An asset deck for given deck and asset already exists");
                return BadRequest($"Asset deck already exists");
            }

            var newAssetDeck = await _assetDeckService.Create(assetDeck);
            _logger.LogInformation(
                "Succesfully created new asset deck with id '{assetDeckId}'",
                newAssetDeck.Id
            );
            return CreatedAtAction(
                nameof(GetAssetDeckById),
                new { id = newAssetDeck.Id },
                newAssetDeck
            );
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while creating new asset deck");
            throw;
        }
    }


    /// <summary>
    /// Add a safe position to a asset deck
    /// </summary>
    /// <remarks>
    /// <para> This query adds a new safe position to the database </para>
    /// </remarks>
    [HttpPost]
    [Authorize(Roles = Role.Admin)]
    [Route("{asset}/{deck}/safe-position")]
    [ProducesResponseType(typeof(AssetDeck), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AssetDeck>> AddSafePosition(
        [FromRoute] string asset,
        [FromRoute] string deck,
        [FromBody] Pose safePosition
    )
    {
        _logger.LogInformation("Adding new safe position");
        try
        {
            var assetDeck = await _assetDeckService.AddSafePosition(asset, deck, new SafePosition(safePosition));
            if (assetDeck != null)
            {
                _logger.LogInformation("Succesfully added new safe position for asset '{assetId}' and deck '{deckId}'", asset, deck);
                return CreatedAtAction(nameof(GetAssetDeckById), new { id = assetDeck.Id }, assetDeck); ;
            }
            else
            {
                _logger.LogInformation("Creating AssetDeck for asset '{assetId}' and deck '{deckId}'", asset, deck);
                // Cloning to avoid tracking same object
                var tempPose = ObjectCopier.Clone(safePosition);
                assetDeck = await _assetDeckService.Create(
                    new CreateAssetDeckQuery
                    {
                        AssetCode = asset,
                        DeckName = deck,
                        DefaultLocalizationPose = new Pose()
                    },
                    new List<Pose> { tempPose }
                );
                return CreatedAtAction(nameof(GetAssetDeckById), new { id = assetDeck.Id }, assetDeck);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while creating or adding new safe zone");
            throw;
        }
    }

    /// <summary>
    /// Deletes the asset deck with the specified id from the database.
    /// </summary>
    [HttpDelete]
    [Authorize(Roles = Role.Admin)]
    [Route("{id}")]
    [ProducesResponseType(typeof(AssetDeck), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AssetDeck>> DeleteAssetDeck([FromRoute] string id)
    {
        var assetDeck = await _assetDeckService.Delete(id);
        if (assetDeck is null)
            return NotFound($"Asset deck with id {id} not found");
        return Ok(assetDeck);
    }
}
