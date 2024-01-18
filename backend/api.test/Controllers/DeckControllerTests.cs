using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Api.Controllers.Models;
using Api.Services;
using Api.Test.Database;
using Api.Test.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Api.Test.Controllers;

[Collection("Database collection")]
public class DeckControllerTests(DatabaseFixture fixture) : IAsyncLifetime
{
    private readonly DatabaseUtilities _databaseUtilities = new(fixture.Context);
    private readonly HttpClient _client = fixture.Client;
    private readonly JsonSerializerOptions _serializerOptions = fixture.SerializerOptions;
    private readonly IDeckService _deckService = fixture.ServiceProvider.GetRequiredService<IDeckService>();

    private readonly Func<Task> _resetDatabase = fixture.ResetDatabase;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _resetDatabase();
    }

    [Fact]
    public async Task TestCreatDeckEndpoint()
    {
        // Arrange
        var installation = await _databaseUtilities.NewInstallation();
        var plant = await _databaseUtilities.NewPlant(installation.InstallationCode);

        var query = new CreateDeckQuery
        {
            InstallationCode = installation.InstallationCode,
            PlantCode = plant.PlantCode,
            Name = "deck"
        };

        var content = new StringContent(
            JsonSerializer.Serialize(query),
            null,
            "application/json"
        );

        // Act
        const string Url = "/decks";
        var response = await _client.PostAsync(Url, content);

        // Assert
        var deck = await _deckService.ReadByInstallationAndPlantAndName(installation, plant, query.Name);

        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal(query.Name, deck!.Name);
    }
}
