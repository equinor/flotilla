using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Api.Controllers.Models;
using Api.Database.Models;
using Api.Test.Database;
using Api.Test.Utilities;
using Xunit;
namespace Api.Test.Controllers;

[Collection("Database collection")]
public class RobotControllerTests(DatabaseFixture fixture) : IAsyncLifetime
{
    private readonly DatabaseUtilities _databaseUtilities = new(fixture.Context);
    private readonly HttpClient _client = fixture.Client;
    private readonly JsonSerializerOptions _serializerOptions = fixture.SerializerOptions;

    private readonly Func<Task> _resetDatabase = fixture.ResetDatabase;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _resetDatabase();
    }

    [Fact]
    public async Task CheckThatGetRobotsGivesTheExpectedResult()
    {
        // Arrange
        var installation = await _databaseUtilities.NewInstallation();
        var plant = await _databaseUtilities.NewPlant(installation.InstallationCode);
        var deck = await _databaseUtilities.NewDeck(installation.InstallationCode, plant.PlantCode);
        var area = await _databaseUtilities.NewArea(installation.InstallationCode, plant.PlantCode, deck.Name);
        var robotOne = await _databaseUtilities.NewRobot(RobotStatus.Available, installation, area);
        var robotTwo = await _databaseUtilities.NewRobot(RobotStatus.Available, installation, area);

        // Act
        const string Url = "/robots";
        var response = await _client.GetAsync(Url);
        var robots = await response.Content.ReadFromJsonAsync<List<RobotResponse>>(_serializerOptions);

        // Assert
        Assert.Equal(2, robots!.Count);
    }

    [Fact]
    public async Task CheckThatGetRobotByIdReturnsNotFoundForInvalidId()
    {
        const string RobotId = "RandomString";
        const string Url = "/robots/" + RobotId;
        var response = await _client.GetAsync(Url);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CheckThatGetRobotByIdReturnsCorrectRobot()
    {
        // Arrange
        var installation = await _databaseUtilities.NewInstallation();
        var plant = await _databaseUtilities.NewPlant(installation.InstallationCode);
        var deck = await _databaseUtilities.NewDeck(installation.InstallationCode, plant.PlantCode);
        var area = await _databaseUtilities.NewArea(installation.InstallationCode, plant.PlantCode, deck.Name);
        var robot = await _databaseUtilities.NewRobot(RobotStatus.Available, installation, area);

        // Act
        var robotResponse = await _client.GetAsync("/robots/" + robot.Id);
        var robotFromResponse = await robotResponse.Content.ReadFromJsonAsync<RobotResponse>(_serializerOptions);

        // Assert
        Assert.Equal(robot.IsarId, robotFromResponse!.IsarId);
        Assert.Equal(robot.Id, robotFromResponse!.Id);
    }
}
