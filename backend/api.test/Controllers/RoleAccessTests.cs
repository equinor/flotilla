using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Api.Controllers.Models;
using Api.Database.Models;
using Api.Test.Database;
using Api.Test.Mocks;
using Api.Test.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
namespace Api.Test.Controllers;

[Collection("Database collection")]
public class RoleAccessTests(DatabaseFixture fixture) : IAsyncLifetime
{
    private readonly DatabaseUtilities _databaseUtilities = new(fixture.Context);
    private readonly HttpClient _client = fixture.Client;
    private readonly JsonSerializerOptions _serializerOptions = fixture.SerializerOptions;

    private readonly MockHttpContextAccessor _httpContextAccessor =
        (MockHttpContextAccessor)fixture.Factory.Services.GetService<IHttpContextAccessor>()!;

    private readonly Func<Task> _resetDatabase = fixture.ResetDatabase;

    public async Task InitializeAsync()
    {
        _httpContextAccessor.SetHttpContextRoles(["Role.Admin"]);
        await Task.CompletedTask;
    }
    public async Task DisposeAsync()
    {
        await _resetDatabase();
    }

    [Fact]
    public async Task CheckThatWrongAccessRoleGivesNotFoundResponse()
    {
        // Arrange
        var installation = await _databaseUtilities.NewInstallation(name: "installation", installationCode: "test");
        var plant = await _databaseUtilities.NewPlant(installation.InstallationCode);

        var accessRoleQuery = new CreateAccessRoleQuery
        {
            InstallationCode = installation.InstallationCode,
            RoleName = "User.Test",
            AccessLevel = RoleAccessLevel.USER
        };
        var accessRoleContent = new StringContent(
            JsonSerializer.Serialize(accessRoleQuery),
            null,
            "application/json"
        );

        const string AccessRoleUrl = "/access-roles";
        var accessRoleResponse = await _client.PostAsync(AccessRoleUrl, accessRoleContent);

        _httpContextAccessor.SetHttpContextRoles(["User.WrongRole"]);

        // Act
        string getPlantUrl = $"/plants/{plant.Id}";
        var plantResponse = await _client.GetAsync(getPlantUrl);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, plantResponse.StatusCode);
    }

    [Fact]
    public async Task ExplicitlyAuthorisedPostInstallationPlantDeckAndAreaTest()
    {
        // Arrange
        var installation = await _databaseUtilities.NewInstallation();
        var plant = await _databaseUtilities.NewPlant(installation.InstallationCode);
        var deck = await _databaseUtilities.NewDeck(installation.InstallationCode, plant.PlantCode);
        var area = await _databaseUtilities.NewArea(installation.InstallationCode, plant.PlantCode, deck.Name);

        var accessRoleQuery = new CreateAccessRoleQuery
        {
            InstallationCode = installation.InstallationCode,
            RoleName = "User.Test",
            AccessLevel = RoleAccessLevel.USER
        };
        var accessRoleContent = new StringContent(
            JsonSerializer.Serialize(accessRoleQuery),
            null,
            "application/json"
        );

        const string AccessRoleUrl = "/access-roles";
        var accessRoleResponse = await _client.PostAsync(AccessRoleUrl, accessRoleContent);

        _httpContextAccessor.SetHttpContextRoles(["User.Test"]);

        // Act
        string getAreaUrl = $"/areas/{area.Id}";
        var areaResponse = await _client.GetAsync(getAreaUrl);
        var sameArea = await areaResponse.Content.ReadFromJsonAsync<AreaResponse>(_serializerOptions);

        // Assert
        Assert.Equal(sameArea!.Id, area.Id);
    }

    [Fact]
    public async Task AdminAuthorisedPostInstallationPlantDeckAndAreaTest()
    {
        // Arrange
        var installation = await _databaseUtilities.NewInstallation();
        var plant = await _databaseUtilities.NewPlant(installation.InstallationCode);
        var deck = await _databaseUtilities.NewDeck(installation.InstallationCode, plant.PlantCode);
        var area = await _databaseUtilities.NewArea(installation.InstallationCode, plant.PlantCode, deck.Name);

        // Act
        string getAreaUrl = $"/areas/{area.Id}";
        var areaResponse = await _client.GetAsync(getAreaUrl);
        var sameArea = await areaResponse.Content.ReadFromJsonAsync<AreaResponse>(_serializerOptions);

        // Assert
        Assert.Equal(sameArea!.Id, area.Id);
    }
}
