using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Api.Database.Models;
using Api.Services;
using Api.Test.Database;
using Testcontainers.PostgreSql;
using Xunit;

namespace Api.Test.Controllers;

public class SourceControllerTests : IAsyncLifetime
{
    public required DatabaseUtilities DatabaseUtilities;
    public required PostgreSqlContainer Container;
    public required HttpClient Client;
    public required JsonSerializerOptions SerializerOptions;

    public required ISourceService SourceService;

    public async ValueTask InitializeAsync()
    {
        (Container, var connectionString, var connection) =
            await TestSetupHelpers.ConfigurePostgreSqlDatabase();
        var factory = TestSetupHelpers.ConfigureWebApplicationFactory(
            postgreSqlConnectionString: connectionString
        );

        Client = TestSetupHelpers.ConfigureHttpClient(factory);
        SerializerOptions = TestSetupHelpers.ConfigureJsonSerializerOptions();
        DatabaseUtilities = new DatabaseUtilities(
            TestSetupHelpers.ConfigurePostgreSqlContext(connectionString)
        );
    }

    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task CheckThatListAllSourcesWorksAsExpected()
    {
        // Arrange
        var sourceOne = await DatabaseUtilities.NewSource(sourceId: "TestIdOne");
        var sourceTwo = await DatabaseUtilities.NewSource(sourceId: "TestIdTwo");

        // Act
        var response = await Client.GetAsync("/sources", TestContext.Current.CancellationToken);
        var sources = await response.Content.ReadFromJsonAsync<List<Source>>(
            SerializerOptions,
            cancellationToken: TestContext.Current.CancellationToken
        );

        // Assert
        Assert.Equal(2, sources!.Count);
        Assert.Equal(sourceOne.Id, sources[0].Id);
        Assert.Equal(sourceTwo.Id, sources[1].Id);
    }

    [Fact]
    public async Task CheckThatLookupSourceByIdWorksAsExpected()
    {
        var source = await DatabaseUtilities.NewSource();
        var response = await Client.GetAsync(
            $"/sources/{source.Id}",
            TestContext.Current.CancellationToken
        );
        var sourceFromResponse = await response.Content.ReadFromJsonAsync<Source>(
            SerializerOptions,
            cancellationToken: TestContext.Current.CancellationToken
        );
        Assert.Equal(source.Id, sourceFromResponse!.Id);
    }
}
