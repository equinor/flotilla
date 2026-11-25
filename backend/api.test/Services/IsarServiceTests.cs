using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
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
    }
}
