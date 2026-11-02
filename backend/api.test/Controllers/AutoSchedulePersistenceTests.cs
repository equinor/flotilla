using System;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Api.Controllers.Models;
using Api.Database.Models;
using Api.HostedServices;
using Api.Services;
using Api.Test.Database;
using Api.Utilities;
using Hangfire.States;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Api.Test.Controllers
{
    public class AutoSchedulePersistenceTests
    {
        [Theory]
        [InlineData(null, true)]
        [InlineData("{}", true)]
        [InlineData("{\"23:59:59\":\"existing-job\"}", true)]
        [InlineData("{\"23:59:59\":\"existing-job\"}", false)]
        public async Task RemovingEntireScheduleSurvivesReloadAndRebuild(
            string? existingJobs,
            bool jobDeleted
        )
        {
            var (container, connectionString, connection) =
                await TestSetupHelpers.ConfigurePostgreSqlDatabase();
            await using var ownedContainer = container;
            await using var ownedConnection = connection;
            await using var factory = TestSetupHelpers.ConfigureWebApplicationFactory(
                connectionString
            );
            factory
                .BackgroundJobs.Setup(j =>
                    j.ChangeState("existing-job", It.IsAny<DeletedState>(), null)
                )
                .Returns(jobDeleted);
            using var client = TestSetupHelpers.ConfigureHttpClient(factory);
            using var scope = factory.Services.CreateScope();
            var utilities = scope.ServiceProvider.GetRequiredService<DatabaseUtilities>();
            var definitions = scope.ServiceProvider.GetRequiredService<IMissionDefinitionService>();
            var installation = await utilities.NewInstallation();
            var plant = await utilities.NewPlant(installation.InstallationCode);
            var area = await utilities.NewInspectionArea(
                installation.InstallationCode,
                plant.PlantCode
            );
            var mission = await utilities.NewMissionDefinition(
                null,
                installation.InstallationCode,
                area,
                [
                    new TaskDefinition
                    {
                        Index = 1,
                        RobotPose = new Pose(),
                        TargetPosition = new Position(),
                        SensorType = SensorType.Image,
                    },
                ]
            );
            mission.AutoScheduleFrequency = new AutoScheduleFrequency
            {
                SchedulingTimesCETperWeek =
                [
                    new TimeAndDay(TimeZoneUtilities.NowCet().DayOfWeek, TimeOnly.MaxValue),
                ],
                AutoScheduledJobs = existingJobs,
            };
            await definitions.Create(mission);

            using var response = await client.PatchAsJsonAsync(
                $"missions/definitions/{mission.Id}",
                new { schedulingTimesCETperWeek = Array.Empty<object>() },
                TestContext.Current.CancellationToken
            );
            response.EnsureSuccessStatusCode();
            var options = TestSetupHelpers.ConfigureJsonSerializerOptions();
            var returned = await response.Content.ReadFromJsonAsync<MissionDefinitionResponse>(
                options,
                TestContext.Current.CancellationToken
            );
            Assert.NotNull(returned);
            Assert.Null(returned.AutoScheduleFrequency);

            await using var context = TestSetupHelpers.ConfigurePostgreSqlContext(connectionString);
            var persisted = await context.MissionDefinitions.SingleAsync(
                m => m.Id == mission.Id,
                TestContext.Current.CancellationToken
            );
            Assert.Null(persisted.AutoScheduleFrequency);
            var reloaded = await client.GetFromJsonAsync<MissionDefinitionResponse>(
                $"missions/definitions/{mission.Id}",
                options,
                TestContext.Current.CancellationToken
            );
            Assert.NotNull(reloaded);
            Assert.Null(reloaded.AutoScheduleFrequency);

            using var hostedService = new AutoSchedulingHostedService(
                NullLogger<AutoSchedulingHostedService>.Instance,
                factory.Services.GetRequiredService<IServiceScopeFactory>()
            );
            Assert.Null(await hostedService.TestableDoWork());
            factory.BackgroundJobs.Verify(
                j => j.ChangeState("existing-job", It.IsAny<DeletedState>(), null),
                existingJobs is null or "{}" ? Times.Never() : Times.Once()
            );
            factory.BackgroundJobs.VerifyNoOtherCalls();
        }
    }
}
