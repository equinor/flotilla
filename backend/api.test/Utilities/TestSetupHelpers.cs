using System;
using System.Data.Common;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Api.Database.Context;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Respawn;
using Testcontainers.PostgreSql;

namespace Api.Test.Utilities;
public static class TestSetupHelpers
{
    public static async Task<(PostgreSqlContainer, string, DbConnection)> ConfigurePostgreSqlContainer()
    {
        var container = new PostgreSqlBuilder().Build();
        await container.StartAsync();

        string? connectionString = container.GetConnectionString();

        var context = ConfigureFlotillaDbContext(connectionString);
        await context.Database.MigrateAsync();

        var connection = context.Database.GetDbConnection();
        await connection.OpenAsync();

        return (container, connectionString, connection);
    }

    public static FlotillaDbContext ConfigureFlotillaDbContext(string connectionString)
    {
        var optionsBuilder = new DbContextOptionsBuilder<FlotillaDbContext>();
        optionsBuilder.UseNpgsql(
            connectionString,
            o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery));
        var context = new FlotillaDbContext(optionsBuilder.Options);
        return context;
    }

    public static async Task<Respawner> ConfigureDatabaseRespawner(DbConnection connection)
    {
        var respawnerOptions = new RespawnerOptions()
        {
            SchemasToInclude = new[] { "public" }, DbAdapter = DbAdapter.Postgres
        };

        return await Respawner.CreateAsync(connection, respawnerOptions);
    }

    public static TestWebApplicationFactory<Program> ConfigureWebApplicationFactory(string databaseConnectionString)
    {
        return new TestWebApplicationFactory<Program>(databaseConnectionString);
    }

    public static HttpClient ConfigureHttpClient(TestWebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost:8000")
        });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            TestAuthHandler.AuthenticationScheme
        );

        return client;
    }

    public static IServiceProvider ConfigureServiceProvider(TestWebApplicationFactory<Program> factory)
    {
        return factory.Services;
    }

    public static JsonSerializerOptions ConfigureJsonSerializerOptions()
    {
        return new JsonSerializerOptions
        {
            Converters =
            {
                new JsonStringEnumConverter()
            },
            PropertyNameCaseInsensitive = true
        };
    }
}
