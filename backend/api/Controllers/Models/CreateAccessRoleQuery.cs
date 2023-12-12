using Api.Database.Models;

namespace Api.Controllers.Models
{
    public struct CreateAccessRoleQuery
    {
        public string InstallationCode { get; set; }
        public string RoleName { get; set; }
        public RoleAccessLevel AccessLevel { get; set; }
    }
}
