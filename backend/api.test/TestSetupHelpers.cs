using System;
using System.Data.Common;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Api.Database.Context;
using Api.Services;
using Api.Test.Database;
using Api.Test.Mocks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Testcontainers.PostgreSql;
using Xunit.Sdk;

namespace Api.Test;

public static class TestSetupHelpers
{
    private const string PostgresVersion = "postgres:17.10";

    public static async Task<(
        PostgreSqlContainer,
        string,
        DbConnection
    )> ConfigurePostgreSqlDatabase()
    {
        var container = new PostgreSqlBuilder(PostgresVersion).Build();
        await container.StartAsync();

        string? connectionString = container.GetConnectionString();
        var context = ConfigurePostgreSqlContext(connectionString);
        await context.Database.MigrateAsync();

        var connection = context.Database.GetDbConnection();
        await connection.OpenAsync();

        return (container, connectionString, connection);
    }

    public static JsonSerializerOptions ConfigureJsonSerializerOptions()
    {
        return new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter() },
            PropertyNameCaseInsensitive = true,
        };
    }

    public static IServiceProvider ConfigureServiceProvider(
        TestWebApplicationFactory<Program> factory
    )
    {
        return factory.Services;
    }

    public static FlotillaDbContext ConfigurePostgreSqlContext(string connectionString)
    {
        var optionsBuilder = new DbContextOptionsBuilder<FlotillaDbContext>();
        optionsBuilder.UseNpgsql(
            connectionString,
            o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery)
        );
        var context = new FlotillaDbContext(optionsBuilder.Options);
        return context;
    }

    public static TestWebApplicationFactory<Program> ConfigureWebApplicationFactory(
        string? postgreSqlConnectionString = null
    )
    {
        return new TestWebApplicationFactory<Program>(postgreSqlConnectionString);
    }

    public static HttpClient ConfigureHttpClient(TestWebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                BaseAddress = new Uri("http://localhost:8000"),
            }
        );
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            TestAuthHandler.AuthenticationScheme
        );

        return client;
    }

    public static UnauthenticatedWebApplicationFactory<Program> ConfigureUnauthenticatedWebApplicationFactory(
        string? postgreSqlConnectionString = null
    )
    {
        return new UnauthenticatedWebApplicationFactory<Program>(postgreSqlConnectionString);
    }

    public static HttpClient ConfigureUnauthenticatedHttpClient(
        UnauthenticatedWebApplicationFactory<Program> factory
    )
    {
        var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                BaseAddress = new Uri("http://localhost:8000"),
            }
        );

        return client;
    }

    /// <summary>
    /// Create an instance of DatabaseUutilities. It should only be used when the Application Factory is not used.
    /// It is called Isolated since it creates a new instance of all services.
    /// </summary>
    public static DatabaseUtilities CreateIsolatedDatabaseUtilities(FlotillaDbContext context)
    {
        var _accessRoleService = new AccessRoleService(context, new HttpContextAccessor());
        var _installationService = new InstallationService(
            context,
            _accessRoleService,
            new Mock<ILogger<InstallationService>>().Object
        );
        var _missionTaskService = new MissionTaskService(
            context,
            new Mock<ILogger<MissionTaskService>>().Object
        );
        var _plantService = new PlantService(context, _installationService, _accessRoleService);
        var _areaPolygonService = new AreaPolygonService(
            new Mock<ILogger<AreaPolygonService>>().Object
        );
        var signalRService = new MockSignalRService();
        var _inspectionAreaService = new InspectionAreaService(
            context,
            _installationService,
            _plantService,
            _accessRoleService,
            signalRService,
            _areaPolygonService,
            new Mock<ILogger<InspectionAreaService>>().Object
        );
        var _userInfoService = new UserInfoService(
            context,
            new HttpContextAccessor(),
            new Mock<ILogger<UserInfoService>>().Object
        );
        var _robotService = new RobotService(
            context,
            new Mock<ILogger<RobotService>>().Object,
            signalRService,
            _accessRoleService,
            _installationService,
            _inspectionAreaService
        );
        var _missionRunService = new MissionRunService(
            context,
            signalRService,
            new Mock<ILogger<MissionRunService>>().Object,
            _accessRoleService,
            _missionTaskService,
            _inspectionAreaService,
            _robotService,
            _userInfoService
        );
        var _errorHandlingService = new ErrorHandlingService(
            new Mock<ILogger<ErrorHandlingService>>().Object,
            _robotService,
            _missionRunService
        );
        var _exclusionAreaService = new ExclusionAreaService(
            context,
            _installationService,
            _plantService,
            _accessRoleService,
            signalRService,
            _areaPolygonService
        );
        var _missionSchedulingService = new MissionSchedulingService(
            new Mock<ILogger<MissionSchedulingService>>().Object,
            _missionRunService,
            _robotService,
            new MockIsarService(),
            signalRService,
            _errorHandlingService,
            _inspectionAreaService,
            _exclusionAreaService
        );
        var _missionDefinitionService = new MissionDefinitionService(
            context,
            signalRService,
            _accessRoleService,
            new Mock<ILogger<MissionDefinitionService>>().Object,
            _missionRunService
        );
        var _autoScheduleService = new AutoScheduleService(
            new Mock<ILogger<AutoScheduleService>>().Object,
            _missionDefinitionService,
            _robotService,
            _missionRunService,
            _missionSchedulingService,
            signalRService
        );
        var databaseUtilities = new DatabaseUtilities(
            _missionRunService,
            _missionDefinitionService,
            _installationService,
            _plantService,
            _inspectionAreaService,
            _robotService
        );
        return databaseUtilities;
    }

    public static async Task<bool> WaitFor(
        Func<Task<bool>> workFunction,
        float waitPeriodSeconds = 5,
        float waitIncrementSeconds = 1
    )
    {
        DateTime startTime = DateTime.UtcNow;
        while (DateTime.UtcNow < startTime.AddSeconds(waitPeriodSeconds))
        {
            var isDone = await workFunction();
            if (isDone)
                return true;
            var cts = new CancellationTokenSource();
            await Task.Delay((int)(waitIncrementSeconds * 1000), cts.Token);
        }
        throw TrueException.ForNonTrueValue(
            $"Waited for something that never happened within {waitPeriodSeconds} seconds. ",
            false
        );
    }
}
