using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;

namespace Api.Test.Mocks
{
    public class MockHttpContextAccessor : IHttpContextAccessor
    {
        public HttpContext? HttpContext
        {
            get
            {
                var context = new DefaultHttpContext();
                var tokenHandler = new JwtSecurityTokenHandler();
                var claims = new List<Claim>
                {
                    new(ClaimTypes.Role, "Role.Admin"),
                    new(ClaimTypes.Role, "Role.User"),
                    new(ClaimTypes.Role, "Role.User.JSV")
                };

                var rng = RandomNumberGenerator.Create();
                byte[] key = new byte[32];
                rng.GetBytes(key);
                var securityKey = new SymmetricSecurityKey(key) { KeyId = Guid.NewGuid().ToString() };
                var signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

                string issuer = Guid.NewGuid().ToString();
                string jwtToken = tokenHandler.WriteToken(new JwtSecurityToken(issuer, null, claims, null, DateTime.UtcNow.AddMinutes(20), signingCredentials));
                context.Request.Headers.Authorization = jwtToken;
                return context;
            }
            set { }
        }
    }
}
