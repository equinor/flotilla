using System;
using System.Data.Common;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using Api.Database.Context;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Respawn;
using Testcontainers.PostgreSql;
using Xunit;

namespace Api.Test.Utilities
{
    public class DatabaseFixture : IAsyncLifetime
    {
        public FlotillaDbContext Context => CreateContext();

        public required TestWebApplicationFactory<Program> Factory;

        public required IServiceProvider ServiceProvider;

        public required HttpClient Client;

        public required JsonSerializerOptions SerializerOptions;

        public required PostgreSqlContainer Container;

        public required string ConnectionString;

        public required Respawner Respawner;

        public required DbConnection Connection;

        public async Task InitializeAsync()
        {
            (Container, ConnectionString, Connection) =
                await TestSetupHelpers.ConfigurePostgreSqlContainer();
            Respawner = await TestSetupHelpers.ConfigureDatabaseRespawner(Connection);
            Factory = TestSetupHelpers.ConfigureWebApplicationFactory(ConnectionString);
            Client = TestSetupHelpers.ConfigureHttpClient(Factory);
            ServiceProvider = TestSetupHelpers.ConfigureServiceProvider(Factory);
            SerializerOptions = TestSetupHelpers.ConfigureJsonSerializerOptions();
        }

        public async Task DisposeAsync()
        {
            await Context.DisposeAsync();
            await Connection.CloseAsync();
            await Container.DisposeAsync();
            await Factory.DisposeAsync();
        }

        public Task ResetDatabase()
        {
            return Respawner.ResetAsync(Connection);
        }

        private FlotillaDbContext CreateContext()
        {
            return TestSetupHelpers.ConfigureFlotillaDbContext(ConnectionString);
        }
    }

    [CollectionDefinition("Database collection")]
    public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }

    // Class for mocking authentication options
    public class TestAuthHandlerOptions : AuthenticationSchemeOptions
    {
        public string DefaultUserId { get; set; } = null!;
    }

    // Class for mocking authentication handler
    public class TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
                               ILoggerFactory logger,
                               UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public const string AuthenticationScheme = "Test";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, "Test.User"),
                new Claim(ClaimTypes.Role, "Role.Admin")
            };
            var identity = new ClaimsIdentity(claims, AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, AuthenticationScheme);

            var result = AuthenticateResult.Success(ticket);

            return Task.FromResult(result);
        }
    }
}
