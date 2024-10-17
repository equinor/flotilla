using System;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Api.Database.Context;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Api.Test
{
    // Class for building and disposing dbcontext
    public class DatabaseFixture : IDisposable
    {
        public FlotillaDbContext NewContext => CreateContext();
        private SqliteConnection? _connection;

        private DbContextOptions<FlotillaDbContext> CreateOptions()
        {
            string connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = ":memory:",
                Cache = SqliteCacheMode.Shared
            }.ToString();
            _connection = new SqliteConnection(connectionString);
            _connection.Open();
            var builder = new DbContextOptionsBuilder<FlotillaDbContext>();
            builder.EnableSensitiveDataLogging();
            builder.UseSqlite(_connection);
            return builder.Options;
        }

        public FlotillaDbContext CreateContext()
        {
            var options = CreateOptions();
            var context = new FlotillaDbContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        public void Dispose()
        {
            if (_connection != null)
            {
                _connection.Dispose();
                _connection = null;
            }
            GC.SuppressFinalize(this);
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
