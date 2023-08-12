using Api.Controllers.Models;
using Api.Database.Models;
using Api.Services;
using Azure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController]
    [Route("areas")]
    public class AreaController : ControllerBase
    {
        private readonly IAreaService _areaService;

        private readonly IMissionDefinitionService _missionDefinitionService;

        private readonly IMapService _mapService;

        private readonly ILogger<AreaController> _logger;

        public AreaController(
            ILogger<AreaController> logger,
            IMapService mapService,
            IAreaService areaService,
            IMissionDefinitionService missionDefinitionService
        )
        {
            _logger = logger;
            _mapService = mapService;
            _areaService = areaService;
            _missionDefinitionService = missionDefinitionService;
        }

        /// <summary>
        /// Add a new area
        /// </summary>
        /// <remarks>
        /// <para> This query adds a new area to the database </para>
        /// </remarks>
        [HttpPost]
        [Authorize(Roles = Role.Admin)]
        [ProducesResponseType(typeof(AreaResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<AreaResponse>> Create([FromBody] CreateAreaQuery area)
        {
            _logger.LogInformation("Creating new area");
            try
            {
                var existingArea = await _areaService.ReadByInstallationAndName(area.InstallationCode, area.AreaName);
                if (existingArea != null)
                {
                    _logger.LogWarning("An area for given name and installation already exists");
                    return Conflict($"Area already exists");
                }

                var newArea = await _areaService.Create(area);
                _logger.LogInformation(
                    "Succesfully created new area with id '{areaId}'",
                    newArea.Id
                );

                var response = new AreaResponse
                {
                    Id = newArea.Id,
                    DeckName = newArea.Deck?.Name ?? string.Empty,
                    PlantCode = newArea.Plant?.PlantCode ?? string.Empty,
                    InstallationCode = newArea.Installation?.InstallationCode ?? string.Empty,
                    AreaName = newArea.Name,
                    MapMetadata = newArea.MapMetadata,
                    DefaultLocalizationPose = newArea.DefaultLocalizationPose,
                    SafePositions = newArea.SafePositions
                };
                return CreatedAtAction(
                    nameof(GetAreaById),
                    new { id = newArea.Id },
                    response
                );
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error while creating new area");
                throw;
            }
        }

        /// <summary>
        /// Add safe position to an area
        /// </summary>
        /// <remarks>
        /// <para> This query adds a new safe position to the database </para>
        /// </remarks>
        [HttpPost]
        [Authorize(Roles = Role.Admin)]
        [Route("{installationCode}/{areaName}/safe-position")]
        [ProducesResponseType(typeof(AreaResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<AreaResponse>> AddSafePosition(
            [FromRoute] string installationCode,
            [FromRoute] string areaName,
            [FromBody] Pose safePosition
        )
        {
            _logger.LogInformation(@"Adding new safe position to {Installation}, {Area}", installationCode, areaName);
            try
            {
                var area = await _areaService.AddSafePosition(installationCode, areaName, new SafePosition(safePosition));
                if (area != null)
                {
                    _logger.LogInformation(@"Successfully added new safe position for installation '{installationId}'
                        and name '{name}'", installationCode, areaName);
                    if (area.Deck == null || area.Plant == null || area.Installation == null)
                    {
                        string errorMessage = "Deck, plant or installation missing from area";
                        _logger.LogWarning(errorMessage);
                        return StatusCode(StatusCodes.Status500InternalServerError, errorMessage);
                    }
                    var response = new AreaResponse
                    {
                        Id = area.Id,
                        DeckName = area.Deck.Name,
                        PlantCode = area.Plant.PlantCode,
                        InstallationCode = area.Installation.InstallationCode,
                        AreaName = area.Name,
                        MapMetadata = area.MapMetadata,
                        DefaultLocalizationPose = area.DefaultLocalizationPose,
                        SafePositions = area.SafePositions
                    };
                    return CreatedAtAction(nameof(GetAreaById), new { id = area.Id }, response); ;
                }
                else
                {
                    _logger.LogInformation(@"No area with installation {installationCode} and name {areaName} could be found.", installationCode, areaName);
                    return NotFound(@$"No area with installation {installationCode} and name {areaName} could be found.");
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error while creating or adding new safe zone");
                throw;
            }
        }

        /// <summary>
        /// Deletes the area with the specified id from the database.
        /// </summary>
        [HttpDelete]
        [Authorize(Roles = Role.Admin)]
        [Route("{id}")]
        [ProducesResponseType(typeof(AreaResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<AreaResponse>> DeleteArea([FromRoute] string id)
        {
            var area = await _areaService.Delete(id);
            if (area is null)
                return NotFound($"Area with id {id} not found");
            if (area.Deck == null || area.Plant == null || area.Installation == null)
            {
                string errorMessage = "Deck, plant or installation missing from area";
                _logger.LogWarning(errorMessage);
                return StatusCode(StatusCodes.Status500InternalServerError, errorMessage);
            }
            var response = new AreaResponse
            {
                Id = area.Id,
                DeckName = area.Deck.Name,
                PlantCode = area.Plant.PlantCode,
                InstallationCode = area.Installation.InstallationCode,
                AreaName = area.Name,
                MapMetadata = area.MapMetadata,
                DefaultLocalizationPose = area.DefaultLocalizationPose,
                SafePositions = area.SafePositions
            };
            return Ok(response);
        }

        /// <summary>
        /// List all installation areas in the Flotilla database
        /// </summary>
        /// <remarks>
        /// <para> This query gets all installation areas </para>
        /// </remarks>
        [HttpGet]
        [Authorize(Roles = Role.Any)]
        [ProducesResponseType(typeof(IList<AreaResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IList<AreaResponse>>> GetAreas()
        {
            try
            {
                var areas = await _areaService.ReadAll();
                var response = areas.Select(area => new AreaResponse
                {
                    Id = area.Id,
                    DeckName = area.Deck?.Name ?? string.Empty,
                    PlantCode = area.Plant?.PlantCode ?? string.Empty,
                    InstallationCode = area.Installation?.InstallationCode ?? string.Empty,
                    AreaName = area.Name,
                    MapMetadata = area.MapMetadata,
                    DefaultLocalizationPose = area.DefaultLocalizationPose,
                    SafePositions = area.SafePositions
                });
                return Ok(response);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error during GET of areas from database");
                throw;
            }
        }

        /// <summary>
        /// Lookup area by specified id.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = Role.Any)]
        [Route("{id}")]
        [ProducesResponseType(typeof(AreaResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<AreaResponse>> GetAreaById([FromRoute] string id)
        {
            try
            {
                var area = await _areaService.ReadById(id);
                if (area == null)
                    return NotFound($"Could not find area with id {id}");
                if (area.Deck == null || area.Plant == null || area.Installation == null)
                {
                    string errorMessage = "Deck, plant or installation missing from area";
                    _logger.LogWarning(errorMessage);
                    return StatusCode(StatusCodes.Status500InternalServerError, errorMessage);
                }
                var response = new AreaResponse
                {
                    Id = area.Id,
                    DeckName = area.Deck.Name,
                    PlantCode = area.Plant.PlantCode,
                    InstallationCode = area.Installation.InstallationCode,
                    AreaName = area.Name,
                    MapMetadata = area.MapMetadata,
                    DefaultLocalizationPose = area.DefaultLocalizationPose,
                    SafePositions = area.SafePositions
                };
                return Ok(response);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error during GET of areas from database");
                throw;
            }
        }

        /// <summary>
        /// Lookup all the mission definitions related to an area
        /// </summary>
        [HttpGet]
        [Authorize(Roles = Role.Any)]
        [Route("{id}/mission-definitions")]
        [ProducesResponseType(typeof(AreaResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IList<MissionDefinition>>> GetMissionDefinitionsInArea([FromRoute] string id)
        {
            try
            {
                var area = await _areaService.ReadById(id);
                if (area == null)
                    return NotFound($"Could not find area with id {id}");

                var missionDefinitions = await _missionDefinitionService.ReadByAreaId(area.Id);
                return Ok(missionDefinitions);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error during GET of area missions from database");
                throw;
            }
        }

        /// <summary>
        /// Gets map metadata for localization poses belonging to area with specified id
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
            var area = await _areaService.ReadById(id);
            if (area is null)
            {
                string errorMessage = $"Area not found for area with ID {id}";
                _logger.LogError("{ErrorMessage}", errorMessage);
                return NotFound(errorMessage);
            }
            if (area.Installation == null)
            {
                string errorMessage = "Installation missing from area";
                _logger.LogWarning(errorMessage);
                return StatusCode(StatusCodes.Status500InternalServerError, errorMessage);
            }

            MapMetadata? mapMetadata;
            var positions = new List<Position>
            {
                area.DefaultLocalizationPose.Position
            };
            try
            {
                mapMetadata = await _mapService.ChooseMapFromPositions(positions, area.Installation.InstallationCode);
            }
            catch (RequestFailedException e)
            {
                string errorMessage = $"An error occurred while retrieving the map for area {area.Id}";
                _logger.LogError(e, "{ErrorMessage}", errorMessage);
                return StatusCode(StatusCodes.Status502BadGateway, errorMessage);
            }
            catch (ArgumentOutOfRangeException e)
            {
                string errorMessage = $"Could not find a suitable map for area {area.Id}";
                _logger.LogError(e, "{ErrorMessage}", errorMessage);
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
