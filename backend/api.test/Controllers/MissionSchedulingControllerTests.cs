using System;
using System.Data.Common;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Api.Controllers.Models;
using Api.Database.Context;
using Api.Database.Models;
using Api.Services;
using Api.Test.Database;
using Api.Test.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;
using Xunit.Abstractions;

namespace Api.Test.Controllers;

public class MissionSchedulingControllerTests(ITestOutputHelper outputHelper) : IAsyncLifetime
{
    private FlotillaDbContext Context => CreateContext();
    private TestWebApplicationFactory<Program> _factory;
    private IServiceProvider _serviceProvider;
    private HttpClient _client;
    private PostgreSqlContainer _container;
    private string _connectionString;
    private DbConnection _connection;
    private DatabaseUtilities _databaseUtilities;
    private JsonSerializerOptions _serializerOptions;
    private IMissionRunService _missionRunService;
    private IMissionDefinitionService _missionDefinitionService;

    public async Task InitializeAsync()
    {
        (var container, string connectionString, var connection) =
            await TestSetupHelpers.ConfigurePostgreSqlContainer();
        _container = container;
        _connectionString = connectionString;
        _connection = connection;

        outputHelper.WriteLine($"Connection string is {connectionString}");

        _databaseUtilities = new DatabaseUtilities(Context);

        _factory = TestSetupHelpers.ConfigureWebApplicationFactory(_connectionString);
        _client = TestSetupHelpers.ConfigureHttpClient(_factory);

        _serviceProvider = TestSetupHelpers.ConfigureServiceProvider(_factory);
        _serializerOptions = TestSetupHelpers.ConfigureJsonSerializerOptions();

        _missionRunService = _serviceProvider.GetRequiredService<IMissionRunService>();
        _missionDefinitionService = _serviceProvider.GetRequiredService<IMissionDefinitionService>();
    }

    public async Task DisposeAsync()
    {
        //await Task.CompletedTask;
        //await Context.DisposeAsync();
        //await _connection.CloseAsync();
        await _factory.DisposeAsync();
        await _container.DisposeAsync();

        //await Task.Delay(5000);
    }

    private FlotillaDbContext CreateContext()
    {
        return TestSetupHelpers.ConfigureFlotillaDbContext(_connectionString);
    }

    [Fact]
    public async Task CheckThatOneEchoMissionIsSuccessfullyStarted()
    {
        // Arrange
        var installation = await _databaseUtilities.NewInstallation();
        var plant = await _databaseUtilities.NewPlant(installation.InstallationCode);
        var deck = await _databaseUtilities.NewDeck(installation.InstallationCode, plant.PlantCode);
        var area = await _databaseUtilities.NewArea(installation.InstallationCode, plant.PlantCode, deck.Name);
        var robot = await _databaseUtilities.NewRobot(RobotStatus.Available, installation, area);

        const string MissionsUrl = "/missions";
        const int EchoMissionId = 95;

        // Act
        var query = new ScheduledMissionQuery
        {
            RobotId = robot.Id,
            InstallationCode = installation.InstallationCode,
            AreaName = area.Name,
            EchoMissionId = EchoMissionId,
            DesiredStartTime = DateTime.UtcNow
        };
        var content = new StringContent(
            JsonSerializer.Serialize(query),
            null,
            "application/json"
        );

        var response = await _client.PostAsync(MissionsUrl, content);
        var missionRun = await response.Content.ReadFromJsonAsync<MissionRun>(_serializerOptions);

        // Assert
        Assert.True(missionRun!.Id != null);
        Assert.True(missionRun.Status == MissionStatus.Pending);
    }

    [Fact(Skip = "Crisis")]
    public async Task CheckThatSchedulingMultipleEchoMissionsBehavesAsExpected()
    {
        // Arrange
        var installation = await _databaseUtilities.NewInstallation();
        var plant = await _databaseUtilities.NewPlant(installation.InstallationCode);
        var deck = await _databaseUtilities.NewDeck(installation.InstallationCode, plant.PlantCode);
        var area = await _databaseUtilities.NewArea(installation.InstallationCode, plant.PlantCode, deck.Name);
        var robot = await _databaseUtilities.NewRobot(RobotStatus.Available, installation, area);

        const string MissionsUrl = "/missions";
        const int EchoMissionId = 97;

        // Act
        var query = new ScheduledMissionQuery
        {
            RobotId = robot.Id,
            InstallationCode = installation.InstallationCode,
            AreaName = area.Name,
            EchoMissionId = EchoMissionId,
            DesiredStartTime = DateTime.UtcNow
        };
        var content = new StringContent(
            JsonSerializer.Serialize(query),
            null,
            "application/json"
        );

        var responseOne = await _client.PostAsync(MissionsUrl, content);
        var missionRunOne = await responseOne.Content.ReadFromJsonAsync<MissionRun>(_serializerOptions);

        var responseTwo = await _client.PostAsync(MissionsUrl, content);
        var missionRunTwo = await responseTwo.Content.ReadFromJsonAsync<MissionRun>(_serializerOptions);

        var responseThree = await _client.PostAsync(MissionsUrl, content);
        var missionRunThree = await responseThree.Content.ReadFromJsonAsync<MissionRun>(_serializerOptions);

        var missionRuns = await _missionRunService.ReadAll(new MissionRunQueryStringParameters());

        // Assert
        Assert.True(missionRuns.Where((m) => m.Id == missionRunOne!.Id).ToList().Count == 1);
        Assert.True(missionRuns.Where((m) => m.Id == missionRunTwo!.Id).ToList().Count == 1);
        Assert.True(missionRuns.Where((m) => m.Id == missionRunThree!.Id).ToList().Count == 1);
    }

