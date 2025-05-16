using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;

namespace Api.Test.Mocks
{
    public class MockHttpContextAccessor : IHttpContextAccessor
    {
        private HttpContext? CustomRolesHttpContext { get; set; }

#pragma warning disable CA1859
        private static HttpContext GetHttpContextWithRoles(List<string> roles)
#pragma warning restore CA1859
        {
            var context = new DefaultHttpContext();
            var tokenHandler = new JwtSecurityTokenHandler();
            var claims = roles.Select<string, Claim>(r => new(ClaimTypes.Role, r)).ToList();

            var rng = RandomNumberGenerator.Create();
            byte[] key = new byte[32];
            rng.GetBytes(key);
            var securityKey = new SymmetricSecurityKey(key) { KeyId = Guid.NewGuid().ToString() };
            var signingCredentials = new SigningCredentials(
                securityKey,
                SecurityAlgorithms.HmacSha256
            );

            string issuer = Guid.NewGuid().ToString();
            string jwtToken = tokenHandler.WriteToken(
                new JwtSecurityToken(
                    issuer,
                    null,
                    claims,
                    null,
                    DateTime.UtcNow.AddMinutes(20),
                    signingCredentials
                )
            );
            context.Request.Headers.Authorization = jwtToken;
            return context;
        }

        public void SetHttpContextRoles(List<string> roles)
        {
            CustomRolesHttpContext = GetHttpContextWithRoles(roles);
        }

        public HttpContext? HttpContext
        {
            get
            {
                if (CustomRolesHttpContext is not null)
                    return CustomRolesHttpContext;

                return GetHttpContextWithRoles(["Role.Admin"]);
            }
            set { }
        }
    }
}
