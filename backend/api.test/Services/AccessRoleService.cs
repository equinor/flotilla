using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Database.Context;
using Api.Database.Models;
using Api.Services;
using Api.Test.Database;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace Api.Test.Services;

public class AccessRoleServiceTest : IAsyncLifetime
{
    public required DatabaseUtilities DatabaseUtilities;
    public required PostgreSqlContainer Container;
    public required IAccessRoleService AccessRoleService;
    public required FlotillaDbContext Context;

    public async ValueTask InitializeAsync()
    {
        (Container, string cs, var _) = await TestSetupHelpers.ConfigurePostgreSqlDatabase();

        var factory = TestSetupHelpers.ConfigureWebApplicationFactory(
            postgreSqlConnectionString: cs
        );
        var sp = TestSetupHelpers.ConfigureServiceProvider(factory);

        Context = TestSetupHelpers.ConfigurePostgreSqlContext(cs);
        DatabaseUtilities = new DatabaseUtilities(Context);
        AccessRoleService = sp.GetRequiredService<IAccessRoleService>();

        var http = sp.GetRequiredService<IHttpContextAccessor>();
        http.HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(
                new ClaimsIdentity([new Claim(ClaimTypes.Role, "Role.Admin")], "TestAuth")
            ),
        };

        var installationHUA = await DatabaseUtilities.NewInstallation("HUA");
        await AccessRoleService.Create(
            installationHUA,
            "Role.ReadOnly.HUA",
            RoleAccessLevel.READ_ONLY
        );
        await AccessRoleService.Create(installationHUA, "Role.User.HUA", RoleAccessLevel.USER);
    }

    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task GetAllowedInstallationCodes_ReadMode_WithReadOnlyRole_ReturnsHUA()
    {
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, "Role.ReadOnly.HUA")],
            "TestAuth"
        );

        var user = new ClaimsPrincipal(identity);

        var result = await AccessRoleService.GetAllowedInstallationCodes(user, AccessMode.Read);

        Assert.Single(result);
        Assert.Equal("HUA", result[0]);
    }

    [Fact]
    public async Task GetAllowedInstallationCodes_ReadMode_WithUserRole_ReturnsHUA()
    {
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, "Role.User.HUA")],
            "TestAuth"
        );

        var user = new ClaimsPrincipal(identity);

        var result = await AccessRoleService.GetAllowedInstallationCodes(user, AccessMode.Read);

        Assert.Single(result);
        Assert.Equal("HUA", result[0]);
    }

    [Fact]
    public async Task GetAllowedInstallationCodes_WriteMode_WithUserRole_ReturnsHUA()
    {
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, "Role.User.HUA")],
            "TestAuth"
        );

        var user = new ClaimsPrincipal(identity);

        var result = await AccessRoleService.GetAllowedInstallationCodes(user, AccessMode.Write);

        Assert.Single(result);
        Assert.Equal("HUA", result[0]);
    }

    [Fact]
    public async Task GetAllowedInstallationCodes_WriteMode_WithReadOnlyRole_ReturnsEmpty()
    {
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, "Role.ReadOnly.HUA")],
            "TestAuth"
        );
        var user = new ClaimsPrincipal(identity);

        var result = await AccessRoleService.GetAllowedInstallationCodes(user, AccessMode.Write);

        Assert.Empty(result);
    }
}
