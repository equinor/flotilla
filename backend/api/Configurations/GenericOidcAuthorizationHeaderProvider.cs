using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web.Extensibility;

namespace Api.Configurations
{
    /// <summary>
    /// Acquires downstream API tokens from a generic OpenID Connect issuer when
    /// <c>Authentication:Provider</c> is <c>Oidc</c>. Always an application token.
    ///
    /// The token endpoint is read from the discovery document rather than assumed:
    /// Entra publishes it under <c>/oauth2/v2.0/token</c> and Keycloak under
    /// <c>/protocol/openid-connect/token</c>.
    /// </summary>
    public class GenericOidcAuthorizationHeaderProvider(
        IServiceProvider serviceProvider,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<GenericOidcAuthorizationHeaderProvider> logger
    ) : BaseAuthorizationHeaderProvider(serviceProvider)
    {
        private const string BearerScheme = "Bearer";

        // Renew a little before expiry so a token cannot lapse mid-request.
        private static readonly TimeSpan ExpiryMargin = TimeSpan.FromSeconds(60);

        private readonly ConcurrentDictionary<string, CachedToken> _cache = new();
        private readonly SemaphoreSlim _lock = new(1, 1);
        private readonly SemaphoreSlim _discoveryLock = new(1, 1);

        private string? _tokenEndpoint;

        private string Authority =>
            configuration["AzureAd:Authority"]
            ?? throw new InvalidOperationException("AzureAd:Authority is not configured");

        private string ClientId =>
            configuration["AzureAd:ClientId"]
            ?? throw new InvalidOperationException("AzureAd:ClientId is not configured");

        public override Task<string> CreateAuthorizationHeaderForAppAsync(
            string scopes,
            AuthorizationHeaderProviderOptions? downstreamApiOptions = null,
            CancellationToken cancellationToken = default
        ) => GetAuthorizationHeaderAsync(scopes, cancellationToken);

        /// <summary>
        /// On-behalf-of is not implemented for a generic issuer. Delegating to the
        /// application token instead would send a service principal where the caller
        /// asked for the signed-in user, so this refuses rather than substituting a
        /// different identity. Call <c>CallApiForAppAsync</c>, or run the service
        /// under the EntraId provider, which does support on-behalf-of.
        /// </summary>
        public override Task<string> CreateAuthorizationHeaderForUserAsync(
            IEnumerable<string> scopes,
            AuthorizationHeaderProviderOptions? authorizationHeaderProviderOptions = null,
            ClaimsPrincipal? claimsPrincipal = null,
            CancellationToken cancellationToken = default
        ) => throw new NotSupportedException(OnBehalfOfNotSupported);

        /// <inheritdoc cref="CreateAuthorizationHeaderForUserAsync" />
        /// <remarks>
        /// The unified entry point: <c>IDownstreamApi</c> routes both flows through
        /// here and distinguishes them with <c>RequestAppToken</c>, so this must serve
        /// the app flow and refuse only the user one.
        /// </remarks>
        public override Task<string> CreateAuthorizationHeaderAsync(
            IEnumerable<string> scopes,
            AuthorizationHeaderProviderOptions? options = null,
            ClaimsPrincipal? claimsPrincipal = null,
            CancellationToken cancellationToken = default
        ) =>
            options?.RequestAppToken == true
                ? GetAuthorizationHeaderAsync(string.Join(' ', scopes), cancellationToken)
                : throw new NotSupportedException(OnBehalfOfNotSupported);

        public const string OnBehalfOfNotSupported =
            "On-behalf-of token acquisition is not supported when "
            + "Authentication:Provider is Oidc. Use an application token via "
            + "CallApiForAppAsync, or set Authentication:Provider to EntraId.";

        private async Task<string> GetAuthorizationHeaderAsync(
            string scopes,
            CancellationToken cancellationToken
        )
        {
            if (
                _cache.TryGetValue(scopes, out var cached)
                && cached.ExpiresAt - ExpiryMargin > DateTimeOffset.UtcNow
            )
            {
                return $"{BearerScheme} {cached.AccessToken}";
            }

            await _lock.WaitAsync(cancellationToken);
            try
            {
                // Another caller may have refreshed while this one waited.
                if (
                    _cache.TryGetValue(scopes, out cached)
                    && cached.ExpiresAt - ExpiryMargin > DateTimeOffset.UtcNow
                )
                {
                    return $"{BearerScheme} {cached.AccessToken}";
                }

                var token = await RequestTokenAsync(scopes, cancellationToken);
                _cache[scopes] = token;
                return $"{BearerScheme} {token.AccessToken}";
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <summary>
        /// Resolve the token endpoint from the issuer's OpenID configuration, once.
        /// </summary>
        private async Task<string> GetTokenEndpointAsync(CancellationToken cancellationToken)
        {
            if (_tokenEndpoint is not null)
            {
                return _tokenEndpoint;
            }

            await _discoveryLock.WaitAsync(cancellationToken);
            try
            {
                if (_tokenEndpoint is not null)
                {
                    return _tokenEndpoint;
                }

                string configurationUrl =
                    $"{Authority.TrimEnd('/')}/.well-known/openid-configuration";

                using var client = httpClientFactory.CreateClient();
                var document = await client.GetFromJsonAsync<OpenIdConfiguration>(
                    configurationUrl,
                    cancellationToken
                );

                _tokenEndpoint =
                    document?.TokenEndpoint
                    ?? throw new InvalidOperationException(
                        $"The OpenID configuration at {configurationUrl} advertises no token_endpoint"
                    );

                logger.LogInformation(
                    "Resolved token endpoint {TokenEndpoint} from {ConfigurationUrl}",
                    _tokenEndpoint,
                    configurationUrl
                );

                return _tokenEndpoint;
            }
            finally
            {
                _discoveryLock.Release();
            }
        }

        private async Task<CachedToken> RequestTokenAsync(
            string scopes,
            CancellationToken cancellationToken
        )
        {
            string tokenEndpoint = await GetTokenEndpointAsync(cancellationToken);

            logger.LogInformation(
                "Acquiring token for scope '{Scopes}' from {TokenEndpoint}",
                scopes,
                tokenEndpoint
            );

            var form = new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["scope"] = scopes,
                ["client_id"] = ClientId,
            };

            // The client credentials grant requires an authenticated client. Keycloak,
            // for one, refuses it outright from a public client.
            string? clientSecret = configuration["AzureAd:ClientSecret"];
            if (!string.IsNullOrEmpty(clientSecret))
            {
                form["client_secret"] = clientSecret;
            }

            using var client = httpClientFactory.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
            {
                Content = new FormUrlEncodedContent(form),
            };
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var response = await client.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadFromJsonAsync<TokenResponse>(
                cancellationToken: cancellationToken
            );

            if (string.IsNullOrEmpty(payload?.AccessToken))
            {
                throw new InvalidOperationException(
                    $"Token endpoint at {tokenEndpoint} returned no access_token for scope '{scopes}'"
                );
            }

            return new CachedToken(
                payload.AccessToken,
                DateTimeOffset.UtcNow.AddSeconds(payload.ExpiresIn ?? 3600)
            );
        }

        private sealed record CachedToken(string AccessToken, DateTimeOffset ExpiresAt);

        private sealed class OpenIdConfiguration
        {
            [System.Text.Json.Serialization.JsonPropertyName("token_endpoint")]
            public string? TokenEndpoint { get; set; }
        }

        private sealed class TokenResponse
        {
            [System.Text.Json.Serialization.JsonPropertyName("access_token")]
            public string? AccessToken { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("expires_in")]
            public int? ExpiresIn { get; set; }
        }
    }
}
