using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Api.Controllers.Models;
using Api.Database.Models;
using Api.Test;
using Api.Test.Database;
using Api.Test.Mocks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace Api.Test.Controllers
{
    public class AccessRoleControllerTests : IAsyncLifetime
    {
        private TestWebApplicationFactory<Program>? _factory;

        private DatabaseUtilities DatabaseUtilities = null!;
        private PostgreSqlContainer Container = null!;
        private HttpClient Client = null!;
        private JsonSerializerOptions SerializerOptions = null!;
        private MockHttpContextAccessor HttpContextAccessor = null!;

        public async Task InitializeAsync()
        {
            (Container, string connectionString, var _) =
                await TestSetupHelpers.ConfigurePostgreSqlDatabase();

            _factory = TestSetupHelpers.ConfigureWebApplicationFactory(
                postgreSqlConnectionString: connectionString
            );

            var serviceProvider = TestSetupHelpers.ConfigureServiceProvider(_factory);

            Client = TestSetupHelpers.ConfigureHttpClient(_factory);
            SerializerOptions = TestSetupHelpers.ConfigureJsonSerializerOptions();

            HttpContextAccessor = (MockHttpContextAccessor)
                serviceProvider.GetService<IHttpContextAccessor>()!;

            DatabaseUtilities = new DatabaseUtilities(
                TestSetupHelpers.ConfigurePostgreSqlContext(connectionString)
            );
        }

        public async Task DisposeAsync()
        {
            if (_factory is not null)
                await _factory.DisposeAsync();
            Client.Dispose();
            if (Container is not null)
                await Container.DisposeAsync();
        }

        [Fact]
        public async Task GetAccessRolesReturnsOkWhenUserIsAdmin()
        {
            // Arrange
            HttpContextAccessor.SetHttpContextRoles([Role.Admin]);

            // Act
            var response = await Client.GetAsync("/access-roles");

            // Assert
            Assert.True(response.IsSuccessStatusCode);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetAccessRolesReturnsForbiddenWhenUserIsNotAdmin()
        {
            // Arrange
            var installation = await DatabaseUtilities.NewInstallation(installationCode: "INS");

            var accessRoleQuery = new CreateAccessRoleQuery
            {
                InstallationCode = installation.InstallationCode,
                RoleName = $"Role.User.{installation.InstallationCode}",
                AccessLevel = RoleAccessLevel.USER,
            };

            var accessRoleContent = new StringContent(
                JsonSerializer.Serialize(accessRoleQuery),
                Encoding.UTF8,
                "application/json"
            );

            // Act
            var accessRoleResponse = await Client.PostAsync("/access-roles", accessRoleContent);

            // Assert
            Assert.True(accessRoleResponse.IsSuccessStatusCode);

            HttpContextAccessor.SetHttpContextRoles([$"Role.User.{installation.InstallationCode}"]);

            // Act
            var response = await Client.GetAsync("/access-roles");

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task CreateReturnsCreatedWhenUserIsAdminAndDataIsValid()
        {
            // Arrange
            var installation = await DatabaseUtilities.NewInstallation(installationCode: "INS");
            HttpContextAccessor.SetHttpContextRoles([Role.Admin]);

            var accessRoleQuery = new CreateAccessRoleQuery
            {
                InstallationCode = installation.InstallationCode,
                RoleName = $"Role.User.{installation.InstallationCode}",
                AccessLevel = RoleAccessLevel.USER,
            };

            var content = new StringContent(
                JsonSerializer.Serialize(accessRoleQuery),
                Encoding.UTF8,
                "application/json"
            );

            // Act
            var response = await Client.PostAsync("/access-roles", content);

            // Assert
            Assert.True(response.IsSuccessStatusCode);
        }

        [Fact]
        public async Task CreateReturnsForbiddenWhenUserIsNotAdmin()
        {
            // Arrange
            var installation = await DatabaseUtilities.NewInstallation(installationCode: "INS");

            var accessRoleQuery = new CreateAccessRoleQuery
            {
                InstallationCode = installation.InstallationCode,
                RoleName = $"Role.User.{installation.InstallationCode}",
                AccessLevel = RoleAccessLevel.USER,
            };

            var accessRoleContent = new StringContent(
                JsonSerializer.Serialize(accessRoleQuery),
                Encoding.UTF8,
                "application/json"
            );

            // Act
            var accessRoleResponse = await Client.PostAsync("/access-roles", accessRoleContent);

            // Assert
            Assert.True(accessRoleResponse.IsSuccessStatusCode);

            // Arrange
            HttpContextAccessor.SetHttpContextRoles([$"Role.User.{installation.InstallationCode}"]);

            // Act
            var response = await Client.GetAsync("/access-roles");

            // Assert
            Assert.False(response.IsSuccessStatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}