    [Fact]
    public async Task CheckThatGetMissionByIdReturnsNotFoundForInvalidId()
    {
        const string MissionId = "RandomString";
        const string Url = "/missions/runs/" + MissionId;
        var response = await _client.GetAsync(Url);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CheckThatDeleteMissionReturnsNotFoundForInvalidId()
    {
        const string MissionId = "RandomString";
        const string Url = "/missions/runs/" + MissionId;
        var response = await _client.DeleteAsync(Url);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CheckThatSchedulingDuplicateCustomMissionsIsSuccessful()
    {
        // Arrange
        var installation = await _databaseUtilities.NewInstallation();
        var plant = await _databaseUtilities.NewPlant(installation.InstallationCode);
        var deck = await _databaseUtilities.NewDeck(installation.InstallationCode, plant.PlantCode);
        var area = await _databaseUtilities.NewArea(installation.InstallationCode, plant.PlantCode, deck.Name);
        var robot = await _databaseUtilities.NewRobot(RobotStatus.Available, installation, area);

        var query = new CustomMissionQuery
        {
            RobotId = robot.Id,
            InstallationCode = installation.InstallationCode,
            AreaName = area.Name,
            DesiredStartTime = DateTime.UtcNow,
            InspectionFrequency = new TimeSpan(14, 0, 0, 0),
            Name = "TestMission",
            Tasks = [
                new CustomTaskQuery
                {
                    RobotPose = new Pose(new Position(23, 14, 4), new Orientation()),
                    Inspections = [],
                    TaskOrder = 0
                },
                new CustomTaskQuery
                {
                    RobotPose = new Pose(),
                    Inspections = [],
                    TaskOrder = 1
                }
            ]
        };
        var content = new StringContent(
            JsonSerializer.Serialize(query),
            null,
            "application/json"
        );

        // Act
        const string CustomMissionsUrl = "/missions/custom";
        var responseOne = await _client.PostAsync(CustomMissionsUrl, content);
        var missionRunOne = await responseOne.Content.ReadFromJsonAsync<MissionRun>(_serializerOptions);

        var responseTwo = await _client.PostAsync(CustomMissionsUrl, content);
        var missionRunTwo = await responseTwo.Content.ReadFromJsonAsync<MissionRun>(_serializerOptions);

        var missionDefinitions = await _missionDefinitionService.ReadAll(new MissionDefinitionQueryStringParameters());

        // Assert
        Assert.Equal(missionRunOne!.MissionId, missionRunTwo!.MissionId);
        Assert.Single(missionDefinitions);
    }

    [Fact(Skip = "Reason")]
    public async Task GetNextRun()
    {
        // Arrange
        var installation = await _databaseUtilities.NewInstallation();
        var plant = await _databaseUtilities.NewPlant(installation.InstallationCode);
        var deck = await _databaseUtilities.NewDeck(installation.InstallationCode, plant.PlantCode);
        var area = await _databaseUtilities.NewArea(installation.InstallationCode, plant.PlantCode, deck.Name);
        var robot = await _databaseUtilities.NewRobot(RobotStatus.Available, installation, area);

        var query = new CustomMissionQuery
        {
            RobotId = robot.Id,
            InstallationCode = installation.InstallationCode,
            AreaName = area.Name,
            DesiredStartTime = DateTime.UtcNow,
            InspectionFrequency = new TimeSpan(14, 0, 0, 0),
            Name = "TestMission",
            Tasks = [
                new CustomTaskQuery
                {
                    RobotPose = new Pose(),
                    Inspections = [],
                    TaskOrder = 0
                }
            ]
        };
        var content = new StringContent(
            JsonSerializer.Serialize(query),
            null,
            "application/json"
        );

        const string CustomMissionsUrl = "/missions/custom";
        var response = await _client.PostAsync(CustomMissionsUrl, content);
        var missionRun = await response.Content.ReadFromJsonAsync<MissionRun>(_serializerOptions);

        Assert.True(missionRun!.Status == MissionStatus.Pending);

        var scheduleQuery = new ScheduleMissionQuery
        {
            RobotId = robot.Id,
            DesiredStartTime = DateTime.UtcNow,
        };
        var scheduleContent = new StringContent(
            JsonSerializer.Serialize(scheduleQuery),
            null,
            "application/json"
        );

        string scheduleMissionsUrl = $"/missions/schedule/{missionRun.MissionId}";

        var missionRunOneResponse = await _client.PostAsync(scheduleMissionsUrl, scheduleContent);
        var missionRunOne = await missionRunOneResponse.Content.ReadFromJsonAsync<MissionRun>(_serializerOptions);

        var missionRunTwoResponse = await _client.PostAsync(scheduleMissionsUrl, scheduleContent);
        var missionRunTwo = await missionRunTwoResponse.Content.ReadFromJsonAsync<MissionRun>(_serializerOptions);

        var missionRunThreeResponse = await _client.PostAsync(scheduleMissionsUrl, scheduleContent);
        var missionRunThree = await missionRunThreeResponse.Content.ReadFromJsonAsync<MissionRun>(_serializerOptions);

        // Act
        string nextMissionUrl = $"missions/definitions/{missionRun.MissionId}/next-run";
        var nextMissionResponse = await _client.GetAsync(nextMissionUrl);

        // Assert
        var nextMissionRun = await nextMissionResponse.Content.ReadFromJsonAsync<MissionRun>(_serializerOptions);
        Assert.NotNull(nextMissionRun);
        Assert.Equal(missionRunOne!.MissionId, missionRun.MissionId);
        Assert.Equal(missionRunTwo!.MissionId, missionRun.MissionId);
        Assert.Equal(missionRunThree!.MissionId, missionRun.MissionId);
        Assert.True(nextMissionRun.Id == missionRunTwo.Id);
    }

    [Fact(Skip = "This one might be causing issues")]
    public async Task MissionDoesNotStartIfRobotIsNotInSameInstallationAsMission()
    {
        // Arrange
        var installationOne = await _databaseUtilities.NewInstallation(name: "instOne", installationCode: "one");
        var installationTwo = await _databaseUtilities.NewInstallation(name: "instTwo", installationCode: "two");
        var plant = await _databaseUtilities.NewPlant(installationOne.InstallationCode);
        var deck = await _databaseUtilities.NewDeck(installationOne.InstallationCode, plant.PlantCode);
        var area = await _databaseUtilities.NewArea(installationOne.InstallationCode, plant.PlantCode, deck.Name);
        var robot = await _databaseUtilities.NewRobot(RobotStatus.Available, installationTwo, area);

        var query = new CustomMissionQuery
        {
            RobotId = robot.Id,
            InstallationCode = installationOne.InstallationCode,
            AreaName = area.Name,
            DesiredStartTime = DateTime.UtcNow,
            InspectionFrequency = new TimeSpan(14, 0, 0, 0),
            Name = "TestMission",
            Tasks = [
                new CustomTaskQuery
                {
                    RobotPose = new Pose(),
                    Inspections = [],
                    TaskOrder = 0
                }
            ]
        };
        var content = new StringContent(
            JsonSerializer.Serialize(query),
            null,
            "application/json"
        );

        // Act
        const string CustomMissionsUrl = "/missions/custom";
        var response = await _client.PostAsync(CustomMissionsUrl, content);

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.Conflict);
    }

    [Fact(Skip = "Also causing issues")]
    public async Task MissionFailsIfRobotIsNotInSameDeckAsMission()
    {
        // Arrange
        var installation = await _databaseUtilities.NewInstallation();
        var plant = await _databaseUtilities.NewPlant(installation.InstallationCode);
        var deckOne = await _databaseUtilities.NewDeck(installation.InstallationCode, plant.PlantCode, name: "deckOne");
        var deckTwo = await _databaseUtilities.NewDeck(installation.InstallationCode, plant.PlantCode, name: "deckTwo");
        var areaOne = await _databaseUtilities.NewArea(installation.InstallationCode, plant.PlantCode, deckOne.Name, name: "areaOne");
        var areaTwo = await _databaseUtilities.NewArea(installation.InstallationCode, plant.PlantCode, deckTwo.Name, name: "areaTwo");
        var robot = await _databaseUtilities.NewRobot(RobotStatus.Available, installation, areaOne);

        var query = new CustomMissionQuery
        {
            RobotId = robot.Id,
            InstallationCode = installation.InstallationCode,
            AreaName = areaTwo.Name,
            DesiredStartTime = DateTime.UtcNow,
            InspectionFrequency = new TimeSpan(14, 0, 0, 0),
            Name = "TestMission",
            Tasks = [
                new CustomTaskQuery
                {
                    RobotPose = new Pose(),
                    Inspections = [],
                    TaskOrder = 0
                }
            ]
        };
        var content = new StringContent(
            JsonSerializer.Serialize(query),
            null,
            "application/json"
        );

        // Act
        string customMissionsUrl = "/missions/custom";
        var missionResponse = await _client.PostAsync(customMissionsUrl, content);
        var missionRun = await missionResponse.Content.ReadFromJsonAsync<MissionRun>(_serializerOptions);
        await Task.Delay(2000);

        string missionRunByIdUrl = $"/missions/runs/{missionRun!.Id}";
        var missionByIdResponse = await _client.GetAsync(missionRunByIdUrl);
        var missionRunAfterUpdate = await missionByIdResponse.Content.ReadFromJsonAsync<MissionRun>(_serializerOptions);

        // Assert
        Assert.True(missionRunAfterUpdate!.Status == MissionStatus.Cancelled);
    }
}
