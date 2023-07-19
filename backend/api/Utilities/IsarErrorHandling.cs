namespace Api.Utilities
{
    public static class IsarErrorHandling
    {
        public static string? MapIsarError(string isarErrorReason)
        {
            if (!isarErrorMapping.ContainsKey(isarErrorReason))
            {
                return null;
            }
            return isarErrorMapping[isarErrorReason];
        }

        private static readonly Dictionary<string, string> isarErrorMapping = new()
        {
            {"robot_communication_exception", "The step failed as the system was not able to communicate with the robot"}
        };
    }
}
