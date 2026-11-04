namespace Api.Services.Models
{
    public struct InstallationRole
    {
        public string InstallationCode { get; set; }
        public bool HasWriteAccess { get; set; }
    }

    public struct UserRoles
    {
        public List<InstallationRole> InstallationRoles { get; set; }
        public bool IsAdmin { get; set; }
    }
}
