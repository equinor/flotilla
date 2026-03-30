using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Api.Services.MissionLoaders;
using Testcontainers.PostgreSql;
using Xunit;

namespace Api.Test.Controllers;

public class MissionLoaderControllerTests : IAsyncLifetime
{
    public required PostgreSqlContainer Container;
    public required HttpClient Client;
    public required HttpClient UnauthenticatedClient;
    public required JsonSerializerOptions SerializerOptions;

    public async ValueTask InitializeAsync()
    {
        (Container, var connectionString, _) = await TestSetupHelpers.ConfigurePostgreSqlDatabase();

        var factory = TestSetupHelpers.ConfigureWebApplicationFactory(
            postgreSqlConnectionString: connectionString
        );
        var unauthFactory = TestSetupHelpers.ConfigureUnauthenticatedWebApplicationFactory(
            postgreSqlConnectionString: connectionString
        );

        Client = TestSetupHelpers.ConfigureHttpClient(factory);
        UnauthenticatedClient = TestSetupHelpers.ConfigureUnauthenticatedHttpClient(unauthFactory);
        SerializerOptions = TestSetupHelpers.ConfigureJsonSerializerOptions();
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task GetAvailableMissionsReturnsOkWithMissionsList()
    {
        // Act
        var response = await Client.GetAsync(
            "/mission-loader/available-missions/TTT",
            TestContext.Current.CancellationToken
        );
        var missions = await response.Content.ReadFromJsonAsync<List<CondensedMissionDefinition>>(
            SerializerOptions,
            cancellationToken: TestContext.Current.CancellationToken
        );

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(missions);
        Assert.Single(missions);
        Assert.Equal("TTT", missions[0].InstallationCode);
        Assert.Equal("test", missions[0].Name);
    }

    [Fact]
    public async Task GetMissionByIdReturnsOkWithMission()
    {
        // Act
        var response = await Client.GetAsync(
            "/mission-loader/missions/test-mission-id",
            TestContext.Current.CancellationToken
        );
        var mission = await response.Content.ReadFromJsonAsync<CondensedMissionDefinition>(
            SerializerOptions,
            cancellationToken: TestContext.Current.CancellationToken
        );

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(mission);
        Assert.Equal("TTT", mission.InstallationCode);
        Assert.Equal("test", mission.Name);
    }

    [Fact]
    public async Task GetAvailableMissionsUnauthenticatedUserReturnsUnauthorized()
    {
        // Act
        var response = await UnauthenticatedClient.GetAsync(
            "/mission-loader/available-missions/TTT",
            TestContext.Current.CancellationToken
        );

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMissionByIdUnauthenticatedUserReturnsUnauthorized()
    {
        // Act
        var response = await UnauthenticatedClient.GetAsync(
            "/mission-loader/missions/test-mission-id",
            TestContext.Current.CancellationToken
        );

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
