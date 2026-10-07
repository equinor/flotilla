using System;
using System.Linq;
using System.Threading.Tasks;
using Api.Controllers.Models;
using Api.Database.Context;
using Api.Database.Models;
using Api.Services;
using Api.Test.Database;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Testcontainers.PostgreSql;
using Xunit;

namespace Api.Test.Services
{
    public class MissionDefinitionServiceTest : IAsyncLifetime
    {
        public required DatabaseUtilities DatabaseUtilities { get; set; }
        public required IMissionDefinitionService MissionDefinitionService { get; set; }
        public required IInstallationService InstallationService { get; set; }
        private IServiceProvider _services = null!;

        public async ValueTask InitializeAsync()
        {
            (var container, string connectionString, var connection) =
                await TestSetupHelpers.ConfigurePostgreSqlDatabase();
            var factory = TestSetupHelpers.ConfigureWebApplicationFactory(
                postgreSqlConnectionString: connectionString
            );
            var serviceProvider = TestSetupHelpers.ConfigureServiceProvider(factory);
            _services = serviceProvider;

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

        [Theory]
        [InlineData(true, false)]
        [InlineData(false, false)]
        [InlineData(true, true)]
        [InlineData(false, true)]
        public async Task ReadByIdSeparatesAccessFromTracking(bool readOnly, bool canWrite)
        {
            var installation = await DatabaseUtilities.NewInstallation("BBB");
            var plant = await DatabaseUtilities.NewPlant(installation.InstallationCode);
            var area = await DatabaseUtilities.NewInspectionArea(
                installation.InstallationCode,
                plant.PlantCode
            );
            var definition = await DatabaseUtilities.NewMissionDefinition(
                null,
                installation.InstallationCode,
                area,
                [
                    new TaskDefinition
                    {
                        Index = 1,
                        RobotPose = new Pose(),
                        TargetPosition = new Position(),
                    },
                ],
                writeToDatabase: true
            );
            var roles = new Mock<IAccessRoleService>(MockBehavior.Strict);
            roles.Setup(s => s.GetAllowedInstallationCodes(AccessMode.Read)).ReturnsAsync(["BBB"]);
            roles
                .Setup(s => s.GetAllowedInstallationCodes(AccessMode.Write))
                .ReturnsAsync(canWrite ? ["BBB"] : ["AAA"]);
            var context = _services.GetRequiredService<FlotillaDbContext>();
            var service = ActivatorUtilities.CreateInstance<Api.Services.MissionDefinitionService>(
                _services,
                context,
                roles.Object
            );
            context.ChangeTracker.Clear();

            var readable = await service.ReadById(definition.Id, readOnly);
            Assert.NotNull(readable);
            Assert.Equal(!readOnly, context.ChangeTracker.Entries<MissionDefinition>().Any());
            context.ChangeTracker.Clear();

            var writable = await service.ReadById(definition.Id, readOnly, AccessMode.Write);
            Assert.Equal(canWrite, writable != null);
            Assert.Equal(
                canWrite && !readOnly,
                context.ChangeTracker.Entries<MissionDefinition>().Any()
            );
            roles.VerifyAll();
        }
    }
}
