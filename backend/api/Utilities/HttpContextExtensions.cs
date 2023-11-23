using System.IdentityModel.Tokens.Jwt;

namespace Api.Utilities
{
    public readonly record struct ExecutorIdentity(string ObjectId, string? ExecutorName);

    public static class HttpContextExtensions
    {
        public static string GetRequestToken(this HttpContext client)
        {
            if (!client.Request.Headers.TryGetValue("Authorization", out var value))
            {
                throw new Exception("Not a protected endpoint!");
            }

            return value.ToString().Replace("Bearer ", "");
        }

        public static List<System.Security.Claims.Claim> GetRequestedRoles(this HttpContext client)
        {
            string accessTokenBase64 = client.GetRequestToken();

            var handler = new JwtSecurityTokenHandler();
            var jwtSecurityToken = handler.ReadJwtToken(accessTokenBase64);

            var claims = jwtSecurityToken.Claims;
            var roles = claims.Where((c) => c.Type == "roles").ToList();
            return roles;
        }

        public static List<string> GetRequestedRoleNames(this HttpContext client)
        {
            var roleClaims = GetRequestedRoles(client);
            return roleClaims.Select((c) => c.Value).ToList();
        }
    }
}
