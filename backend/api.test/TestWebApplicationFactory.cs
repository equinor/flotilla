using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Api.Database.Context;
using Api.EventHandlers;
using Api.Mqtt;
using Api.Services;
using Api.Test.Mocks;
using Api.Test.Utilities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
namespace Api.Test
{
    public class TestWebApplicationFactory<TProgram>(string? databaseConnectionString) : WebApplicationFactory<Program> where TProgram : class
    {
        public IConfiguration? Configuration;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            string projectDir = Directory.GetCurrentDirectory();
            string configPath = Path.Combine(projectDir, "appsettings.Test.json");
            Configuration = new ConfigurationBuilder()
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
                    var descriptorDbContext =
                        services.FirstOrDefault(descriptor => descriptor.ServiceType == typeof(FlotillaDbContext));
                    if (descriptorDbContext != null) { services.Remove(descriptorDbContext); }

                    services.AddDbContext<FlotillaDbContext>(
                        options =>
                            options.UseNpgsql(
                                databaseConnectionString,
                                o =>
                                {
                                    o.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
                                    o.EnableRetryOnFailure();
                                }
                            ),
                        ServiceLifetime.Transient
                    );

                    services.AddScoped<IAccessRoleService, AccessRoleService>();
                    services.AddScoped<IIsarService, MockIsarService>();
                    services.AddSingleton<IHttpContextAccessor, MockHttpContextAccessor>();
                    services.AddScoped<IEchoService, MockEchoService>();
                    services.AddScoped<IMapService, MockMapService>();
                    services.AddScoped<IBlobService, MockBlobService>();
                    services.AddScoped<IStidService, MockStidService>();
                    services.AddScoped<ICustomMissionService, MockCustomMissionService>();

                    services.AddAuthorizationBuilder().AddFallbackPolicy(
                        TestAuthHandler.AuthenticationScheme, policy => policy.RequireAuthenticatedUser()
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

        public override async ValueTask DisposeAsync()
        {
            Console.WriteLine("Test");
            //var token = new CancellationToken(true);
            //await this.Services.GetRequiredService<MissionEventHandler>().StopAsync(token);
            //await this.Services.GetRequiredService<MqttEventHandler>().StopAsync(token);
            //await this.Services.GetRequiredService<InspectionFindingEventHandler>().StopAsync(token);
            //await this.Services.GetRequiredService<MqttService>().StopAsync(token);
            //await this.Services.GetRequiredService<IsarConnectionEventHandler>().StopAsync(token);

            await base.DisposeAsync();
        }
    }
}
