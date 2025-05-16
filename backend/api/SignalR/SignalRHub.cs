using Api.Services;
using Microsoft.AspNetCore.SignalR;

namespace Api.SignalRHubs
{
    public interface ISignalRClient { }

    public class SignalRHub(IAccessRoleService accessRoleService) : Hub<ISignalRClient>
    {
        /// <summary>
        /// Called when a new connection is made.
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            if (Context.User != null)
            {
                var roles = Context
                    .User.Claims.Where(c =>
                        c.Type.EndsWith("/role", StringComparison.CurrentCulture)
                    )
                    .Select(c => c.Value)
                    .ToList();

                var installationCodes = await accessRoleService.GetAllowedInstallationCodes(roles);

                if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Local")
                {
                    string? localDevUser = Environment.GetEnvironmentVariable("LOCAL_DEVUSERID");
                    if (localDevUser is null || localDevUser.Equals("", StringComparison.Ordinal))
                        throw new HubException(
                            "Running in development mode, but missing LOCAL_DEVUSERID value in environment"
                        );

                    await Groups.AddToGroupAsync(Context.ConnectionId, localDevUser); // This is used instead of Users.All
                    foreach (string installationCode in installationCodes)
                        await Groups.AddToGroupAsync(
                            Context.ConnectionId,
                            localDevUser + installationCode.ToUpperInvariant()
                        );
                }
                else
                {
                    foreach (string installationCode in installationCodes)
                        await Groups.AddToGroupAsync(
                            Context.ConnectionId,
                            installationCode.ToUpperInvariant()
                        );
                }
            }

            await base.OnConnectedAsync();
        }
    }
}
