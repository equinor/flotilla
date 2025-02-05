using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Api.Database.Models;
using Api.Test.Database;
using Testcontainers.PostgreSql;
using Xunit;

namespace Api.Test.Controllers;

public class RobotModelControllerTests : IAsyncLifetime
{
    public required DatabaseUtilities DatabaseUtilities;
    public required PostgreSqlContainer Container;
    public required HttpClient Client;
    public required JsonSerializerOptions SerializerOptions;

    public async Task InitializeAsync()
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

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CheckThatListAllRobotModelsReturnsSuccess()
    {
        var response = await Client.GetAsync("/robot-models");
        var robotModels = await response.Content.ReadFromJsonAsync<List<RobotModel>>(
            SerializerOptions
        );

        // Seven models are added by default to the database
        // This number must be changed if new robots are introduced
        Assert.Equal(7, robotModels!.Count);
    }
}
