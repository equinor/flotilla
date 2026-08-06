using System;
using System.Threading.Tasks;
using Api.Controllers.Models;
using Api.Database.Models;
using Api.Services;
using Api.Test.Database;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace Api.Test.Services
{
    public class MissionDefinitionServiceTest : IAsyncLifetime
    {
        public required DatabaseUtilities DatabaseUtilities { get; set; }
        public required IMissionDefinitionService MissionDefinitionService { get; set; }
        public required IInstallationService InstallationService { get; set; }

        public async ValueTask InitializeAsync()
        {
            (var container, string connectionString, var connection) =
                await TestSetupHelpers.ConfigurePostgreSqlDatabase();
            var factory = TestSetupHelpers.ConfigureWebApplicationFactory(
                postgreSqlConnectionString: connectionString
            );
            var serviceProvider = TestSetupHelpers.ConfigureServiceProvider(factory);

            DatabaseUtilities = serviceProvider.GetRequiredService<DatabaseUtilities>();
            MissionDefinitionService =
                serviceProvider.GetRequiredService<IMissionDefinitionService>();
            InstallationService = serviceProvider.GetRequiredService<IInstallationService>();
        }

        public ValueTask DisposeAsync()
        {
            GC.SuppressFinalize(this);
            return ValueTask.CompletedTask;
        }

        [Fact]
        public async Task ShallNotCreateMissionDefinitionWithoutTasks()
        {
            var installation = await DatabaseUtilities.NewInstallation();
            var plant = await DatabaseUtilities.NewPlant(installation.InstallationCode);
            var inspectionArea = await DatabaseUtilities.NewInspectionArea(
                installation.InstallationCode,
                plant.PlantCode
            );
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await MissionDefinitionService.Create(
                    new MissionDefinition
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = "Mission Without Tasks",
                        InstallationCode = installation.InstallationCode,
                        InspectionArea = inspectionArea,
                        Tasks = [],
                        LastSuccessfulRun = null,
                    }
                )
            );
        }
    }
}
