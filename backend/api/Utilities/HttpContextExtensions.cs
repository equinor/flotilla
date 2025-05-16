using System.IdentityModel.Tokens.Jwt;

namespace Api.Utilities
{
    public static class HttpContextExtensions
    {
        public static string GetRequestToken(this HttpContext client)
        {
            if (!client.Request.Headers.TryGetValue("Authorization", out var value))
            {
                throw new HttpRequestException("Not a protected endpoint!");
            }

            return value.ToString().Replace("Bearer ", "", StringComparison.CurrentCulture);
        }

        public static List<System.Security.Claims.Claim> GetRequestedRoles(this HttpContext client)
        {
            var claims = client.GetRequestedClaims();
            var roles = claims
                .Where(c =>
                    c.Type == "roles" || c.Type.EndsWith("role", StringComparison.CurrentCulture)
                )
                .ToList();
            return roles;
        }

        public static List<string> GetRequestedRoleNames(this HttpContext client)
        {
            var roleClaims = GetRequestedRoles(client);
            return roleClaims.Select(c => c.Value).ToList();
        }

        public static List<System.Security.Claims.Claim> GetRequestedClaims(this HttpContext client)
        {
            string accessTokenBase64 = client.GetRequestToken();
            var handler = new JwtSecurityTokenHandler();
            var jwtSecurityToken = handler.ReadJwtToken(accessTokenBase64);
            return jwtSecurityToken.Claims.ToList();
        }

        public static string? GetUserObjectId(this HttpContext client)
        {
            var claims = client.GetRequestedClaims();
            var objectIdClaim = claims.FirstOrDefault(c => c.Type == "oid");
            if (objectIdClaim is null)
            {
                return null;
            }
            return objectIdClaim.Value;
        }
    }
}
