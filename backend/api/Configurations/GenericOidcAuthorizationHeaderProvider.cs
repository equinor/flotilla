using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web.Extensibility;

namespace Api.Configurations
{
    /// <summary>
    /// Acquires downstream API tokens from a generic OpenID Connect issuer instead of
    /// Microsoft Entra ID.
    ///
    /// Used only in the <see cref="AuthenticationConfigurations.IntegrationTestEnvironment"/>
    /// environment. <c>IDownstreamApi</c> resolves every bearer token through
    /// <see cref="IAuthorizationHeaderProvider"/>, so substituting this one service
    /// redirects the ISAR, SARA and Pointilla calls in a single place.
    ///
    /// Derives from <see cref="BaseAuthorizationHeaderProvider"/>, the extensibility
    /// point Microsoft.Identity.Web provides for exactly this purpose, so that any
    /// protocol the overrides do not handle still falls back to the default behaviour.
    ///
    /// Every token is an application token: the mock issuer only implements the client
    /// credentials grant, and the integration tests do not exercise on-behalf-of flows.
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

        private string Authority =>
            configuration["AzureAd:Authority"]
            ?? throw new InvalidOperationException("AzureAd:Authority is not configured");

        public override Task<string> CreateAuthorizationHeaderForAppAsync(
            string scopes,
            AuthorizationHeaderProviderOptions? downstreamApiOptions = null,
            CancellationToken cancellationToken = default
        ) => GetAuthorizationHeaderAsync(scopes, cancellationToken);

        public override Task<string> CreateAuthorizationHeaderForUserAsync(
            IEnumerable<string> scopes,
            AuthorizationHeaderProviderOptions? authorizationHeaderProviderOptions = null,
            ClaimsPrincipal? claimsPrincipal = null,
            CancellationToken cancellationToken = default
        ) => GetAuthorizationHeaderAsync(string.Join(' ', scopes), cancellationToken);

        public override Task<string> CreateAuthorizationHeaderAsync(
            IEnumerable<string> scopes,
            AuthorizationHeaderProviderOptions? options = null,
            ClaimsPrincipal? claimsPrincipal = null,
            CancellationToken cancellationToken = default
        ) => GetAuthorizationHeaderAsync(string.Join(' ', scopes), cancellationToken);

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

        private async Task<CachedToken> RequestTokenAsync(
            string scopes,
            CancellationToken cancellationToken
        )
        {
            logger.LogInformation(
                "Acquiring token for scope '{Scopes}' from {Authority}",
                scopes,
                Authority
            );

            using var client = httpClientFactory.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{Authority}/token")
            {
                Content = new FormUrlEncodedContent(
                    new Dictionary<string, string>
                    {
                        ["grant_type"] = "client_credentials",
                        ["scope"] = scopes,
                        ["client_id"] = configuration["AzureAd:ClientId"] ?? "flotilla",
                    }
                ),
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
                    $"Token endpoint at {Authority} returned no access_token for scope '{scopes}'"
                );
            }

            return new CachedToken(
                payload.AccessToken,
                DateTimeOffset.UtcNow.AddSeconds(payload.ExpiresIn ?? 3600)
            );
        }

        private sealed record CachedToken(string AccessToken, DateTimeOffset ExpiresAt);

        private sealed class TokenResponse
        {
            [System.Text.Json.Serialization.JsonPropertyName("access_token")]
            public string? AccessToken { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("expires_in")]
            public int? ExpiresIn { get; set; }
        }
    }
}
