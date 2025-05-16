namespace Api.Services.Models
{
    public struct MediaConfig
    {
        public string? Url { get; set; }
        public string? Token { get; set; }
        public string? RobotId { get; set; }
        public MediaConnectionType MediaConnectionType { get; set; }
    }

    public enum MediaConnectionType
    {
        LiveKit,
    }
}
