using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;

namespace Api.Utilities
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken
        )
        {
            var (statusCode, message) = exception switch
            {
                InvalidPolygonException => (StatusCodes.Status400BadRequest, "Invalid polygon"),
                InvalidDataException e => (StatusCodes.Status400BadRequest, e.Message),
                RobotPreCheckFailedException e => (StatusCodes.Status400BadRequest, e.Message),
                MissionPauseException e => (StatusCodes.Status400BadRequest, e.Message),
                MissionResumeException e => (StatusCodes.Status400BadRequest, e.Message),
                MultipleInspectionAreasFoundException e => (
                    StatusCodes.Status400BadRequest,
                    e.Message
                ),
                RobotNotFoundException e => (StatusCodes.Status404NotFound, e.Message),
                InstallationNotFoundException e => (StatusCodes.Status404NotFound, e.Message),
                RobotBusyException => (StatusCodes.Status409Conflict, null),
                RobotNotInSameInstallationAsMissionException e => (
                    StatusCodes.Status409Conflict,
                    e.Message
                ),
                TeamsNotificationException => (StatusCodes.Status502BadGateway, null),
                IsarCommunicationException e => (
                    StatusCodes.Status500InternalServerError,
                    e.Message
                ),
                JsonException e => (StatusCodes.Status500InternalServerError, e.Message),
                _ => (StatusCodes.Status500InternalServerError, (string?)null),
            };

            _logger.LogError(
                exception,
                "Unhandled {ExceptionType} for {Method} {Path}",
                exception.GetType().Name,
                httpContext.Request.Method,
                httpContext.Request.Path
            );

            httpContext.Response.StatusCode = statusCode;
            if (message is not null)
            {
                httpContext.Response.ContentType = "text/plain; charset=utf-8";
                await httpContext.Response.WriteAsync(message, cancellationToken);
            }

            return true;
        }
    }
}
