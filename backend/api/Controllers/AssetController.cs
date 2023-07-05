using Api.Controllers.Models;
using Api.Database.Models;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController]
    [Route("assets")]
    public class AssetController : ControllerBase
    {
        private readonly IAssetService _assetService;

        private readonly IMapService _mapService;

        private readonly ILogger<AssetController> _logger;

        public AssetController(
            ILogger<AssetController> logger,
            IMapService mapService,
            IAssetService assetService
        )
        {
            _logger = logger;
            _mapService = mapService;
            _assetService = assetService;
        }

        /// <summary>
        /// List all assets in the Flotilla database
        /// </summary>
        /// <remarks>
        /// <para> This query gets all assets </para>
        /// </remarks>
        [HttpGet]
        [Authorize(Roles = Role.Any)]
        [ProducesResponseType(typeof(IList<Asset>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IList<Asset>>> GetAssets()
        {
            try
            {
                var assets = await _assetService.ReadAll();
                return Ok(assets);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error during GET of assets from database");
                throw;
            }
        }

        /// <summary>
        /// Lookup asset by specified id.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = Role.Any)]
        [Route("{id}")]
        [ProducesResponseType(typeof(Asset), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<Asset>> GetAssetById([FromRoute] string id)
        {
            try
            {
                var asset = await _assetService.ReadById(id);
                if (asset == null)
                    return NotFound($"Could not find asset with id {id}");
                return Ok(asset);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error during GET of asset from database");
                throw;
            }

        }

        /// <summary>
        /// Add a new asset
        /// </summary>
        /// <remarks>
        /// <para> This query adds a new asset to the database </para>
        /// </remarks>
        [HttpPost]
        [Authorize(Roles = Role.Admin)]
        [ProducesResponseType(typeof(Asset), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<Asset>> Create([FromBody] CreateAssetQuery asset)
        {
            _logger.LogInformation("Creating new asset");
            try
            {
                var existingAsset = await _assetService.ReadByName(asset.AssetCode);
                if (existingAsset != null)
                {
                    _logger.LogInformation("An asset for given name and asset already exists");
                    return BadRequest($"Asset already exists");
                }

                var newAsset = await _assetService.Create(asset);
                _logger.LogInformation(
                    "Succesfully created new asset with id '{assetId}'",
                    newAsset.Id
                );
                return CreatedAtAction(
                    nameof(GetAssetById),
                    new { id = newAsset.Id },
                    newAsset
                );
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error while creating new asset");
                throw;
            }
        }

        /// <summary>
        /// Deletes the asset with the specified id from the database.
        /// </summary>
        [HttpDelete]
        [Authorize(Roles = Role.Admin)]
        [Route("{id}")]
        [ProducesResponseType(typeof(Asset), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<Asset>> DeleteAsset([FromRoute] string id)
        {
            var asset = await _assetService.Delete(id);
            if (asset is null)
                return NotFound($"Asset with id {id} not found");
            return Ok(asset);
        }
    }
}
