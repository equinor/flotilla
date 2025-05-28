using System.Text.RegularExpressions;

namespace Api.Utilities
{
    public static class Validate
    {
        public static string UUID(string id)
        {
            string pattern = @"^[{(]?[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[1-5][0-9a-fA-F]{3}-[89abAB][0-9a-fA-F]{3}-[0-9a-fA-F]{12}[)}]?$";

            var isUUID = Regex.IsMatch(id, pattern);

            if (!isUUID)
            {
                throw new ArgumentException("Invalid UUID format.");
            }

            var sanitizedRobotId = id.Replace("\n", "").Replace("\r", "");
            return sanitizedRobotId;
        }
    }
}
