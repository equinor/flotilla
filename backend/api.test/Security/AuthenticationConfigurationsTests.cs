using System;
using System.Collections.Generic;
using Api.Configurations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
using Xunit;

namespace Api.Test.Security
{
    /// <summary>
    /// Guard rails for the integration-test authentication path.
    ///
    /// The generic OIDC issuer removes Entra ID from the picture entirely, so these
    /// tests exist to make sure it stays confined to the IntegrationTest environment
    /// and cannot be reached from Development, Staging or Production.
    /// </summary>
    public class AuthenticationConfigurationsTests
    {
        private sealed class StubHostEnvironment(string environmentName) : IHostEnvironment
        {
            public string EnvironmentName { get; set; } = environmentName;
            public string ApplicationName { get; set; } = "Api.Test";
            public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
            public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        }

        private static ServiceProvider BuildProvider(string environmentName)
        {
            var settings = new Dictionary<string, string?>
            {
                ["AzureAd:Instance"] = "https://login.microsoftonline.com",
                ["AzureAd:TenantId"] = "00000000-0000-0000-0000-000000000000",
                ["AzureAd:ClientId"] = "flotilla-test",
                ["Redis:UseRedis"] = "false",
                ["Isar:Scopes:0"] = "isar-test/.default",
                ["SARA:Scopes:0"] = "sara-test/.default",
                ["Pointilla:Scopes:0"] = "pointilla-test/.default",
            };

            // Mirrors the appsettings layout: AzureAd:Authority is set only in
            // appsettings.IntegrationTest.json.
            if (environmentName == AuthenticationConfigurations.IntegrationTestEnvironment)
            {
                settings["AzureAd:Authority"] = "http://oauth-mock:8080";
            }

            var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<IConfiguration>(configuration);
            services.ConfigureAuthentication(
                configuration,
                new StubHostEnvironment(environmentName)
            );

            return services.BuildServiceProvider();
        }

        [Theory]
        [InlineData("Development")]
        [InlineData("Staging")]
        [InlineData("Production")]
        [InlineData("Local")]
        [InlineData("Test")]
        public void GenericOidcProviderIsNotRegisteredOutsideIntegrationTest(string environmentName)
        {
            using var provider = BuildProvider(environmentName);

            var headerProvider = provider.GetService<IAuthorizationHeaderProvider>();

            Assert.IsNotType<GenericOidcAuthorizationHeaderProvider>(headerProvider);
        }

        [Theory]
        [InlineData("Development")]
        [InlineData("Staging")]
        [InlineData("Production")]
        public void EntraIssuerValidatorIsWiredOutsideIntegrationTest(string environmentName)
        {
            using var provider = BuildProvider(environmentName);

            var options = provider
                .GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
                .Get(JwtBearerDefaults.AuthenticationScheme);

            // AddMicrosoftIdentityWebApi installs the Entra-aware issuer validator.
            // If this is ever null outside IntegrationTest, issuer validation has been
            // weakened for a real deployment.
            Assert.NotNull(options.TokenValidationParameters.IssuerValidator);
        }

        [Fact]
        public void GenericOidcProviderIsRegisteredInIntegrationTest()
        {
            using var provider = BuildProvider(
                AuthenticationConfigurations.IntegrationTestEnvironment
            );

            var headerProvider = provider.GetRequiredService<IAuthorizationHeaderProvider>();

            Assert.IsType<GenericOidcAuthorizationHeaderProvider>(headerProvider);
        }

        [Fact]
        public void IntegrationTestEnvironmentPointsTokenValidationAtTheMockIssuer()
        {
            using var provider = BuildProvider(
                AuthenticationConfigurations.IntegrationTestEnvironment
            );

            var options = provider
                .GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
                .Get(JwtBearerDefaults.AuthenticationScheme);

            Assert.Equal("http://oauth-mock:8080", options.Authority);
            Assert.Equal("flotilla-test", options.Audience);
            Assert.False(options.RequireHttpsMetadata);

            var parameters = options.TokenValidationParameters;
            Assert.True(parameters.ValidateIssuer);
            Assert.True(parameters.ValidateAudience);
            Assert.True(parameters.ValidateLifetime);
            // The Entra-specific validator must be gone, otherwise the mock's issuer
            // would be rejected and instance discovery would hit login.microsoftonline.com.
            Assert.Null(parameters.IssuerValidator);
        }

        [Fact]
        public void SignalRQueryStringTokenHookIsAppliedInEveryEnvironment()
        {
            foreach (
                var environmentName in new[]
                {
                    "Development",
                    "Production",
                    AuthenticationConfigurations.IntegrationTestEnvironment,
                }
            )
            {
                using var provider = BuildProvider(environmentName);

                var options = provider
                    .GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
                    .Get(JwtBearerDefaults.AuthenticationScheme);

                Assert.NotNull(options.Events?.OnMessageReceived);
            }
        }
    }
}
