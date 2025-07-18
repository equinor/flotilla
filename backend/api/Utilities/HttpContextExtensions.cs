using System.Security.Claims;

namespace Api.Utilities
{
    public static class HttpContextExtensions
    {
        public static List<System.Security.Claims.Claim> GetRequestedRoles(this HttpContext context)
        {
            return context.User?.Claims.Where(c => c.Type == ClaimTypes.Role).ToList()
                ?? new List<Claim>();
        }

        public static List<Claim> GetRequestedClaims(this HttpContext context)
        {
            return context.User?.Claims.ToList() ?? new List<Claim>();
        }

        public static string? GetUserObjectId(this HttpContext context)
        {
            return context.User?.FindFirst("oid")?.Value;
        }
    }
}
