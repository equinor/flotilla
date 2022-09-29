namespace Api.Utilities
{
    public class MissionException : Exception
    {
        public MissionException(string message) : base(message) { }
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
