using Api.Controllers.Models;
using Api.Database.Models;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController]
    [Route("installations")]
    public class InstallationController : ControllerBase
    {
        private readonly IInstallationService _installationService;
        private readonly IAssetService _assetService;

        private readonly IMapService _mapService;

        private readonly ILogger<InstallationController> _logger;

        public InstallationController(
            ILogger<InstallationController> logger,
            IMapService mapService,
            IInstallationService installationService,
            IAssetService assetService
        )
        {
            _logger = logger;
            _mapService = mapService;
            _installationService = installationService;
            _assetService = assetService;
        }

        /// <summary>
        /// List all installations in the Flotilla database
        /// </summary>
        /// <remarks>
        /// <para> This query gets all installations </para>
        /// </remarks>
        [HttpGet]
        [Authorize(Roles = Role.Any)]
        [ProducesResponseType(typeof(IList<Installation>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IList<Installation>>> GetInstallations()
        {
            try
            {
                var installations = await _installationService.ReadAll();
                return Ok(installations);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error during GET of installations from database");
                throw;
            }
        }

        /// <summary>
        /// Lookup installation by specified id.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = Role.Any)]
        [Route("{id}")]
        [ProducesResponseType(typeof(Installation), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<Installation>> GetInstallationById([FromRoute] string id)
        {
            try
            {
                var installation = await _installationService.ReadById(id);
                if (installation == null)
                    return NotFound($"Could not find installation with id {id}");
                return Ok(installation);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error during GET of installation from database");
                throw;
            }

        }

        /// <summary>
        /// Add a new installation
        /// </summary>
        /// <remarks>
        /// <para> This query adds a new installation to the database </para>
        /// </remarks>
        [HttpPost]
        [Authorize(Roles = Role.Admin)]
        [ProducesResponseType(typeof(Installation), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<Installation>> Create([FromBody] CreateInstallationQuery installation)
        {
            _logger.LogInformation("Creating new installation");
            try
            {
                var existingAsset = await _assetService.ReadByName(installation.AssetCode);
                if (existingAsset == null)
                {
                    return NotFound($"Asset with asset code {installation.AssetCode} not found");
                }
                var existingInstallation = await _installationService.ReadByAssetAndName(existingAsset, installation.InstallationCode);
                if (existingInstallation != null)
                {
                    _logger.LogInformation("An installation for given name and installation already exists");
                    return BadRequest($"Installation already exists");
                }

                var newInstallation = await _installationService.Create(installation);
                _logger.LogInformation(
                    "Succesfully created new installation with id '{installationId}'",
                    newInstallation.Id
                );
                return CreatedAtAction(
                    nameof(GetInstallationById),
                    new { id = newInstallation.Id },
                    newInstallation
                );
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error while creating new installation");
                throw;
            }
        }

        /// <summary>
        /// Deletes the installation with the specified id from the database.
        /// </summary>
        [HttpDelete]
        [Authorize(Roles = Role.Admin)]
        [Route("{id}")]
        [ProducesResponseType(typeof(Installation), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<Installation>> DeleteInstallation([FromRoute] string id)
        {
            var installation = await _installationService.Delete(id);
            if (installation is null)
                return NotFound($"Installation with id {id} not found");
            return Ok(installation);
        }
    }
}
