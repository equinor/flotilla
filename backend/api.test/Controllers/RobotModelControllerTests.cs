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

public class RobotModelControllerTests : IAsyncLifetime
{
    public required DatabaseUtilities DatabaseUtilities;
    public required PostgreSqlContainer Container;
    public required HttpClient Client;
    public required JsonSerializerOptions SerializerOptions;

    public required IRobotModelService RobotModelService;

    public async Task InitializeAsync()
    {
        (Container, var connectionString, var connection) =
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

        RobotModelService = serviceProvider.GetRequiredService<IRobotModelService>();
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
        Assert.Equal(6, robotModels!.Count);
    }

    [Fact]
    public async Task CheckThatLookupRobotModelByRobotTypeReturnsSuccess()
    {
        const RobotType RobotType = RobotType.Robot;

        var response = await Client.GetAsync("/robot-models/type/" + RobotType);
        var robotModel = await response.Content.ReadFromJsonAsync<RobotModel>(SerializerOptions);

        Assert.Equal(RobotType, robotModel!.Type);
    }

    [Fact]
    public async Task CheckThatLookupRobotModelByIdReturnsSuccess()
    {
        var robotModel = await RobotModelService.ReadByRobotType(RobotType.Robot);

        var response = await Client.GetAsync("/robot-models/" + robotModel!.Id);
        var robotModelFromResponse = await response.Content.ReadFromJsonAsync<RobotModel>(
            SerializerOptions
        );

        Assert.Equal(robotModel.Id, robotModelFromResponse!.Id);
        Assert.Equal(robotModel.Type, robotModelFromResponse!.Type);
    }

    [Fact]
    public async Task CheckThatCreateRobotModelReturnsSuccess()
    {
        // Arrange
        var modelBefore = await RobotModelService.ReadByRobotType(RobotType.Robot);
        _ = await RobotModelService.Delete(modelBefore!.Id);

        var query = new CreateRobotModelQuery { RobotType = RobotType.Robot };
        var content = new StringContent(JsonSerializer.Serialize(query), null, "application/json");

        // Act
        var response = await Client.PostAsync("/robot-models", content);

        // Assert
        var modelAfter = await RobotModelService.ReadByRobotType(RobotType.Robot);

        Assert.True(response.IsSuccessStatusCode);
        Assert.NotEqual(modelBefore!.Id, modelAfter!.Id);
    }
}
