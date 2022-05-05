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
}
