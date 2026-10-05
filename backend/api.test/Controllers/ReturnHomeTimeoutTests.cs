using System;
using System.Data.Common;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Api.Database.Models;
using Api.Services;
using Api.Test.Database;
using Api.Test.Mocks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Abstractions;
using Moq;
using Testcontainers.PostgreSql;
using Xunit;

namespace Api.Test.Controllers
{
    public class ReturnHomeTimeoutTests : IAsyncLifetime
    {
        private readonly Mock<IDownstreamApi> _downstreamApi = new();
        private TestWebApplicationFactory<Program> _factory = null!;
        private WebApplicationFactory<Program> _app = null!;
        private PostgreSqlContainer _container = null!;
        private DbConnection _connection = null!;
        private HttpClient _client = null!;
        private Robot _robot = null!;
        private HttpStatusCode _isarStatus = HttpStatusCode.OK;
        private Exception? _isarFailure;
        private int _calls;
        private string? _serviceName;
        private DownstreamApiOptions? _options;
        private string? _body;
        private string? _contentType;
        private CancellationToken _cancellationToken;

        public async ValueTask InitializeAsync()
        {
            (_container, string connectionString, _connection) =
                await TestSetupHelpers.ConfigurePostgreSqlDatabase();
            _downstreamApi
                .Setup(api =>
                    api.CallApiForAppAsync(
                        It.IsAny<string>(),
                        It.IsAny<Action<DownstreamApiOptions>>(),
                        It.IsAny<HttpContent>(),
                        It.IsAny<CancellationToken>()
                    )
                )
                .Returns(
                    async (
                        string serviceName,
                        Action<DownstreamApiOptions> configure,
                        HttpContent content,
                        CancellationToken cancellationToken
                    ) =>
                    {
                        _calls++;
                        _serviceName = serviceName;
                        _options = new DownstreamApiOptions();
                        configure(_options);
                        _cancellationToken = cancellationToken;
                        if (_isarFailure is not null)
                            throw _isarFailure;
                        _body = await content.ReadAsStringAsync(cancellationToken);
                        _contentType = content.Headers.ContentType?.MediaType;
                        return new HttpResponseMessage(_isarStatus)
                        {
                            Content = new StringContent("null", null, "application/json"),
                        };
                    }
                );

            _factory = TestSetupHelpers.ConfigureWebApplicationFactory(connectionString);
            _app = _factory.WithWebHostBuilder(builder =>
                builder.ConfigureTestServices(services =>
                {
                    services.AddScoped<IIsarService, IsarService>();
                    services.AddSingleton(_downstreamApi.Object);
                })
            );
            _client = _app.CreateClient();
            var database = _app.Services.GetRequiredService<DatabaseUtilities>();
            var installation = await database.NewInstallation("BBB");
            _robot = await database.NewRobot(RobotStatus.Available, installation);
        }

        public async ValueTask DisposeAsync()
        {
            _client?.Dispose();
            if (_app is not null)
                await _app.DisposeAsync();
            if (_factory is not null)
                await _factory.DisposeAsync();
            if (_connection is not null)
                await _connection.DisposeAsync();
            if (_container is not null)
                await _container.DisposeAsync();
            GC.SuppressFinalize(this);
        }

        [Fact]
        public async Task PostsExactIsarRequestWithoutChangingRobot()
        {
            using var response = await Post("{\"seconds\":60}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(
                "",
                await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)
            );
            Assert.Equal(1, _calls);
            Assert.Equal(IsarService.ServiceName, _serviceName);
            Assert.Equal("POST", _options?.HttpMethod);
            Assert.Equal(_robot.IsarUri, _options?.BaseUrl);
            Assert.Equal("schedule/set-return-home-timeout", _options?.RelativePath);
            Assert.Equal("{\"seconds\":60}", _body);
            Assert.Equal("application/json", _contentType);
            Assert.True(_cancellationToken.CanBeCanceled);
            var robotService = _app.Services.GetRequiredService<IRobotService>();
            var robot = await robotService.ReadById(_robot.Id);
            Assert.Equal(RobotStatus.Available, robot?.Status);
        }

        [Fact]
        public async Task RejectsInvalidInputWithoutCallingIsar()
        {
            string[] invalidBodies =
            [
                "{}",
                "{\"seconds\":0}",
                "{\"seconds\":-1}",
                "{\"seconds\":1.5}",
                "{\"seconds\":\"60\"}",
            ];
            foreach (string body in invalidBodies)
            {
                using var response = await Post(body);
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            }
            Assert.Equal(0, _calls);
        }

        [Fact]
        public async Task UnknownRobotDoesNotCallIsar()
        {
            using var response = await Post("{\"seconds\":60}", "unknown-robot");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal(0, _calls);
        }

        [Fact]
        public async Task RobotOutsideAccessibleInstallationDoesNotCallIsar()
        {
            var accessor = (MockHttpContextAccessor)
                _app.Services.GetRequiredService<IHttpContextAccessor>();
            accessor.SetHttpContextRoles(["Role.User.NON"]);

            using var response = await Post("{\"seconds\":60}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal(0, _calls);
        }

        [Theory]
        [InlineData(RoleAccessLevel.READ_ONLY, "Role.ReadOnly.BBB", HttpStatusCode.NotFound, 0)]
        [InlineData(RoleAccessLevel.USER, "Role.User.BBB", HttpStatusCode.NoContent, 1)]
        public async Task RequiresInstallationWriteAccess(
            RoleAccessLevel accessLevel,
            string installationRole,
            HttpStatusCode expectedStatus,
            int expectedCalls
        )
        {
            var accessRoles = _app.Services.GetRequiredService<IAccessRoleService>();
            await accessRoles.Create(_robot.CurrentInstallation, installationRole, accessLevel);
            var accessor = (MockHttpContextAccessor)
                _app.Services.GetRequiredService<IHttpContextAccessor>();
            accessor.SetHttpContextRoles(["Role.User", installationRole]);

            using var response = await Post("{\"seconds\":60}");

            Assert.Equal(expectedStatus, response.StatusCode);
            Assert.Equal(expectedCalls, _calls);
        }

        [Theory]
        [InlineData(HttpStatusCode.Conflict, HttpStatusCode.Conflict)]
        [InlineData(HttpStatusCode.UnprocessableEntity, HttpStatusCode.InternalServerError)]
        [InlineData(HttpStatusCode.NotFound, HttpStatusCode.InternalServerError)]
        public async Task DoesNotHideIsarFailures(
            HttpStatusCode isarStatus,
            HttpStatusCode expected
        )
        {
            _isarStatus = isarStatus;

            using var response = await Post("{\"seconds\":60}");

            Assert.Equal(expected, response.StatusCode);
            Assert.Equal(1, _calls);
        }

        [Fact]
        public async Task ConnectionFailureIsNotReportedAsSuccess()
        {
            _isarFailure = new HttpRequestException("ISAR connection failed");

            using var response = await Post("{\"seconds\":60}");

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            Assert.Equal(1, _calls);
        }

        private Task<HttpResponseMessage> Post(string body, string? robotId = null)
        {
            return _client.PostAsync(
                $"/return-to-home/set-return-home-timeout/{robotId ?? _robot.Id}",
                new StringContent(body, null, "application/json"),
                TestContext.Current.CancellationToken
            );
        }
    }
}
