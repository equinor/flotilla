namespace Api.Controllers.Models
{
    public struct CreateDeckQuery
    {
        public string AssetCode { get; set; }
        public string InstallationCode { get; set; }
        public string Name { get; set; }
    }
}
