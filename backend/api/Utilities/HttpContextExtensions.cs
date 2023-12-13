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

        public static string? GetUserNameId(this HttpContext client)
        {
            string accessTokenBase64 = client.GetRequestToken();

            var handler = new JwtSecurityTokenHandler();
            var jwtSecurityToken = handler.ReadJwtToken(accessTokenBase64);

            var claims = jwtSecurityToken.Claims;
            string? objectId = claims.Where((c) => c.Type == "oid" || c.Type.EndsWith("oid", StringComparison.CurrentCulture)).Select((n) => n.Value).FirstOrDefault();
            string? nameId = claims.Where((c) => c.Type == "name" || c.Type.EndsWith("name", StringComparison.CurrentCulture)).Select((n) => n.Value).FirstOrDefault();
            return nameId;
        }

        public static List<System.Security.Claims.Claim> GetRequestedRoles(this HttpContext client)
        {
            string accessTokenBase64 = client.GetRequestToken();

            var handler = new JwtSecurityTokenHandler();
            var jwtSecurityToken = handler.ReadJwtToken(accessTokenBase64);

            var claims = jwtSecurityToken.Claims;
            var roles = claims.Where((c) => c.Type == "roles" || c.Type.EndsWith("role", StringComparison.CurrentCulture)).ToList();
            return roles;
        }

        public static List<string> GetRequestedRoleNames(this HttpContext client)
        {
            var roleClaims = GetRequestedRoles(client);
            return roleClaims.Select((c) => c.Value).ToList();
        }
    }
}
