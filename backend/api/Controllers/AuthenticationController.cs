using Api.Controllers.Models;
using Api.Services;
using Api.Services.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthenticationController(IAccessRoleService accessRoleService) : ControllerBase
    {
        /// <summary>
        /// List all accesses for the requesting user
        /// </summary>
        /// <remarks>
        /// <para> This query gets all the accesses the user has </para>
        /// </remarks>
        [HttpGet]
        [Route("me")]
        [Authorize(Policy = Role.Any)]
        [ProducesResponseType(typeof(UserRoles), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<UserRoles>> GetAccessRoles()
        {
            try
            {
                return Ok(await accessRoleService.GetUserRoles());
            }
            catch (HttpRequestException e)
            {
                return StatusCode(StatusCodes.Status403Forbidden, e.Message);
            }
        }
    }
}
