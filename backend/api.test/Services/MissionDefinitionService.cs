using System;
using System.Linq;
using System.Threading.Tasks;
using Api.Controllers.Models;
using Api.Database.Context;
using Api.Database.Models;
using Api.Services;
using Api.Test.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Npgsql;
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
        private string _connectionString = null!;

        public async ValueTask InitializeAsync()
        {
            (var container, string connectionString, var connection) =
                await TestSetupHelpers.ConfigurePostgreSqlDatabase();
            _connectionString = connectionString;
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
        [InlineData(true, false, false)]
        [InlineData(false, false, false)]
        [InlineData(true, true, false)]
        [InlineData(false, true, false)]
        [InlineData(true, false, true)]
        [InlineData(false, false, true)]
        [InlineData(true, true, true)]
        [InlineData(false, true, true)]
        public async Task ReadByIdSeparatesAccessFromTracking(
            bool readOnly,
            bool canWrite,
            bool forUpdate
        )
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
            await using var context = TestSetupHelpers.ConfigurePostgreSqlContext(
                _connectionString
            );
            var service = ActivatorUtilities.CreateInstance<Api.Services.MissionDefinitionService>(
                _services,
                context,
                roles.Object
            );
            context.ChangeTracker.Clear();
            await using var transaction = forUpdate
                ? await context.Database.BeginTransactionAsync(
                    TestContext.Current.CancellationToken
                )
                : null;

            var readable = await service.ReadById(definition.Id, readOnly, forUpdate: forUpdate);
            Assert.NotNull(readable);
            Assert.Equal(!readOnly, context.ChangeTracker.Entries<MissionDefinition>().Any());
            context.ChangeTracker.Clear();

            var writable = await service.ReadById(
                definition.Id,
                readOnly,
                AccessMode.Write,
                forUpdate
            );
            Assert.Equal(canWrite, writable != null);
            Assert.Equal(
                canWrite && !readOnly,
                context.ChangeTracker.Entries<MissionDefinition>().Any()
            );
            roles.VerifyAll();
        }

        [Fact]
        public async Task ReadByIdForUpdateRequiresAnActiveTransaction()
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                MissionDefinitionService.ReadById("missing", forUpdate: true)
            );
            Assert.Equal(
                "Reading a mission definition for update requires an active transaction.",
                exception.Message
            );
        }

        [Theory]
        [InlineData("commit")]
        [InlineData("rollback")]
        [InlineData("dispose")]
        public async Task ReadByIdForUpdateLocksUntilTransactionEnds(string completion)
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
            await using var ownerContext = TestSetupHelpers.ConfigurePostgreSqlContext(
                _connectionString
            );
            await using var contenderContext = TestSetupHelpers.ConfigurePostgreSqlContext(
                _connectionString
            );
            var owner = ActivatorUtilities.CreateInstance<Api.Services.MissionDefinitionService>(
                _services,
                ownerContext
            );
            var contender =
                ActivatorUtilities.CreateInstance<Api.Services.MissionDefinitionService>(
                    _services,
                    contenderContext
                );
            await using var ownerTransaction = await ownerContext.Database.BeginTransactionAsync(
                TestContext.Current.CancellationToken
            );
            Assert.NotNull(await owner.ReadById(definition.Id, forUpdate: true));

            await using (
                var contenderTransaction = await contenderContext.Database.BeginTransactionAsync(
                    TestContext.Current.CancellationToken
                )
            )
            {
                await contenderContext.Database.ExecuteSqlRawAsync(
                    "SET LOCAL lock_timeout = '250ms'",
                    TestContext.Current.CancellationToken
                );
                // Tracking and write access alone do not acquire a row lock.
                Assert.NotNull(await contender.ReadById(definition.Id, false, AccessMode.Write));
                contenderContext.ChangeTracker.Clear();

                var roles = new Mock<IAccessRoleService>(MockBehavior.Strict);
                roles.Setup(r => r.GetAllowedInstallationCodes(AccessMode.Read)).ReturnsAsync([]);
                var denied =
                    ActivatorUtilities.CreateInstance<Api.Services.MissionDefinitionService>(
                        _services,
                        contenderContext,
                        roles.Object
                    );
                Assert.Null(await denied.ReadById(definition.Id, forUpdate: true));
                Assert.Null(await contender.ReadById("missing", forUpdate: true));

                var exception = await Assert.ThrowsAsync<PostgresException>(() =>
                    contender.ReadById(definition.Id, forUpdate: true)
                );
                Assert.Equal(PostgresErrorCodes.LockNotAvailable, exception.SqlState);
            }

            switch (completion)
            {
                case "commit":
                    await ownerTransaction.CommitAsync(TestContext.Current.CancellationToken);
                    break;
                case "rollback":
                    await ownerTransaction.RollbackAsync(TestContext.Current.CancellationToken);
                    break;
                default:
                    await ownerTransaction.DisposeAsync();
                    break;
            }

            await using var releasedTransaction =
                await contenderContext.Database.BeginTransactionAsync(
                    TestContext.Current.CancellationToken
                );
            await contenderContext.Database.ExecuteSqlRawAsync(
                "SET LOCAL lock_timeout = '250ms'",
                TestContext.Current.CancellationToken
            );
            Assert.NotNull(await contender.ReadById(definition.Id, forUpdate: true));
        }
    }
}
