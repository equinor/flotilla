using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Api.Controllers.Models;
using Api.Database.Models;
using Api.Services;
using Api.Test.Database;
using Microsoft.Extensions.DependencyInjection;
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

    public async Task InitializeAsync()
    {
        (Container, string connectionString, var connection) =
            await TestSetupHelpers.ConfigurePostgreSqlDatabase();
        var factory = TestSetupHelpers.ConfigureWebApplicationFactory(
            postgreSqlConnectionString: connectionString
        );
        var serviceProvider = TestSetupHelpers.ConfigureServiceProvider(factory);

        Client = TestSetupHelpers.ConfigureHttpClient(factory);
        SerializerOptions = TestSetupHelpers.ConfigureJsonSerializerOptions();
        DatabaseUtilities = new DatabaseUtilities(
            TestSetupHelpers.ConfigurePostgreSqlContext(connectionString)
        );

        SourceService = serviceProvider.GetRequiredService<ISourceService>();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CheckThatListAllSourcesWorksAsExpected()
    {
        // Arrange
        var sourceOne = await DatabaseUtilities.NewSource(sourceId: "TestIdOne");
        var sourceTwo = await DatabaseUtilities.NewSource(sourceId: "TestIdTwo");
        
        // Act
        var response = await Client.GetAsync($"/sources");
        var sources = await response.Content.ReadFromJsonAsync<List<Source>>(SerializerOptions);
        
        // Assert
        Assert.Equal(2, sources!.Count);
        Assert.Equal(sourceOne.Id, sources[0].Id);
        Assert.Equal(sourceTwo.Id, sources[1].Id);
    }
}
