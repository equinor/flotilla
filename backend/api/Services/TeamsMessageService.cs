using Api.Services.Events;

namespace Api.Services
{
    public interface ITeamsMessageService
    {
        public void TriggerTeamsMessageReceived(TeamsMessageEventArgs e);
    }

    public class TeamsMessageService : ITeamsMessageService
    {
        public static event EventHandler<TeamsMessageEventArgs>? TeamsMessage;

        protected virtual void OnTeamsMessageReceived(TeamsMessageEventArgs e)
        {
            TeamsMessage?.Invoke(this, e);
        }

        public void TriggerTeamsMessageReceived(TeamsMessageEventArgs e)
        {
            OnTeamsMessageReceived(e);
        }
    }
}
