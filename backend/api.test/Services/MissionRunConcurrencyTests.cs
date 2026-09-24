using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Api.Controllers.Models;
using Api.Database.Context;
using Api.Database.Models;
using Api.Services;
using Api.Services.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using Testcontainers.PostgreSql;
using Xunit;
using TaskStatus = Api.Database.Models.TaskStatus;

namespace Api.Test.Services;

public class MissionRunConcurrencyTests : IAsyncLifetime
{
    private PostgreSqlContainer _container = null!;
    private string _connectionString = null!;

    public async ValueTask InitializeAsync()
    {
        (_container, _connectionString, var connection) =
            await TestSetupHelpers.ConfigurePostgreSqlDatabase();
        await connection.DisposeAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _container.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    [Theory]
    [InlineData(nameof(MissionRun.Status), MissionStatus.Successful)]
    [InlineData(nameof(MissionRun.StatusReason), "Updated reason")]
    [InlineData(nameof(MissionRun.IsDeprecated), true)]
    [InlineData(nameof(MissionRun.IsDeprecated), false)]
    public async Task PropertyUpdatePreservesConcurrentTaskCompletion(
        string propertyName,
        object value
    )
    {
        var mission = await CreateMission();
        if (propertyName == nameof(MissionRun.IsDeprecated) && value is false)
        {
            await using var setupContext = CreateContext();
            await setupContext
                .MissionRuns.Where(m => m.Id == mission.Id)
                .ExecuteUpdateAsync(
                    s => s.SetProperty(m => m.IsDeprecated, true),
                    TestContext.Current.CancellationToken
                );
        }

        MissionTask? completedTask = null;
        var interceptor = new BeforeSaveInterceptor(async () =>
        {
            await using var taskContext = CreateContext();
            completedTask = await CreateTaskService(taskContext)
                .UpdateMissionTaskStatus(mission.Tasks[0].Id, IsarTaskStatus.Successful);
            await taskContext
                .MissionRuns.Where(m => m.Id == mission.Id)
                .ExecuteUpdateAsync(
                    s => s.SetProperty(m => m.Comment, "Concurrent comment"),
                    TestContext.Current.CancellationToken
                );
        });
        await using var context = CreateContext(interceptor);
        var signalR = new Mock<ISignalRService>();
        var service = CreateService(context, signalR);

        var updated =
            propertyName == nameof(MissionRun.Status)
                ? await service.UpdateMissionRunStatus(mission.Id, (MissionStatus)value)
                : await service.UpdateMissionRunProperty(
                    mission.Id,
                    propertyName,
                    value,
                    includeDeprecated: true
                );

        Assert.NotNull(completedTask);
        AssertCompletedTask(updated, completedTask);
        Assert.Equal(value, typeof(MissionRun).GetProperty(propertyName)!.GetValue(updated));
        Assert.Equal("Concurrent comment", updated.Comment);
        if (propertyName == nameof(MissionRun.Status))
        {
            Assert.Equal(mission.StartTime, updated.StartTime);
            Assert.NotNull(updated.EndTime);
        }

        var persisted = await service.ReadById(mission.Id, includeDeprecated: true);
        Assert.NotNull(persisted);
        AssertCompletedTask(persisted, completedTask);
        Assert.Equal(value, typeof(MissionRun).GetProperty(propertyName)!.GetValue(persisted));
        Assert.Equal("Concurrent comment", persisted.Comment);
        signalR.Verify(
            s =>
                s.SendMessageAsync(
                    "Mission run updated",
                    It.IsAny<Installation>(),
                    It.Is<MissionRunResponse>(m =>
                        m.Tasks.Single().Status == TaskStatus.Successful
                        && m.Tasks.Single().EndTime == completedTask.EndTime
                    )
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task CancellationPreservesConcurrentTaskCompletion()
    {
        var mission = await CreateMission();
        MissionTask? completedTask = null;
        var interceptor = new BeforeSaveInterceptor(async () =>
        {
            await using var taskContext = CreateContext();
            completedTask = await CreateTaskService(taskContext)
                .UpdateMissionTaskStatus(mission.Tasks[0].Id, IsarTaskStatus.Successful);
        });
        await using var context = CreateContext(interceptor);
        var service = CreateService(context, new Mock<ISignalRService>());

        var updated = await service.SetMissionRunToCancelled(mission.Id);

        Assert.NotNull(completedTask);
        AssertCompletedTask(updated, completedTask);
        Assert.Equal(MissionStatus.Cancelled, updated.Status);
        Assert.NotNull(updated.EndTime);
        var persisted = await service.ReadById(mission.Id);
        Assert.NotNull(persisted);
        AssertCompletedTask(persisted, completedTask);
        Assert.Equal(MissionStatus.Cancelled, persisted.Status);
    }

    [Fact]
    public async Task RemovingExcludedTasksPreservesConcurrentCompletionOfRemainingTasks()
    {
        var mission = await CreateMission(taskCount: 2);
        MissionTask? completedTask = null;
        var interceptor = new BeforeSaveInterceptor(async () =>
        {
            await using var taskContext = CreateContext();
            completedTask = await CreateTaskService(taskContext)
                .UpdateMissionTaskStatus(mission.Tasks[0].Id, IsarTaskStatus.Successful);
        });
        await using var context = CreateContext(interceptor);
        var service = CreateService(context, new Mock<ISignalRService>());

        var updated = await service.RemoveTasks(mission.Id, [mission.Tasks[1].Id]);

        Assert.NotNull(completedTask);
        AssertCompletedTask(updated, completedTask);
        var persisted = await service.ReadById(mission.Id);
        Assert.NotNull(persisted);
        AssertCompletedTask(persisted, completedTask);
    }

    [Fact]
    public async Task FailureStillPersistsIntentionalTaskChanges()
    {
        var mission = await CreateMission(taskCount: 3);
        await using var context = CreateContext();
        var taskService = CreateTaskService(context);
        var successfulTask = await taskService.UpdateMissionTaskStatus(
            mission.Tasks[1].Id,
            IsarTaskStatus.Successful
        );
        await taskService.UpdateMissionTaskStatus(mission.Tasks[2].Id, IsarTaskStatus.NotStarted);
        var service = CreateService(context, new Mock<ISignalRService>());

        var updated = await service.SetMissionRunToFailed(mission.Id, "Lost connection");
        var persisted = await service.ReadById(mission.Id);
        Assert.NotNull(persisted);

        foreach (var result in new[] { updated, persisted })
        {
            Assert.Equal(MissionStatus.Failed, result.Status);
            Assert.Equal("Lost connection", result.StatusReason);
            Assert.NotNull(result.EndTime);
            Assert.Equal(TaskStatus.Failed, result.Tasks[0].Status);
            Assert.NotNull(result.Tasks[0].EndTime);
            Assert.Equal(TaskStatus.Successful, result.Tasks[1].Status);
            Assert.Equal(successfulTask.EndTime, result.Tasks[1].EndTime);
            Assert.Equal(TaskStatus.NotStarted, result.Tasks[2].Status);
            Assert.Null(result.Tasks[2].EndTime);
        }
    }

    private static void AssertCompletedTask(MissionRun mission, MissionTask expected)
    {
        var task = Assert.Single(mission.Tasks);
        Assert.Equal(TaskStatus.Successful, task.Status);
        Assert.Equal(expected.StartTime, task.StartTime);
        Assert.Equal(expected.EndTime, task.EndTime);
        Assert.NotNull(task.EndTime);
    }

    private async Task<MissionRun> CreateMission(int taskCount = 1)
    {
        await using var context = CreateContext();
        var utilities = TestSetupHelpers.CreateIsolatedDatabaseUtilities(context);
        var installation = await utilities.NewInstallation();
        var plant = await utilities.NewPlant(installation.InstallationCode);
        var area = await utilities.NewInspectionArea(
            installation.InstallationCode,
            plant.PlantCode
        );
        var robot = await utilities.NewRobot(RobotStatus.Busy, installation, area.Id);
        var definition = await utilities.NewMissionDefinition(
            null,
            installation.InstallationCode,
            area,
            Enumerable
                .Range(0, taskCount)
                .Select(i => new TaskDefinition
                {
                    Index = i,
                    RobotPose = new Pose(),
                    TargetPosition = new Position(),
                })
                .ToList()
        );
        var mission = await utilities.NewMissionRun(
            definition,
            robot,
            writeToDatabase: true,
            missionStatus: MissionStatus.Ongoing
        );
        foreach (var task in mission.Tasks)
        {
            await CreateTaskService(context)
                .UpdateMissionTaskStatus(task.Id, IsarTaskStatus.InProgress);
        }
        return mission;
    }

    private FlotillaDbContext CreateContext(SaveChangesInterceptor? interceptor = null)
    {
        var options = new DbContextOptionsBuilder<FlotillaDbContext>().UseNpgsql(_connectionString);
        if (interceptor != null)
            options.AddInterceptors(interceptor);
        return new FlotillaDbContext(options.Options);
    }

    private static IAccessRoleService CreateAccessRoleService()
    {
        var accessRole = new Mock<IAccessRoleService>();
        accessRole
            .Setup(s => s.GetAllowedInstallationCodes(It.IsAny<AccessMode>()))
            .ReturnsAsync(["INSTCODE"]);
        return accessRole.Object;
    }

    private static MissionTaskService CreateTaskService(FlotillaDbContext context) =>
        new(context, CreateAccessRoleService(), Mock.Of<ILogger<MissionTaskService>>());

    private static MissionRunService CreateService(
        FlotillaDbContext context,
        Mock<ISignalRService> signalR
    ) =>
        new(
            context,
            signalR.Object,
            Mock.Of<ILogger<MissionRunService>>(),
            CreateAccessRoleService(),
            CreateTaskService(context),
            Mock.Of<IInspectionAreaService>(),
            Mock.Of<IRobotService>(),
            Mock.Of<IUserInfoService>()
        );

    private sealed class BeforeSaveInterceptor(Func<Task> beforeSave) : SaveChangesInterceptor
    {
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default
        )
        {
            await beforeSave();
            return result;
        }
    }
}
