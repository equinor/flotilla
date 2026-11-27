using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Api.Database.Models;
using Api.Services;
using Api.Utilities;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Identity.Abstractions;
using Moq;
using Xunit;

namespace Api.Test.Services
{
    public class IsarServiceTests
    {
        private const string IsarUri = "http://isar.test";

        private static IsarService CreateIsarService(HttpStatusCode statusCode)
        {
            var isarApi = new Mock<IDownstreamApi>();
            isarApi
                .Setup(api =>
                    api.CallApiForAppAsync(
                        It.IsAny<string>(),
                        It.IsAny<Action<DownstreamApiOptions>>(),
                        It.IsAny<HttpContent>(),
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(() => new HttpResponseMessage(statusCode));

            return new IsarService(isarApi.Object, NullLogger<IsarService>.Instance);
        }

        private static Robot CreateRobot() => new();

        [Fact]
        public async Task StartMissionThrowsRobotBusyExceptionWhenIsarReturnsConflict()
        {
            var isarService = CreateIsarService(HttpStatusCode.Conflict);

            await Assert.ThrowsAsync<RobotBusyException>(() =>
                isarService.StartMission(CreateRobot(), new MissionRun())
            );
        }

        [Fact]
        public async Task StartMissionThrowsMissionExceptionWhenIsarReturnsServerError()
        {
            var isarService = CreateIsarService(HttpStatusCode.InternalServerError);

            await Assert.ThrowsAsync<MissionException>(() =>
                isarService.StartMission(CreateRobot(), new MissionRun())
            );
        }

        [Fact]
        public async Task ReturnHomeDoesNotThrowWhenIsarReturnsConflict()
        {
            var isarService = CreateIsarService(HttpStatusCode.Conflict);

            var exception = await Record.ExceptionAsync(() =>
                isarService.ReturnHome(CreateRobot())
            );

            Assert.Null(exception);
        }

        [Fact]
        public async Task ReturnHomeThrowsMissionExceptionWhenIsarReturnsServerError()
        {
            var isarService = CreateIsarService(HttpStatusCode.InternalServerError);

            await Assert.ThrowsAsync<MissionException>(() =>
                isarService.ReturnHome(CreateRobot())
            );
        }

        [Fact]
        public async Task SetReturnHomeTimeoutThrowsRobotBusyExceptionWhenIsarReturnsConflict()
        {
            var isarService = CreateIsarService(HttpStatusCode.Conflict);

            await Assert.ThrowsAsync<RobotBusyException>(() =>
                isarService.SetReturnHomeTimeout(
                    CreateRobot(),
                    30,
                    TestContext.Current.CancellationToken
                )
            );
        }

        [Fact]
        public async Task SetReturnHomeTimeoutThrowsCommunicationExceptionWhenIsarReturnsServerError()
        {
            var isarService = CreateIsarService(HttpStatusCode.InternalServerError);

            await Assert.ThrowsAsync<IsarCommunicationException>(() =>
                isarService.SetReturnHomeTimeout(
                    CreateRobot(),
                    30,
                    TestContext.Current.CancellationToken
                )
            );
        }

        [Fact]
        public async Task StopMissionDoesNotThrowWhenIsarReturnsConflict()
        {
            var isarService = CreateIsarService(HttpStatusCode.Conflict);

            var exception = await Record.ExceptionAsync(() =>
                isarService.StopMission(CreateRobot())
            );

            Assert.Null(exception);
        }

        [Fact]
        public async Task StopMissionThrowsMissionStoppedExceptionWhenIsarReturnsServerError()
        {
            var isarService = CreateIsarService(HttpStatusCode.InternalServerError);

            await Assert.ThrowsAsync<MissionStoppedException>(() =>
                isarService.StopMission(CreateRobot())
            );
        }

        [Fact]
        public async Task PauseMissionDoesNotThrowWhenIsarReturnsConflict()
        {
            var isarService = CreateIsarService(HttpStatusCode.Conflict);

            var exception = await Record.ExceptionAsync(() =>
                isarService.PauseMission(CreateRobot())
            );

            Assert.Null(exception);
        }

        [Fact]
        public async Task PauseMissionThrowsMissionPauseExceptionWhenIsarReturnsServerError()
        {
            var isarService = CreateIsarService(HttpStatusCode.InternalServerError);

            await Assert.ThrowsAsync<MissionPauseException>(() =>
                isarService.PauseMission(CreateRobot())
            );
        }

        [Fact]
        public async Task ResumeMissionDoesNotThrowWhenIsarReturnsConflict()
        {
            var isarService = CreateIsarService(HttpStatusCode.Conflict);

            var exception = await Record.ExceptionAsync(() =>
                isarService.ResumeMission(CreateRobot())
            );

            Assert.Null(exception);
        }

        [Fact]
        public async Task ResumeMissionThrowsMissionResumeExceptionWhenIsarReturnsServerError()
        {
            var isarService = CreateIsarService(HttpStatusCode.InternalServerError);

            await Assert.ThrowsAsync<MissionResumeException>(() =>
                isarService.ResumeMission(CreateRobot())
            );
        }

        [Fact]
        public async Task ReleaseInterventionNeededDoesNotThrowWhenIsarReturnsConflict()
        {
            var isarService = CreateIsarService(HttpStatusCode.Conflict);

            var exception = await Record.ExceptionAsync(() =>
                isarService.ReleaseInterventionNeeded(IsarUri)
            );

            Assert.Null(exception);
        }

        [Fact]
        public async Task ReleaseInterventionNeededThrowsCommunicationExceptionWhenIsarReturnsServerError()
        {
            var isarService = CreateIsarService(HttpStatusCode.InternalServerError);

            await Assert.ThrowsAsync<IsarCommunicationException>(() =>
                isarService.ReleaseInterventionNeeded(IsarUri)
            );
        }

        [Fact]
        public async Task SendToLockdownDoesNotThrowWhenIsarReturnsConflict()
        {
            var isarService = CreateIsarService(HttpStatusCode.Conflict);

            var exception = await Record.ExceptionAsync(() =>
                isarService.SendToLockdown(IsarUri)
            );

            Assert.Null(exception);
        }

        [Fact]
        public async Task SendToLockdownThrowsCommunicationExceptionWhenIsarReturnsServerError()
        {
            var isarService = CreateIsarService(HttpStatusCode.InternalServerError);

            await Assert.ThrowsAsync<IsarCommunicationException>(() =>
                isarService.SendToLockdown(IsarUri)
            );
        }

        [Fact]
        public async Task ReleaseFromLockdownDoesNotThrowWhenIsarReturnsConflict()
        {
            var isarService = CreateIsarService(HttpStatusCode.Conflict);

            var exception = await Record.ExceptionAsync(() =>
                isarService.ReleaseFromLockdown(IsarUri)
            );

            Assert.Null(exception);
        }

        [Fact]
        public async Task ReleaseFromLockdownThrowsCommunicationExceptionWhenIsarReturnsServerError()
        {
            var isarService = CreateIsarService(HttpStatusCode.InternalServerError);

            await Assert.ThrowsAsync<IsarCommunicationException>(() =>
                isarService.ReleaseFromLockdown(IsarUri)
            );
        }

        [Fact]
        public async Task SetMaintenanceModeThrowsConflictExceptionWhenIsarReturnsConflict()
        {
            var isarService = CreateIsarService(HttpStatusCode.Conflict);

            await Assert.ThrowsAsync<IsarConflictException>(() =>
                isarService.SetMaintenanceMode(IsarUri)
            );
        }

        [Fact]
        public async Task ReleaseMaintenanceModeThrowsConflictExceptionWhenIsarReturnsConflict()
        {
            var isarService = CreateIsarService(HttpStatusCode.Conflict);

            await Assert.ThrowsAsync<IsarConflictException>(() =>
                isarService.ReleaseMaintenanceMode(IsarUri)
            );
        }

        [Fact]
        public async Task SetMaintenanceModeThrowsCommunicationExceptionWhenIsarReturnsServerError()
        {
            var isarService = CreateIsarService(HttpStatusCode.InternalServerError);

            await Assert.ThrowsAsync<IsarCommunicationException>(() =>
                isarService.SetMaintenanceMode(IsarUri)
            );
        }

        [Fact]
        public async Task ReleaseMaintenanceModeThrowsCommunicationExceptionWhenIsarReturnsServerError()
        {
            var isarService = CreateIsarService(HttpStatusCode.InternalServerError);

            await Assert.ThrowsAsync<IsarCommunicationException>(() =>
                isarService.ReleaseMaintenanceMode(IsarUri)
            );
        }

        [Fact]
        public async Task GetMediaStreamConfigThrowsConfigExceptionWhenIsarReturnsConflict()
        {
            var isarService = CreateIsarService(HttpStatusCode.Conflict);

            await Assert.ThrowsAsync<ConfigException>(() =>
                isarService.GetMediaStreamConfig(CreateRobot())
            );
        }

        [Fact]
        public async Task GetMediaStreamConfigThrowsConfigExceptionWhenIsarReturnsServerError()
        {
            var isarService = CreateIsarService(HttpStatusCode.InternalServerError);

            await Assert.ThrowsAsync<ConfigException>(() =>
                isarService.GetMediaStreamConfig(CreateRobot())
            );
        }
    }
}
