using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Api.Database.Context;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Testcontainers.PostgreSql;
using Xunit;

namespace Api.Test
{
    // Class for building and disposing dbcontext
    public class DatabaseFixture : IAsyncLifetime
    {
        private readonly PostgreSqlContainer _container = new PostgreSqlBuilder().Build();

        private string ConnectionString => _container.GetConnectionString();
        public string ContainerId => $"{_container.Id}";
        public Task InitializeAsync()
            => _container.StartAsync();
        public Task DisposeAsync()
            => _container.DisposeAsync().AsTask();

        public FlotillaDbContext Context => CreateContext();

        private FlotillaDbContext CreateContext()
        {
            var optionsBuilder = new DbContextOptionsBuilder<FlotillaDbContext>();
            optionsBuilder.UseNpgsql(
                ConnectionString,
                o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery));
            var context = new FlotillaDbContext(optionsBuilder.Options);
            context.Database.EnsureCreated();
            //InitDb.PopulateDb(context);
            return context;
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
