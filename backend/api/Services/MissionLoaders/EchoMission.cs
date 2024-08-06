# nullable disable
namespace Api.Services.MissionLoaders
{
    public class EchoMission
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string InstallationCode { get; set; }

        public Uri URL { get; set; }

        public virtual IList<EchoTag> Tags { get; set; }
    }
}
