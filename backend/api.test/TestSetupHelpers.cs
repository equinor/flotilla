using System;
using System.Data.Common;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Api.Database.Context;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Api.Test;

public static class TestSetupHelpers
{
    public static async Task<(string, DbConnection)> ConfigureDatabase(string databaseName)
    {
        string connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = $"file:{databaseName}?mode=memory",
            Cache = SqliteCacheMode.Shared,
        }.ToString();

        var context = ConfigureFlotillaDbContext(connectionString);
        await context.Database.EnsureCreatedAsync();

        var connection = context.Database.GetDbConnection();
        await connection.OpenAsync();

        return (connectionString, connection);
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

    public static FlotillaDbContext ConfigureFlotillaDbContext(string connectionString)
    {
        var optionsBuilder = new DbContextOptionsBuilder<FlotillaDbContext>();
        optionsBuilder.EnableSensitiveDataLogging();
        optionsBuilder.UseSqlite(connectionString);

        var context = new FlotillaDbContext(optionsBuilder.Options);
        return context;
    }

    public static TestWebApplicationFactory<Program> ConfigureWebApplicationFactory(
        string databaseName
    )
    {
        return new TestWebApplicationFactory<Program>(databaseName);
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
}
