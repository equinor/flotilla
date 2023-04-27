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
