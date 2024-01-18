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
public class PlantControllerTests(DatabaseFixture fixture) : IAsyncLifetime
{
    private readonly DatabaseUtilities _databaseUtilities = new(fixture.Context);
    private readonly HttpClient _client = fixture.Client;
    private readonly JsonSerializerOptions _serializerOptions = fixture.SerializerOptions;
    private readonly IPlantService _plantService = fixture.ServiceProvider.GetRequiredService<IPlantService>();

    private readonly Func<Task> _resetDatabase = fixture.ResetDatabase;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _resetDatabase();
    }

    [Fact]
    public async Task TestCreatPlantEndpoint()
    {
        // Arrange
        var installation = await _databaseUtilities.NewInstallation();

        var query = new CreatePlantQuery
        {
            InstallationCode = installation.InstallationCode,
            PlantCode = "plantCode",
            Name = "plant"
        };

        var content = new StringContent(
            JsonSerializer.Serialize(query),
            null,
            "application/json"
        );

        // Act
        const string Url = "/plants";
        var response = await _client.PostAsync(Url, content);

        // Assert
        var plant = await _plantService.ReadByInstallationAndName(installation, query.PlantCode);

        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal(query.Name, plant!.Name);
    }
}

