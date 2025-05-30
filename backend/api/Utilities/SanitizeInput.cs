using Api.Controllers.Models;
using Api.Database.Models;

namespace Api.Utilities
{
    public static class Sanitize
    {
        public static string SanitizeUserInput(string inputString)
        {
            return inputString.Replace("\n", "").Replace("\r", "");
        }

        public static ScheduleMissionQuery SanitizeUserInput(ScheduleMissionQuery inputQuery)
        {
            inputQuery.RobotId = SanitizeUserInput(inputQuery.RobotId);
            return inputQuery;
        }

        public static ScheduledMissionQuery SanitizeUserInput(ScheduledMissionQuery inputQuery)
        {
            inputQuery.RobotId = SanitizeUserInput(inputQuery.RobotId);
            inputQuery.MissionSourceId = SanitizeUserInput(inputQuery.MissionSourceId);
            inputQuery.InstallationCode = SanitizeUserInput(inputQuery.InstallationCode);
            return inputQuery;
        }

        public static CustomMissionQuery SanitizeUserInput(CustomMissionQuery inputQuery)
        {
            inputQuery.RobotId = SanitizeUserInput(inputQuery.RobotId);
            inputQuery.InstallationCode = SanitizeUserInput(inputQuery.InstallationCode);
            inputQuery.Name = SanitizeUserInput(inputQuery.Name);

            return inputQuery;
        }

        public static CreateRobotQuery SanitizeUserInput(CreateRobotQuery inputQuery)
        {
            inputQuery.Name = SanitizeUserInput(inputQuery.Name);
            inputQuery.IsarId = SanitizeUserInput(inputQuery.IsarId);
            inputQuery.SerialNumber = SanitizeUserInput(inputQuery.SerialNumber);
            inputQuery.CurrentInstallationCode = SanitizeUserInput(
                inputQuery.CurrentInstallationCode
            );
            inputQuery.Host = SanitizeUserInput(inputQuery.Host);

            return inputQuery;
        }

        public static UpdateRobotQuery SanitizeUserInput(UpdateRobotQuery inputQuery)
        {
            inputQuery.InspectionAreaId =
                inputQuery.InspectionAreaId == null
                    ? null
                    : SanitizeUserInput(inputQuery.InspectionAreaId);
            inputQuery.MissionId =
                inputQuery.MissionId == null ? null : SanitizeUserInput(inputQuery.MissionId);

            return inputQuery;
        }

        public static SkipAutoMissionQuery SanitizeUserInput(SkipAutoMissionQuery inputQuery)
        {
            inputQuery.TimeOfDay = new TimeOnly(
                inputQuery.TimeOfDay.Hour,
                inputQuery.TimeOfDay.Minute
            );
            return inputQuery;
        }
    }
}
