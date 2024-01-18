using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Api.Controllers.Models;
using Api.Services;
using Api.Test.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Api.Test.Controllers;

[Collection("Database collection")]
public class InstallationControllerTests(DatabaseFixture fixture) : IAsyncLifetime
{
    private readonly HttpClient _client = fixture.Client;
    private readonly IInstallationService _installationService = fixture.ServiceProvider.GetRequiredService<IInstallationService>();

    private readonly Func<Task> _resetDatabase = fixture.ResetDatabase;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _resetDatabase();
    }

    [Fact]
    public async Task TestCreateInstallationEndpoint()
    {
        // Arrange
        var query = new CreateInstallationQuery
        {
            InstallationCode = "inst",
            Name = "Installation"
        };

        var content = new StringContent(
            JsonSerializer.Serialize(query),
            null,
            "application/json"
        );

        // Act
        const string Url = "/installations";
        var response = await _client.PostAsync(Url, content);

        // Assert
        var installation = await _installationService.ReadByName(query.InstallationCode);

        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal(query.Name, installation!.Name);
    }
}
