using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Api.Test.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Api.Test.Security
{
    public class ProtectedEndpointsTests : IAsyncLifetime
    {
        public required DatabaseUtilities DatabaseUtilities;
        public required HttpClient Client;
        public required EndpointDataSource DataSource;

        public async Task InitializeAsync()
        {
            (_, string connectionString, _) = await TestSetupHelpers.ConfigurePostgreSqlDatabase();

            var factory = TestSetupHelpers.ConfigureUnauthenticatedWebApplicationFactory(
                postgreSqlConnectionString: connectionString
            );

            Client = TestSetupHelpers.ConfigureUnauthenticatedHttpClient(factory);

            using var scope = factory.Services.CreateScope();
            DataSource = scope.ServiceProvider.GetRequiredService<EndpointDataSource>();

            DatabaseUtilities = new DatabaseUtilities(
                TestSetupHelpers.ConfigurePostgreSqlContext(connectionString)
            );
        }

        public Task DisposeAsync() => Task.CompletedTask;

        private static string ReplaceRouteParamsWithDefaults(string route)
        {
            return route
                .Replace("{id}", "1")
                .Replace("{installationCode}", "TEST")
                .Replace("{plantId}", "1")
                .Replace("{missionId}", "1")
                .Replace("{userId}", "1")
                .Replace("{*path}", "test")
                .Replace("{**slug}", "test")
                .Replace("{", "")
                .Replace("}", "");
        }

        private IEnumerable<(string, string)> GetProtectedRoutes()
        {
            var routes = new List<(string, string)>();

            foreach (var endpoint in DataSource.Endpoints.OfType<RouteEndpoint>())
            {
                var metadata = endpoint.Metadata;

                if (metadata.OfType<IAllowAnonymous>().Any())
                    continue;

                var methods =
                    metadata.OfType<HttpMethodMetadata>().FirstOrDefault()?.HttpMethods ?? [];
                var route = endpoint.RoutePattern.RawText ?? string.Empty;
                var path = ReplaceRouteParamsWithDefaults(route);

                foreach (var method in methods)
                    routes.Add((method.ToUpperInvariant(), path));
            }

            return routes.Distinct();
        }

        [Fact]
        public void AllEndpointsShouldRequireAuthorization()
        {
            foreach (var endpoint in DataSource.Endpoints.OfType<RouteEndpoint>())
            {
                var metadata = endpoint.Metadata;
                var hasAllowAnonymous = metadata.OfType<IAllowAnonymous>().Any();
                var hasAuthorize = metadata.OfType<IAuthorizeData>().Any();

                var route = endpoint.RoutePattern.RawText ?? "UNKNOWN";
                var isWhitelisted =
                    route.Contains("health") || route.Contains("login") || route.Contains("hub");

                if (!hasAuthorize && !hasAllowAnonymous && !isWhitelisted)
                    Assert.Fail(
                        $"Endpoint '{route}' is not protected with [Authorize] or [AllowAnonymous]"
                    );
            }
        }

        [Fact]
        public async Task EndpointsShouldReturn403r401forUnauthorizedRequests()
        {
            var testRoutes = GetProtectedRoutes();

            foreach (var (method, url) in testRoutes)
            {
                var request = new HttpRequestMessage(new HttpMethod(method), url);
                var response = await Client.SendAsync(request);

                Assert.True(
                    response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized,
                    $"Expected 403/401 for {method} {url}, but got {response.StatusCode}"
                );
            }
        }
    }
}
