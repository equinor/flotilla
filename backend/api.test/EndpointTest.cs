using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Api.Database.Models;
using Api.Services;
using Api.Test.Mocks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Api.Test
{
    [Collection("Database collection")]
    public class EndpointTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
    {
        private readonly HttpClient _client;

        public EndpointTests(WebApplicationFactory<Program> factory)
        {
            string projectDir = Directory.GetCurrentDirectory();
            string configPath = Path.Combine(projectDir, "appsettings.Test.json");
            var client = factory.WithWebHostBuilder(builder =>
                {
                    var configuration = new ConfigurationBuilder().AddJsonFile(configPath).Build();
                    builder.UseEnvironment("Test");
                    builder.ConfigureAppConfiguration((context, config) =>
                    {
                        config.AddJsonFile(configPath).AddEnvironmentVariables();
                    });
                    builder.ConfigureTestServices(services =>
                    {
                        services.AddScoped<IIsarService, MockIsarService>();
                        services.AddScoped<IEchoService, MockEchoService>();
                        services.AddAuthorization(options =>
                        {
                            options.FallbackPolicy = new AuthorizationPolicyBuilder(TestAuthHandler.AuthenticationScheme)
                                .RequireAuthenticatedUser()
                                .RequireRole(configuration.GetSection("Authorization")["Roles"])
                                .Build();
                        });
                        services.AddAuthentication(options =>
                        {
                            options.DefaultAuthenticateScheme = TestAuthHandler.AuthenticationScheme;
                            options.DefaultChallengeScheme = TestAuthHandler.AuthenticationScheme;
                        }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                            TestAuthHandler.AuthenticationScheme, options => { });
                    });
                })
                .CreateClient(new WebApplicationFactoryClientOptions
                {
                    AllowAutoRedirect = false,
                });
            client.BaseAddress = new Uri("https://localhost:8000");
            client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue(TestAuthHandler.AuthenticationScheme);
            _client = client;
        }

        public void Dispose()
        {
            _client.Dispose();
            GC.SuppressFinalize(this);
        }

        [Fact]
        public async Task MissionsTest()
        {
            string url = "/missions";
            var response = await _client.GetAsync(url);
            var missions = await response.Content.ReadFromJsonAsync<List<Mission>>(
                new JsonSerializerOptions
                {
                    Converters = { new JsonStringEnumConverter() },
                });
            Assert.True(response.IsSuccessStatusCode);
            Assert.True(missions != null && missions.Count == 3);
        }

        [Fact]
        public async Task RobotsTest()
        {
            string url = "/robots";
            var response = await _client.GetAsync(url);
            var robots = await response.Content.ReadFromJsonAsync<List<Robot>>(
                new JsonSerializerOptions
                {
                    Converters = { new JsonStringEnumConverter() },
                });
            Assert.True(response.IsSuccessStatusCode);
            Assert.True(robots != null && robots.Count == 3);
        }
    }
}
