﻿using System.IO;
using Api.Services;
using Api.Services.MissionLoaders;
using Api.Test.Mocks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Test
{
    public class TestWebApplicationFactory<TProgram> : WebApplicationFactory<Program>
        where TProgram : class
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            string projectDir = Directory.GetCurrentDirectory();
            string configPath = Path.Combine(projectDir, "appsettings.Test.json");
            var configuration = new ConfigurationBuilder().AddJsonFile(configPath).Build();
            builder.UseEnvironment("Test");
            builder.ConfigureAppConfiguration(
                (context, config) =>
                {
                    config.AddJsonFile(configPath).AddEnvironmentVariables();
                }
            );
            builder.ConfigureTestServices(services =>
            {
                services.AddScoped<IAccessRoleService, AccessRoleService>();
                services.AddScoped<IIsarService, MockIsarService>();
                services.AddSingleton<IHttpContextAccessor, MockHttpContextAccessor>();
                services.AddScoped<IMapService, MockMapService>();
                services.AddScoped<IBlobService, MockBlobService>();
                services.AddScoped<IStidService, MockStidService>();
                services.AddScoped<IMissionLoader, MockMissionLoader>();
                services
                    .AddAuthorizationBuilder()
                    .AddFallbackPolicy(
                        TestAuthHandler.AuthenticationScheme,
                        policy => policy.RequireAuthenticatedUser()
                    );
                services
                    .AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = TestAuthHandler.AuthenticationScheme;
                        options.DefaultChallengeScheme = TestAuthHandler.AuthenticationScheme;
                    })
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                        TestAuthHandler.AuthenticationScheme,
                        options => { }
                    );
            });
        }
    }
}
