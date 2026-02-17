using System;
using System.Data.Common;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Api.Database.Context;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;
using Xunit.Sdk;

namespace Api.Test;

public static class TestSetupHelpers
{
    private const string PostgresVersion = "postgres:17.6";

    public static async Task<(string, DbConnection)> ConfigureSqLiteDatabase(string databaseName)
    {
        string connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = $"file:{databaseName}?mode=memory",
            Cache = SqliteCacheMode.Shared,
        }.ToString();

        var context = ConfigureSqLiteContext(connectionString);
        await context.Database.EnsureCreatedAsync();

        var connection = context.Database.GetDbConnection();
        await connection.OpenAsync();

        return (connectionString, connection);
    }

    public static async Task<(
        PostgreSqlContainer,
        string,
        DbConnection
    )> ConfigurePostgreSqlDatabase()
    {
        var container = new PostgreSqlBuilder(PostgresVersion).Build();
        await container.StartAsync();

        string? connectionString = container.GetConnectionString();
        var context = ConfigurePostgreSqlContext(connectionString);
        await context.Database.MigrateAsync();

        var connection = context.Database.GetDbConnection();
        await connection.OpenAsync();

        return (container, connectionString, connection);
    }

    public static JsonSerializerOptions ConfigureJsonSerializerOptions()
    {
        return new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter() },
            PropertyNameCaseInsensitive = true,
        };
    }

    public static IServiceProvider ConfigureServiceProvider(
        TestWebApplicationFactory<Program> factory
    )
    {
        return factory.Services;
    }

    public static FlotillaDbContext ConfigureSqLiteContext(string connectionString)
    {
        var optionsBuilder = new DbContextOptionsBuilder<FlotillaDbContext>();
        optionsBuilder.EnableSensitiveDataLogging();
        optionsBuilder.UseSqlite(connectionString);

        var context = new FlotillaDbContext(optionsBuilder.Options);
        return context;
    }

    public static FlotillaDbContext ConfigurePostgreSqlContext(string connectionString)
    {
        var optionsBuilder = new DbContextOptionsBuilder<FlotillaDbContext>();
        optionsBuilder.UseNpgsql(
            connectionString,
            o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery)
        );
        var context = new FlotillaDbContext(optionsBuilder.Options);
        return context;
    }

    public static TestWebApplicationFactory<Program> ConfigureWebApplicationFactory(
        string? databaseName = null,
        string? postgreSqlConnectionString = null
    )
    {
        return new TestWebApplicationFactory<Program>(databaseName, postgreSqlConnectionString);
    }

    public static HttpClient ConfigureHttpClient(TestWebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                BaseAddress = new Uri("http://localhost:8000"),
            }
        );
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            TestAuthHandler.AuthenticationScheme
        );

        return client;
    }

    public static UnauthenticatedWebApplicationFactory<Program> ConfigureUnauthenticatedWebApplicationFactory(
        string? databaseName = null,
        string? postgreSqlConnectionString = null
    )
    {
        return new UnauthenticatedWebApplicationFactory<Program>(
            databaseName,
            postgreSqlConnectionString
        );
    }

    public static HttpClient ConfigureUnauthenticatedHttpClient(
        UnauthenticatedWebApplicationFactory<Program> factory
    )
    {
        var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                BaseAddress = new Uri("http://localhost:8000"),
            }
        );

        return client;
    }

    public static async Task<bool> WaitFor(
        Func<Task<bool>> workFunction,
        float waitPeriodSeconds = 5,
        float waitIncrementSeconds = 1
    )
    {
        DateTime startTime = DateTime.UtcNow;
        while (DateTime.UtcNow < startTime.AddSeconds(waitPeriodSeconds))
        {
            var isDone = await workFunction();
            if (isDone)
                return true;
            var cts = new CancellationTokenSource();
            await Task.Delay((int)(waitIncrementSeconds * 1000), cts.Token);
        }
        throw TrueException.ForNonTrueValue(
            $"Waited for something that never happened within {waitPeriodSeconds} seconds. ",
            false
        );
    }
}
