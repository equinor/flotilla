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

        private static HttpContext GetHttpContextWithRoles(List<string> roles)
        {
            var context = new DefaultHttpContext();

            var claims = roles.Select(r => new Claim(ClaimTypes.Role, r)).ToList();
            var identity = new ClaimsIdentity(claims, "mock");
            var principal = new ClaimsPrincipal(identity);

            context.User = principal;

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
