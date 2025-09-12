namespace Api.Utilities
{
    public class ConfigException(string message) : Exception(message) { }

    public class MissionException : Exception
    {
        public MissionException(string message)
            : base(message) { }

        public MissionException(string message, int isarStatusCode)
            : base(message)
        {
            IsarStatusCode = isarStatusCode;
        }

        public int IsarStatusCode { get; set; }
    }

    public class MissionStoppedException : Exception
    {
        public MissionStoppedException(string message)
            : base(message) { }

        public MissionStoppedException(string message, int isarStatusCode)
            : base(message)
        {
            IsarStatusCode = isarStatusCode;
        }

        public int IsarStatusCode { get; set; }
    }

    public class MissionPauseException : Exception
    {
        public MissionPauseException(string message)
            : base(message) { }

        public MissionPauseException(string message, int isarStatusCode)
            : base(message)
        {
            IsarStatusCode = isarStatusCode;
        }

        public int IsarStatusCode { get; set; }
    }

    public class MissionResumeException : Exception
    {
        public MissionResumeException(string message)
            : base(message) { }

        public MissionResumeException(string message, int isarStatusCode)
            : base(message)
        {
            IsarStatusCode = isarStatusCode;
        }

        public int IsarStatusCode { get; set; }
    }

    public class MissionArmPositionException : Exception
    {
        public MissionArmPositionException(string message)
            : base(message) { }

        public MissionArmPositionException(string message, int isarStatusCode)
            : base(message)
        {
            IsarStatusCode = isarStatusCode;
        }

        public int IsarStatusCode { get; set; }
    }



    public class SourceException(string message) : Exception(message) { }

    public class InstallationNotFoundException(string message) : Exception(message) { }

    public class PlantNotFoundException(string message) : Exception(message) { }

    public class MissionNotFoundException(string message) : Exception(message) { }

    public class InspectionNotFoundException(string message) : Exception(message) { }

    public class InspectionNotAvailableYetException(string message) : Exception(message) { }

    public class MissionTaskNotFoundException(string message) : Exception(message) { }

    public class MissionRunNotFoundException(string message) : Exception(message) { }

    public class RobotNotFoundException(string message) : Exception(message) { }

    public class RobotInformationNotAvailableException(string message) : Exception(message) { }

    public class RobotPreCheckFailedException(string message) : Exception(message) { }

    public class InspectionAreaExistsException(string message) : Exception(message) { }

    public class ExclusionAreaExistsException(string message) : Exception(message) { }

    public class RobotNotAvailableException(string message) : Exception(message) { }

    public class RobotBusyException(string message) : Exception(message) { }

    public class RobotNotInSameInstallationAsMissionException(string message)
        : Exception(message)
    { }

    public class IsarCommunicationException(string message) : Exception(message) { }

    public class UnsupportedRobotCapabilityException(string message) : Exception(message) { }

    public class InvalidPolygonException(string message) : Exception(message) { }

    public class FailedToRemoveAutoSchedulingException(string message) : Exception(message) { }
}
