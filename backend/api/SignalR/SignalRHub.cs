using Api.Services;
using Microsoft.AspNetCore.SignalR;

namespace Api.SignalRHubs
{
    public interface ISignalRClient
    {
    }

    public class SignalRHub(IAccessRoleService accessRoleService) : Hub<ISignalRClient>
    {
        /// <summary>
        /// Called when a new connection is made.
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            if (Context.User != null)
            {
                var roles = Context.User.Claims
                    .Where((c) => c.Type.EndsWith("/role", StringComparison.CurrentCulture)).Select((c) => c.Value).ToList();

                var installationCodes = await accessRoleService.GetAllowedInstallationCodes(roles);
                foreach (string installationCode in installationCodes)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, installationCode.ToUpperInvariant());
                }
            }

            await base.OnConnectedAsync();
        }
    }
}
