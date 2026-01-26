using System;
using System.Threading.Tasks;
using Api.Database.Context;
using Api.Database.Models;
using Api.HostedServices;
using Api.Services;
using Api.Test.Database;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Testcontainers.PostgreSql;
using Xunit;

namespace Api.Test.HostedServices
{
    public class TestAutoSchedulingHostedService : IAsyncLifetime, IAsyncDisposable
    {
        private FlotillaDbContext Context => CreateContext();
        public required TestWebApplicationFactory<Program> Factory;
        public required IServiceProvider ServiceProvider;
        public required PostgreSqlContainer Container;
        public required string ConnectionString;
        public required DatabaseUtilities DatabaseUtilities;
        public required IMissionDefinitionService MissionDefinitionService;
        public required IRobotService RobotService;
        public required IMissionSchedulingService MissionSchedulingService;
        public required ISignalRService SignalRService;

        private AutoSchedulingHostedService? _service;

        public async ValueTask InitializeAsync()
        {
            (Container, ConnectionString, var connection) =
                await TestSetupHelpers.ConfigurePostgreSqlDatabase();
            Factory = TestSetupHelpers.ConfigureWebApplicationFactory(
                postgreSqlConnectionString: ConnectionString
            );

            ServiceProvider = TestSetupHelpers.ConfigureServiceProvider(Factory);

            DatabaseUtilities = new DatabaseUtilities(Context);

            // Create a scope to resolve services
            using var scope = ServiceProvider.CreateScope();
            var scopedServiceProvider = scope.ServiceProvider;

            // Resolve the services from the scoped service provider
            MissionDefinitionService =
                scopedServiceProvider.GetRequiredService<IMissionDefinitionService>();
            RobotService = scopedServiceProvider.GetRequiredService<IRobotService>();
            MissionSchedulingService =
                scopedServiceProvider.GetRequiredService<IMissionSchedulingService>();
            SignalRService = scopedServiceProvider.GetRequiredService<ISignalRService>();

            var serviceLogger = new Mock<ILogger<AutoSchedulingHostedService>>().Object;

            // Pass the IServiceScopeFactory to the AutoSchedulingHostedService
            _service = new AutoSchedulingHostedService(
                serviceLogger,
                scopedServiceProvider.GetRequiredService<IServiceScopeFactory>()
            );
        }

        public static async ValueTask DisposeAsync()
        {
            await Task.CompletedTask;
        }

        async ValueTask IAsyncDisposable.DisposeAsync()
        {
            await Task.CompletedTask;
            GC.SuppressFinalize(this);
        }

        private FlotillaDbContext CreateContext()
        {
            return TestSetupHelpers.ConfigurePostgreSqlContext(ConnectionString);
        }

        [Fact]
        public async Task SuccessfullyScheduledAutoMission()
        {
            // Arrange
            var installation = await DatabaseUtilities.NewInstallation();
            var plant = await DatabaseUtilities.NewPlant(installation.InstallationCode);
            var inspectionArea = await DatabaseUtilities.NewInspectionArea(
                installation.InstallationCode,
                plant.PlantCode
            );
            var robot = await DatabaseUtilities.NewRobot(
                RobotStatus.Available,
                installation,
                inspectionArea.Id
            );
            var missionRun = await DatabaseUtilities.NewMissionRun(
                installation.InstallationCode,
                robot,
                inspectionArea,
                writeToDatabase: true
            );

            await DatabaseUtilities.NewMissionDefinition(
                "1",
                installation.InstallationCode,
                inspectionArea,
                missionRun,
                writeToDatabase: true
            );

            // Act
            if (_service == null)
            {
                throw new NullReferenceException("AutoSchedulingHostedService is null");
            }
            var jobDelays = await _service.TestableDoWork();

            // Assert
            Assert.True(jobDelays?.Count > 0);
        }
    }
}
