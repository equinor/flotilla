using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Api.Database.Context;
using Api.Services;
using Api.Test.Database;
using Api.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Abstractions;
using Moq;
using Testcontainers.PostgreSql;
using Xunit;

namespace Api.Test.Services
{
    public class EchoServiceTest : IAsyncLifetime
    {
        public required DatabaseUtilities DatabaseUtilities;
        public required PostgreSqlContainer Container;
        public required FlotillaDbContext Context;

        public async Task InitializeAsync()
        {
            (Container, string connectionString, var connection) =
                await TestSetupHelpers.ConfigurePostgreSqlDatabase();

            Context = TestSetupHelpers.ConfigurePostgreSqlContext(connectionString);

            DatabaseUtilities = new DatabaseUtilities(Context);
        }

        public Task DisposeAsync() => Task.CompletedTask;

        [Fact]
        public async Task TestGetAvailableMissions_WhenServerReturns500_ThrowsException()
        {
            //Arrange
            var echoApiMock = new Mock<IDownstreamApi>();
            var logger = new Mock<ILogger<EchoService>>();
            var sourceService = new Mock<ISourceService>();
            var inspectionService = new Mock<IInspectionService>();

            var httpResponse = new HttpResponseMessage(HttpStatusCode.InternalServerError);

            echoApiMock
                .Setup(a =>
                    a.CallApiForAppAsync(
                        It.IsAny<string>(),
                        It.IsAny<Action<DownstreamApiOptions>?>(),
                        It.IsAny<HttpContent?>(),
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(httpResponse);

            var echoService = new EchoService(
                logger.Object,
                echoApiMock.Object,
                sourceService.Object,
                inspectionService.Object
            );
            var installation = await DatabaseUtilities.NewInstallation();

            //Act & Assert
            var exception = await Assert.ThrowsAsync<MissionLoaderUnavailableException>(
                async () => await echoService.GetAvailableMissions(installation.InstallationCode)
            );
            Assert.Equal(
                "Echo API unavailable. Status code: InternalServerError",
                exception.Message
            );
        }

        [Fact]
        public async Task TestGetEchoMission_WhenServerReturns500_ThrowsException()
        {
            //Arrange
            var echoApiMock = new Mock<IDownstreamApi>();
            var logger = new Mock<ILogger<EchoService>>();
            var sourceService = new Mock<ISourceService>();
            var inspectionService = new Mock<IInspectionService>();

            var httpResponse = new HttpResponseMessage(HttpStatusCode.InternalServerError);

            echoApiMock
                .Setup(a =>
                    a.CallApiForAppAsync(
                        It.IsAny<string>(),
                        It.IsAny<Action<DownstreamApiOptions>?>(),
                        It.IsAny<HttpContent?>(),
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(httpResponse);

            var echoService = new EchoService(
                logger.Object,
                echoApiMock.Object,
                sourceService.Object,
                inspectionService.Object
            );
            var dummyEchoMissionId = "1";

            //Act and Assert
            var exception = await Assert.ThrowsAsync<MissionLoaderUnavailableException>(
                async () => await echoService.GetMissionById(dummyEchoMissionId)
            );
            Assert.Equal(
                "Echo API unavailable. Status code: InternalServerError",
                exception.Message
            );
        }
    }
}
