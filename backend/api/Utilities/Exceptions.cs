namespace Api.Utilities
{
    public class MissionException : Exception
    {
        public int IsarStatusCode { get; set; }
        public MissionException(string message) : base(message) { }

        public MissionException(string message, int isarStatusCode) : base(message)
        {
            IsarStatusCode = isarStatusCode;
        }
    }

    public class MissionSourceTypeException : Exception
    {
        public MissionSourceTypeException(string message) : base(message) { }
    }

    public class AssetNotFoundException : Exception
    {
        public AssetNotFoundException(string message) : base(message) { }
    }

    public class InstallationNotFoundException : Exception
    {
        public InstallationNotFoundException(string message) : base(message) { }
    }

    public class DeckNotFoundException : Exception
    {
        public DeckNotFoundException(string message) : base(message) { }
    }

    public class AreaNotFoundException : Exception
    {
        public AreaNotFoundException(string message) : base(message) { }
    }

    public class MissionNotFoundException : Exception
    {
        public MissionNotFoundException(string message) : base(message) { }
    }

    public class RobotPositionNotFoundException : Exception
    {
        public RobotPositionNotFoundException(string message) : base(message) { }
    }

    public class TagPositionNotFoundException : Exception
    {
        public TagPositionNotFoundException(string message) : base(message) { }
    }
}
