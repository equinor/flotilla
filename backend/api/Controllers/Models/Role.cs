namespace Api.Controllers.Models
{
    /// <summary>
    /// Authorization policy names. Installation-level access is enforced by the services.
    /// </summary>
    public static class Role
    {
        /// <summary>
        /// The user must be an admin
        /// </summary>
        public const string Admin = "Admin";

        /// <summary>
        /// The user must be an admin or have a <c>Role.User.CODE</c> role
        /// </summary>
        public const string User = "User";

        /// <summary>
        /// The user must be an admin or have a <c>Role.User.CODE</c> or <c>Role.ReadOnly.CODE</c> role
        /// </summary>
        public const string Any = "Any";
    }
}
