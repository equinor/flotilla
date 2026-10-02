namespace Api.Services.Models
{
    public struct MediaConfig
    {
        public string? Url { get; set; }
        public string? Token { get; set; }
        public string? RobotId { get; set; }
        public MediaConnectionType MediaConnectionType { get; set; }
        public List<MediaStream>? Streams { get; set; }
    }

    public record MediaStream(string CameraId, string Url, int RotationDegrees = 0);

    public enum MediaConnectionType
    {
        LiveKit,
        OvenMediaEngine,
    }
}
