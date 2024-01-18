using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Api.Controllers.Models;
using Api.Database.Models;
using Api.Services;
using Api.Test.Database;
using Api.Test.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
namespace Api.Test.Controllers;

[Collection("Database collection")]
public class AreaControllerTests(DatabaseFixture fixture) : IAsyncLifetime
{
    private readonly DatabaseUtilities _databaseUtilities = new(fixture.Context);
    private readonly HttpClient _client = fixture.Client;
    private readonly JsonSerializerOptions _serializerOptions = fixture.SerializerOptions;

    private readonly IAreaService _areaService = fixture.ServiceProvider.GetRequiredService<IAreaService>();

    private readonly IMissionDefinitionService _missionDefinitionService =
        fixture.ServiceProvider.GetRequiredService<IMissionDefinitionService>();

    private readonly IMissionRunService _missionRunService =
        fixture.ServiceProvider.GetRequiredService<IMissionRunService>();

    private readonly Func<Task> _resetDatabase = fixture.ResetDatabase;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _resetDatabase();
    }

    [Fact]
    public async Task TestCreateAreaEndpoint()
    {
        // Arrange
        var installation = await _databaseUtilities.NewInstallation();
        var plant = await _databaseUtilities.NewPlant(installation.InstallationCode);
        var deck = await _databaseUtilities.NewDeck(installation.InstallationCode, plant.PlantCode);

        var query = new CreateAreaQuery
        {
            InstallationCode = installation.InstallationCode,
            PlantCode = plant.PlantCode,
            DeckName = deck.Name,
            AreaName = "area",
            DefaultLocalizationPose = new Pose()
        };

        var areaContent = new StringContent(
            JsonSerializer.Serialize(query),
            null,
            "application/json"
        );

        // Act
        const string AreaUrl = "/areas";
        var response = await _client.PostAsync(AreaUrl, areaContent);

        // Assert
        var area = await _areaService.ReadByInstallationAndName(installation.InstallationCode, query.AreaName);

        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal(query.AreaName, area!.Name);
    }

    [Fact(Skip = "Reason")]
    public async Task CheckThatMissionDefinitionIsCreatedInAreaWhenSchedulingACustomMissionRun()
    {
        // Arrange
        var installation = await _databaseUtilities.NewInstallation();
        var plant = await _databaseUtilities.NewPlant(installation.InstallationCode);
        var deck = await _databaseUtilities.NewDeck(installation.InstallationCode, plant.PlantCode);
        var area = await _databaseUtilities.NewArea(installation.InstallationCode, plant.PlantCode, deck.Name);
        var robot = await _databaseUtilities.NewRobot(RobotStatus.Available, installation, area);

        var inspections = new List<CustomInspectionQuery>
        {
            new()
            {
                AnalysisType = AnalysisType.CarSeal,
                InspectionTarget = new Position(),
                InspectionType = InspectionType.Image
            }
        };
        var tasks = new List<CustomTaskQuery>
        {
            new()
            {
                Inspections = inspections,
                InspectionTarget = new Position(),
                TagId = "test",
                RobotPose = new Pose(),
                TaskOrder = 0
            }
        };
        var missionQuery = new CustomMissionQuery
        {
            RobotId = robot.Id,
            DesiredStartTime = DateTime.UtcNow,
            InstallationCode = installation.InstallationCode,
            AreaName = area.Name,
            Name = "missionName",
            Tasks = tasks
        };

        var missionContent = new StringContent(
            JsonSerializer.Serialize(missionQuery),
            null,
            "application/json"
        );

        // Act
        const string MissionUrl = "/missions/custom";
        var missionResponse = await _client.PostAsync(MissionUrl, missionContent);

        var userMissionResponse = await missionResponse.Content.ReadFromJsonAsync<MissionRun>(_serializerOptions);
        var areaMissionsResponse = await _client.GetAsync( $"/areas/{area.Id}/mission-definitions");

        // Assert
        var mission = await _missionRunService.ReadById(userMissionResponse!.Id);
        var missionDefinitions = await _missionDefinitionService.ReadByAreaId(area.Id);

        Assert.True(missionResponse.IsSuccessStatusCode);
        Assert.True(areaMissionsResponse.IsSuccessStatusCode);
        Assert.Single(missionDefinitions.Where(m => m.Id.Equals(mission!.MissionId, StringComparison.Ordinal)));
    }

    [Fact]
    public async Task CheckThatGoToSafePositionIsSuccessfullyInitiated()
    {
        // Arrange
        var installation = await _databaseUtilities.NewInstallation();
        var plant = await _databaseUtilities.NewPlant(installation.InstallationCode);
        var deck = await _databaseUtilities.NewDeck(installation.InstallationCode, plant.PlantCode);
        var area = await _databaseUtilities.NewArea(installation.InstallationCode, plant.PlantCode, deck.Name);

        string addSafePositionUrl = $"/areas/{installation.InstallationCode}/{area.Name}/safe-position";
        var testPosition = new Position
        {
            X = 1,
            Y = 2,
            Z = 2
        };
        var query = new Pose
        {
            Position = testPosition,
            Orientation = new Orientation
            {
                X = 0,
                Y = 0,
                Z = 0,
                W = 1
            }
        };
        var content = new StringContent(
            JsonSerializer.Serialize(query),
            null,
            "application/json"
        );

        var areaResponse = await _client.PostAsync(addSafePositionUrl, content);
        var areaContent = await areaResponse.Content.ReadFromJsonAsync<AreaResponse>(_serializerOptions);

        Assert.True(areaResponse.IsSuccessStatusCode);
        Assert.True(areaContent != null);

        // Act
        string goToSafePositionUrl = $"/emergency-action/{installation.InstallationCode}/abort-current-missions-and-send-all-robots-to-safe-zone";
        var missionResponse = await _client.PostAsync(goToSafePositionUrl, null);

        // Assert
        Assert.True(missionResponse.IsSuccessStatusCode);

        // The endpoint posted to above triggers an event and returns a successful response.
        // The test finishes and disposes of objects, but the operations of that event handler are still running, leading to a crash.
        await Task.Delay(5000);
    }

    [Fact]
    public async Task CheckThatMapMetadataIsFoundForArea()
    {
        // Arrange
        var installation = await _databaseUtilities.NewInstallation();
        var plant = await _databaseUtilities.NewPlant(installation.InstallationCode);
        var deck = await _databaseUtilities.NewDeck(installation.InstallationCode, plant.PlantCode);
        var area = await _databaseUtilities.NewArea(installation.InstallationCode, plant.PlantCode, deck.Name);

        // Act
        string url = $"/areas/{area.Id}/map-metadata";
        var response = await _client.GetAsync(url);
        var mapMetadata = await response.Content.ReadFromJsonAsync<MapMetadata>(_serializerOptions);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(mapMetadata);
    }

    [Fact]
    public async Task CheckThatMapMetadataIsNotFoundForInvalidArea()
    {
        var responseInvalid = await _client.GetAsync("/areas/invalidAreaId/map-metadata");
        Assert.Equal(HttpStatusCode.NotFound, responseInvalid.StatusCode);
    }

    [Fact]
    public async Task CheckThatDefaultLocalizationPoseIsUpdatedOnDeck()
    {
        var installation = await _databaseUtilities.NewInstallation();
        var plant = await _databaseUtilities.NewPlant(installation.InstallationCode);
        var deck = await _databaseUtilities.NewDeck(installation.InstallationCode, plant.PlantCode);

        string url = $"/decks/{deck.Id}/update-default-localization-pose";
        var query = new Pose
        {
            Position = new Position
            {
                X = 1,
                Y = 2,
                Z = 3
            },
            Orientation = new Orientation
            {
                X = 0,
                Y = 0,
                Z = 0,
                W = 1
            }
        };
        var content = new StringContent(
            JsonSerializer.Serialize(query),
            null,
            "application/json"
        );

        var response = await _client.PutAsync(url, content);
        var updatedDeck = await response.Content.ReadFromJsonAsync<DeckResponse>(_serializerOptions);

        Assert.Equal(updatedDeck!.DefaultLocalizationPose!.Position, query.Position);
        Assert.Equal(updatedDeck!.DefaultLocalizationPose.Orientation, query.Orientation);
    }

    [Fact]
    public async Task CheckThatAddingDuplicateAreaNameFails()
    {
        // Arrange
        var installation = await _databaseUtilities.NewInstallation();
        var plant = await _databaseUtilities.NewPlant(installation.InstallationCode);
        var deck = await _databaseUtilities.NewDeck(installation.InstallationCode, plant.PlantCode);
        var area = await _databaseUtilities.NewArea(installation.InstallationCode, plant.PlantCode, deck.Name);

        var query = new CreateAreaQuery
        {
            InstallationCode = installation.InstallationCode,
            PlantCode = plant.PlantCode,
            DeckName = deck.Name,
            AreaName = area.Name,
            DefaultLocalizationPose = new Pose()
        };

        var areaContent = new StringContent(
            JsonSerializer.Serialize(query),
            null,
            "application/json"
        );

        // Act
        const string AreaUrl = "/areas";
        var response = await _client.PostAsync(AreaUrl, areaContent);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}

