using Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;

namespace Api.Configurations
{
    public static class AuthenticationConfigurations
    {
        /// <summary>
        /// Selects the identity provider: <c>EntraId</c> (default) or <c>Oidc</c>, any
        /// conformant OpenID Connect issuer. Validation is unchanged either way.
        /// </summary>
        public const string ProviderKey = "Authentication:Provider";
        public const string OidcProvider = "Oidc";

        /// <summary>
        /// The environment used by the armada integration tests. It has no appsettings
        /// file: armada supplies the configuration as environment variables.
        /// </summary>
        public const string IntegrationTestEnvironment = "IntegrationTest";

        public const string LocalEnvironment = "Local";

        public static bool UsesGenericOidc(this IConfiguration configuration) =>
            string.Equals(
                configuration[ProviderKey],
                OidcProvider,
                StringComparison.OrdinalIgnoreCase
            );

        /// <summary>Whether a plain-HTTP authority is tolerated.</summary>
        public static bool AllowsInsecureMetadata(this IHostEnvironment environment) =>
            environment.IsEnvironment(LocalEnvironment)
            || environment.IsEnvironment(IntegrationTestEnvironment);

        /// <summary>
        /// Registers JWT bearer authentication, the token caches and the downstream
        /// API clients.
        /// </summary>
        public static IServiceCollection ConfigureAuthentication(
            this IServiceCollection services,
            IConfiguration configuration,
            IHostEnvironment environment
        )
        {
            bool useRedis = configuration.GetSection("Redis").GetValue<bool>("UseRedis");
            if (useRedis)
            {
                services.ConfigureRedisCache(configuration);
            }

            var authenticationBuilder = services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApi(configuration.GetSection("AzureAd"))
                .EnableTokenAcquisitionToCallDownstreamApi();

            if (useRedis)
            {
                authenticationBuilder.AddDistributedTokenCaches();
            }
            else
            {
                authenticationBuilder.AddInMemoryTokenCaches();
            }

            authenticationBuilder
                .AddDownstreamApi(InspectionService.ServiceName, configuration.GetSection("SARA"))
                .AddDownstreamApi(IsarService.ServiceName, configuration.GetSection("Isar"))
                .AddDownstreamApi(
                    PointillaService.ServiceName,
                    configuration.GetSection("Pointilla")
                );

            if (configuration.UsesGenericOidc())
            {
                ConfigureGenericOidcOverrides(services, configuration, environment);
            }

            ConfigureSignalRQueryStringToken(services);

            return services;
        }

        /// <summary>
        /// Outbound, replacing IAuthorizationHeaderProvider covers all three downstream
        /// APIs, since IDownstreamApi resolves every bearer token through it.
        ///
        /// Inbound, the split across both options phases is required:
        /// JwtBearerPostConfigureOptions rejects a plain HTTP authority and runs before
        /// any post-configuration we can register, while Microsoft.Identity.Web installs
        /// its AadIssuerValidator during post-configuration, so only a later
        /// post-configure can remove it.
        /// </summary>
        private static void ConfigureGenericOidcOverrides(
            IServiceCollection services,
            IConfiguration configuration,
            IHostEnvironment environment
        )
        {
            string authority =
                configuration["AzureAd:Authority"]
                ?? throw new InvalidOperationException(
                    $"AzureAd:Authority is required when {ProviderKey} is {OidcProvider}"
                );
            string audience =
                configuration["AzureAd:ClientId"]
                ?? throw new InvalidOperationException(
                    $"AzureAd:ClientId is required when {ProviderKey} is {OidcProvider}"
                );

            bool allowInsecureMetadata = environment.AllowsInsecureMetadata();
            if (
                !allowInsecureMetadata
                && !authority.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            )
            {
                throw new InvalidOperationException(
                    $"AzureAd:Authority must use HTTPS in the {environment.EnvironmentName} "
                        + $"environment, but was '{authority}'. Plain HTTP is only accepted in "
                        + $"the {LocalEnvironment} and {IntegrationTestEnvironment} environments."
                );
            }

            services.Configure<JwtBearerOptions>(
                JwtBearerDefaults.AuthenticationScheme,
                options =>
                {
                    options.Authority = authority;
                    options.Audience = audience;
                    options.RequireHttpsMetadata = !allowInsecureMetadata;
                }
            );

            services.PostConfigure<JwtBearerOptions>(
                JwtBearerDefaults.AuthenticationScheme,
                options =>
                {
                    var parameters = options.TokenValidationParameters;
                    parameters.ValidateIssuer = true;
                    parameters.ValidateAudience = true;
                    parameters.ValidateLifetime = true;
                    parameters.ValidAudience = audience;
                    parameters.ValidAudiences = [audience];
                    // Drop the Entra-specific issuer validator; the issuer comes from
                    // the discovery document instead.
                    parameters.IssuerValidator = null;
                    parameters.ValidIssuers = null;
                }
            );

            services.AddSingleton<
                IAuthorizationHeaderProvider,
                GenericOidcAuthorizationHeaderProvider
            >();
        }

        /// <summary>
        /// Browsers cannot set headers on WebSocket connections, so SignalR passes the
        /// access token in the query string instead.
        /// </summary>
        private static void ConfigureSignalRQueryStringToken(IServiceCollection services)
        {
            services.Configure<JwtBearerOptions>(
                JwtBearerDefaults.AuthenticationScheme,
                options =>
                {
                    options.Events ??= new JwtBearerEvents();
                    options.Events.OnMessageReceived = context =>
                    {
                        if (
                            context.HttpContext.Request.Path.StartsWithSegments("/hub")
                            && context.Request.Query.TryGetValue("access_token", out var token)
                        )
                        {
                            context.Token = token;
                        }
                        return Task.CompletedTask;
                    };
                }
            );
        }
    }
}
