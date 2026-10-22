using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Api.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Api.Test.Utilities
{
    public class GlobalExceptionHandlerTest
    {
        private static readonly HashSet<Type> HandledLocally =
        [
            typeof(ConfigException),
            typeof(MissionException),
            typeof(MissionStoppedException),
            typeof(MissionNotFoundException),
            typeof(MissionRunNotFoundException),
            typeof(UnsupportedRobotCapabilityException),
            typeof(PlantNotFoundException),
            typeof(InspectionNotFoundException),
            typeof(MissionTaskNotFoundException),
            typeof(InspectionAreaExistsException),
            typeof(ExclusionAreaExistsException),
            typeof(FailedToRemoveAutoSchedulingException),
        ];

        public static IEnumerable<object[]> ExceptionTypes()
        {
            return typeof(InvalidPolygonException)
                .Assembly.GetTypes()
                .Where(t =>
                    t.Namespace == "Api.Utilities"
                    && typeof(Exception).IsAssignableFrom(t)
                    && !t.IsAbstract
                )
                .Select(t => new object[] { t });
        }

        [Theory]
        [MemberData(nameof(ExceptionTypes))]
        public async Task ExceptionTypeIsEitherMappedOrExplicitlyLeftToLocalHandling(
            Type exceptionType
        )
        {
            // Arrange
            var exception = (Exception)Activator.CreateInstance(exceptionType, "test message")!;
            var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);
            var context = new DefaultHttpContext { Response = { Body = new MemoryStream() } };

            // Act
            var handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);

            // Assert
            Assert.True(handled);

            var defaultedWithoutExplicitMapping =
                context.Response.StatusCode == StatusCodes.Status500InternalServerError
                && context.Response.Body.Length == 0;

            if (defaultedWithoutExplicitMapping && !HandledLocally.Contains(exceptionType))
            {
                Assert.Fail(
                    $"{exceptionType.Name} has no explicit mapping in GlobalExceptionHandler and "
                        + "isn't in the HandledLocally allow-list. Pick a status code for it in the "
                        + "switch, or add it to HandledLocally with a one-line reason."
                );
            }
        }

        [Theory]
        [InlineData(typeof(InvalidPolygonException), StatusCodes.Status400BadRequest)]
        [InlineData(typeof(InvalidDataException), StatusCodes.Status400BadRequest)]
        [InlineData(typeof(RobotPreCheckFailedException), StatusCodes.Status400BadRequest)]
        [InlineData(typeof(MissionPauseException), StatusCodes.Status400BadRequest)]
        [InlineData(typeof(MissionResumeException), StatusCodes.Status400BadRequest)]
        [InlineData(typeof(RobotNotFoundException), StatusCodes.Status404NotFound)]
        [InlineData(typeof(InstallationNotFoundException), StatusCodes.Status404NotFound)]
        [InlineData(typeof(RobotBusyException), StatusCodes.Status409Conflict)]
        [InlineData(
            typeof(RobotNotInSameInstallationAsMissionException),
            StatusCodes.Status409Conflict
        )]
        [InlineData(typeof(TeamsNotificationException), StatusCodes.Status502BadGateway)]
        [InlineData(typeof(IsarCommunicationException), StatusCodes.Status500InternalServerError)]
        [InlineData(typeof(JsonException), StatusCodes.Status500InternalServerError)]
        [InlineData(typeof(MultipleInspectionAreasFoundException), StatusCodes.Status400BadRequest)]
        public async Task KnownExceptionTypeMapsToExpectedStatusCode(
            Type exceptionType,
            int expectedStatusCode
        )
        {
            // Arrange
            var exception = (Exception)Activator.CreateInstance(exceptionType, "test message")!;
            var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);
            var context = new DefaultHttpContext { Response = { Body = new MemoryStream() } };

            // Act
            await handler.TryHandleAsync(context, exception, CancellationToken.None);

            // Assert
            Assert.Equal(expectedStatusCode, context.Response.StatusCode);
        }

        [Fact]
        public async Task UnmappedExceptionDefaultsTo500WithEmptyBody()
        {
            // Arrange
            var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);
            var context = new DefaultHttpContext { Response = { Body = new MemoryStream() } };

            // Act
            var handled = await handler.TryHandleAsync(
                context,
                new InvalidOperationException("boom"),
                CancellationToken.None
            );

            // Assert
            Assert.True(handled);
            Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
            Assert.Equal(0, context.Response.Body.Length);
        }
    }
}
