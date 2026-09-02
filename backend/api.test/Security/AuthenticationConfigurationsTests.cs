using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
    /// Guard rails for the generic OpenID Connect path: Entra ID remains the default
    /// with its issuer validator intact, and an unencrypted issuer is refused outside
    /// Local and IntegrationTest.
    /// </summary>
    public class AuthenticationConfigurationsTests
    {
        private const string KeycloakAuthority = "http://keycloak:8080/realms/robotics";

        private sealed class StubHostEnvironment(string environmentName) : IHostEnvironment
        {
            public string EnvironmentName { get; set; } = environmentName;
            public string ApplicationName { get; set; } = "Api.Test";
            public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
            public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        }

        private static ServiceProvider BuildProvider(
            string environmentName,
            string? provider = null,
            string? authority = null
        )
        {
            var settings = new Dictionary<string, string?>
            {
                ["AzureAd:Instance"] = "https://login.microsoftonline.com",
                ["AzureAd:TenantId"] = "00000000-0000-0000-0000-000000000000",
                ["AzureAd:ClientId"] = "flotilla-test",
                ["Redis:UseRedis"] = "false",
                ["Isar:Scopes:0"] = "isar-api",
                ["Pointilla:Scopes:0"] = "pointilla-api",
            };

            if (provider is not null)
            {
                settings[AuthenticationConfigurations.ProviderKey] = provider;
            }

            if (authority is not null)
            {
                settings["AzureAd:Authority"] = authority;
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

        private static ServiceProvider BuildOidcProvider(
            string environmentName,
            string authority = KeycloakAuthority
        ) => BuildProvider(environmentName, AuthenticationConfigurations.OidcProvider, authority);

        [Theory]
        [InlineData("Development")]
        [InlineData("Staging")]
        [InlineData("Production")]
        [InlineData("Local")]
        [InlineData("Test")]
        [InlineData(AuthenticationConfigurations.IntegrationTestEnvironment)]
        public void EntraIdIsTheDefaultProviderInEveryEnvironment(string environmentName)
        {
            using var provider = BuildProvider(environmentName);

            var headerProvider = provider.GetService<IAuthorizationHeaderProvider>();

            Assert.IsNotType<GenericOidcAuthorizationHeaderProvider>(headerProvider);
        }

        [Theory]
        [InlineData("Development")]
        [InlineData("Staging")]
        [InlineData("Production")]
        [InlineData("Local")]
        [InlineData("Test")]
        [InlineData(AuthenticationConfigurations.IntegrationTestEnvironment)]
        public void EntraIssuerValidatorSurvivesUnderTheDefaultProvider(string environmentName)
        {
            using var provider = BuildProvider(environmentName);

            var options = provider
                .GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
                .Get(JwtBearerDefaults.AuthenticationScheme);

            // Null here while the provider is EntraId means issuer validation has been
            // weakened for a real deployment.
            Assert.NotNull(options.TokenValidationParameters.IssuerValidator);
        }

        [Theory]
        [InlineData("Local")]
        [InlineData(AuthenticationConfigurations.IntegrationTestEnvironment)]
        public void OidcProviderRedirectsTokenValidation(string environmentName)
        {
            using var provider = BuildOidcProvider(environmentName);

            var options = provider
                .GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
                .Get(JwtBearerDefaults.AuthenticationScheme);

            Assert.Equal(KeycloakAuthority, options.Authority);
            Assert.Equal("flotilla-test", options.Audience);
            Assert.False(options.RequireHttpsMetadata);

            var parameters = options.TokenValidationParameters;
            Assert.True(parameters.ValidateIssuer);
            Assert.True(parameters.ValidateAudience);
            Assert.True(parameters.ValidateLifetime);
            // The Entra-specific validator must be gone, otherwise the issuer would be
            // rejected and instance discovery would hit login.microsoftonline.com.
            Assert.Null(parameters.IssuerValidator);
        }

        [Theory]
        [InlineData("Local")]
        [InlineData(AuthenticationConfigurations.IntegrationTestEnvironment)]
        public void OidcProviderRegistersTheGenericHeaderProvider(string environmentName)
        {
            using var provider = BuildOidcProvider(environmentName);

            var headerProvider = provider.GetRequiredService<IAuthorizationHeaderProvider>();

            Assert.IsType<GenericOidcAuthorizationHeaderProvider>(headerProvider);
        }

        [Fact]
        public async Task OnBehalfOfIsRefusedRatherThanDowngradedToAnApplicationToken()
        {
            using var provider = BuildOidcProvider(
                AuthenticationConfigurations.IntegrationTestEnvironment
            );
            var headerProvider = provider.GetRequiredService<IAuthorizationHeaderProvider>();

            // Silently returning an application token here would send a service
            // principal to a downstream API that asked for the signed-in user.
            var forUser = await Assert.ThrowsAsync<NotSupportedException>(() =>
                headerProvider.CreateAuthorizationHeaderForUserAsync(
                    ["some-api"],
                    cancellationToken: TestContext.Current.CancellationToken
                )
            );
            var generic = await Assert.ThrowsAsync<NotSupportedException>(() =>
                headerProvider.CreateAuthorizationHeaderAsync(
                    ["some-api"],
                    new AuthorizationHeaderProviderOptions { RequestAppToken = false },
                    cancellationToken: TestContext.Current.CancellationToken
                )
            );

            Assert.Equal(
                GenericOidcAuthorizationHeaderProvider.OnBehalfOfNotSupported,
                forUser.Message
            );
            Assert.Equal(
                GenericOidcAuthorizationHeaderProvider.OnBehalfOfNotSupported,
                generic.Message
            );
        }

        [Fact]
        public async Task ApplicationTokensStillFlowThroughTheUnifiedEntryPoint()
        {
            using var provider = BuildOidcProvider(
                AuthenticationConfigurations.IntegrationTestEnvironment
            );
            var headerProvider = provider.GetRequiredService<IAuthorizationHeaderProvider>();

            // IDownstreamApi routes CallApiForAppAsync through CreateAuthorizationHeaderAsync
            // with RequestAppToken set. Refusing that too breaks every downstream call, not
            // just the on-behalf-of ones. No issuer is reachable here, so the call fails at
            // the network; the point is that it is not refused outright.
            var exception = await Record.ExceptionAsync(() =>
                headerProvider.CreateAuthorizationHeaderAsync(
                    ["some-api"],
                    new AuthorizationHeaderProviderOptions { RequestAppToken = true },
                    cancellationToken: TestContext.Current.CancellationToken
                )
            );

            Assert.IsNotType<NotSupportedException>(exception);
        }

        [Theory]
        [InlineData("Development")]
        [InlineData("Staging")]
        [InlineData("Production")]
        [InlineData("Test")]
        public void UnencryptedIssuerIsRefusedInDeployedEnvironments(string environmentName)
        {
            var exception = Assert.Throws<InvalidOperationException>(() =>
                BuildOidcProvider(environmentName)
            );

            Assert.Contains("must use HTTPS", exception.Message);
        }

        [Theory]
        [InlineData("Development")]
        [InlineData("Staging")]
        [InlineData("Production")]
        public void EncryptedIssuerIsAcceptedInDeployedEnvironments(string environmentName)
        {
            using var provider = BuildOidcProvider(
                environmentName,
                "https://keycloak.example.com/realms/robotics"
            );

            var options = provider
                .GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
                .Get(JwtBearerDefaults.AuthenticationScheme);

            Assert.True(options.RequireHttpsMetadata);
            Assert.Null(options.TokenValidationParameters.IssuerValidator);
        }

        [Fact]
        public void OidcProviderWithoutAnAuthorityFailsLoudly()
        {
            var exception = Assert.Throws<InvalidOperationException>(() =>
                BuildProvider("Local", AuthenticationConfigurations.OidcProvider, authority: null)
            );

            Assert.Contains("AzureAd:Authority is required", exception.Message);
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

            using var oidcProvider = BuildOidcProvider("Local");
            var oidcOptions = oidcProvider
                .GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
                .Get(JwtBearerDefaults.AuthenticationScheme);

            Assert.NotNull(oidcOptions.Events?.OnMessageReceived);
        }
    }
}
