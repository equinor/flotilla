using System.IO;
using Api.Services;
using Api.Test.Mocks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
namespace Api.Test
{
    public class TestWebApplicationFactory<TProgram> : WebApplicationFactory<Program> where TProgram : class
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            string projectDir = Directory.GetCurrentDirectory();
            string configPath = Path.Combine(projectDir, "appsettings.Test.json");
            var configuration = new ConfigurationBuilder()
                            .AddJsonFile(configPath)
                            .Build();
            builder.UseEnvironment("Test");
            builder.ConfigureAppConfiguration(
                (context, config) =>
                {
                    config.AddJsonFile(configPath).AddEnvironmentVariables();
                }
            );
            builder.ConfigureTestServices(
                services =>
                {
                    services.AddScoped<IIsarService, MockIsarService>();
                    services.AddScoped<IEchoService, MockEchoService>();
                    services.AddScoped<IMapService, MockMapService>();
                    services.AddScoped<IBlobService, MockBlobService>();
                    services.AddScoped<IStidService, MockStidService>();
                    services.AddScoped<ICustomMissionService, MockCustomMissionService>();
                    services.AddAuthorization(
                        options =>
                        {
                            options.FallbackPolicy = new AuthorizationPolicyBuilder(
                                    TestAuthHandler.AuthenticationScheme
                                )
                                .RequireAuthenticatedUser()
                                .Build();
                        }
                    );
                    services
                        .AddAuthentication(
                            options =>
                            {
                                options.DefaultAuthenticateScheme =
                                    TestAuthHandler.AuthenticationScheme;
                                options.DefaultChallengeScheme =
                                    TestAuthHandler.AuthenticationScheme;
                            }
                        )
                        .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                            TestAuthHandler.AuthenticationScheme,
                            options => { }
                        );
                }
            );
        }
    }
}
