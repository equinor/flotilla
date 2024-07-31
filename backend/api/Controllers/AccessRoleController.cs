using Api.Controllers.Models;
using Api.Database.Models;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController]
    [Route("access-roles")]
    public class AccessRoleController(
            ILogger<AccessRoleController> logger,
            IAccessRoleService accessRoleService,
            IInstallationService installationService
        ) : ControllerBase
    {
        /// <summary>
        /// List all access roles in the Flotilla database
        /// </summary>
        /// <remarks>
        /// <para> This query gets all access roles </para>
        /// </remarks>
        [HttpGet]
        [Authorize(Roles = Role.Admin)]
        [ProducesResponseType(typeof(IList<AccessRole>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IList<Installation>>> GetAccessRoles()
        {
            try
            {
                var accessRoles = await accessRoleService.ReadAll();
                return Ok(accessRoles);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error during GET of access roles from database");
                throw;
            }
        }

        /// <summary>
        /// Add a new access role
        /// </summary>
        /// <remarks>
        /// <para> This query adds a new access role to the database </para>
        /// </remarks>
        [HttpPost]
        [Authorize(Roles = Role.Admin)]
        [ProducesResponseType(typeof(AccessRole), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<AccessRole>> Create([FromBody] CreateAccessRoleQuery accessRoleQuery)
        {
            logger.LogInformation("Creating new access role");
            try
            {
                if (accessRoleQuery.AccessLevel == RoleAccessLevel.ADMIN)
                    return Unauthorized("Cannot create admin roles using API endpoints");

                var installation = await installationService.ReadByName(accessRoleQuery.InstallationCode, readOnly: true);
                if (installation is null)
                {
                    logger.LogInformation("Installation not found when creating new access roles");
                    return NotFound($"Installation not found");
                }

                var existingAccessRole = await accessRoleService.ReadByInstallation(installation!);
                if (existingAccessRole != null && existingAccessRole.RoleName == accessRoleQuery.RoleName)
                {
                    logger.LogInformation("An access role for the given installation and role name already exists");
                    return BadRequest($"Access role already exists");
                }

                var newAccessRole = await accessRoleService.Create(installation, accessRoleQuery.RoleName, accessRoleQuery.AccessLevel);
                logger.LogInformation(
                    "Succesfully created new access role for installation '{installationCode}'",
                    installation.InstallationCode
                );
                return newAccessRole;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error while creating new access role");
                throw;
            }
        }
    }
}
