namespace api.Services;

// {
//     public interface ITempService
//     {
//         public Task<MissionRun?> ScheduleTempMissionRunIfNotAlreadyScheduled(Robot robot);
//         public Task<MissionRun?> GetActiveTempMissionRun(
//             string robotId,
//             bool readOnly = true
//         );
//     }

//     public class TempService(
//         ILogger<TempService> logger,
//         IMissionRunService missionRunService
//     ) : ITempService
//     {
//     }


// List<Area?> missionAreas;
//             missionAreas = missionTasks
//                 .Where(t => t.TagId != null)
//                 .Select(t =>
//                     stidService.GetTagArea(t.TagId!, scheduledMissionQuery.InstallationCode).Result
//                 )
//                 .ToList();

//             var missionInspectionAreaNames = missionAreas
//                 .Where(a => a != null)
//                 .Select(a => a!.InspectionArea.Name)
//                 .Distinct()
//                 .ToList();
//             if (missionInspectionAreaNames.Count > 1)
//             {
//                 string joinedMissionInspectionAreaNames = string.Join(
//                     ", ",
//                     [.. missionInspectionAreaNames]
//                 );
//                 logger.LogWarning(
//                     "Mission {missionDefinition} has tags on more than one inspection area. The inspection areas are: {joinedMissionInspectionAreaNames}.",
//                     missionDefinition.Name,
//                     joinedMissionInspectionAreaNames
//                 );
//             }

//             var sortedAreas = missionAreas
//                 .GroupBy(i => i)
//                 .OrderByDescending(grp => grp.Count())
//                 .Select(grp => grp.Key);
//             var area = sortedAreas.First();

//             if (area == null && sortedAreas.Count() > 1)
//             {
//                 logger.LogWarning(
//                     "Most common area in mission {missionDefinition} is null. Will use second most common area.",
//                     missionDefinition.Name
//                 );
//                 area = sortedAreas.Skip(1).First();
//             }
//             if (area == null)
//             {
//                 logger.LogError(
//                     $"Mission {missionDefinition.Name} doesn't have any tags with valid area."
//                 );
//                 return NotFound($"No area found for mission '{missionDefinition.Name}'.");
//             }



// List<Area?> missionAreas;
//             missionAreas =
//             [
//                 .. missionTasks
//                     .Where(t => t.TagId != null)
//                     .Select(t =>
//                         stidService.GetTagArea(t.TagId, echoMission.InstallationCode).Result
//                     ),
//             ];

//             var missionInspectionAreaNames = missionAreas
//                 .Where(a => a != null)
//                 .Select(a => a!.InspectionArea.Name)
//                 .Distinct()
//                 .ToList();
//             if (missionInspectionAreaNames.Count > 1)
//             {
//                 string joinedMissionInspectionAreaNames = string.Join(
//                     ", ",
//                     [.. missionInspectionAreaNames]
//                 );
//                 logger.LogWarning(
//                     "Mission {echoMissionName} has tags on more than one inspection area. The inspection areas are: {joinedMissionInspectionAreaNames}.",
//                     echoMission.Name,
//                     joinedMissionInspectionAreaNames
//                 );
//             }

//             var sortedAreas = missionAreas
//                 .GroupBy(i => i)
//                 .OrderByDescending(grp => grp.Count())
//                 .Select(grp => grp.Key);
//             var area = sortedAreas.First();

//             if (area == null && sortedAreas.Count() > 1)
//             {
//                 logger.LogWarning(
//                     "Most common area in mission {echoMissionName} is null. Will use second most common area.",
//                     echoMission.Name
//                 );
//                 area = sortedAreas.Skip(1).First();
//             }
//             if (area == null)
//             {
//                 logger.LogError(
//                     "Mission {echoMissionName} doesn't have any tags with valid area.",
//                     echoMission.Name
//                 );
//                 return null;
//             }


// if (missionRun.InspectionArea == null)
//             {
//                 logger.LogWarning(
//                     "Mission {MissionRunId} does not have an inspection area and was therefore not started",
//                     missionRun.Id
//                 );
//                 return;
//             }

//             if (robot.CurrentInspectionArea == null && missionRun.InspectionArea != null)
//             {
//                 await robotService.UpdateCurrentInspectionArea(
//                     robot.Id,
//                     missionRun.InspectionArea.Id
//                 );
//                 robot.CurrentInspectionArea = missionRun.InspectionArea;
//             }
//             else if (
//                 !await localizationService.RobotIsOnSameInspectionAreaAsMission(
//                     robot.Id,
//                     missionRun.InspectionArea!.Id
//                 )
//             )
//             {
//                 logger.LogError(
//                     "Robot {RobotId} is not on the same inspection area as the mission run {MissionRunId}. Aborting all mission runs",
//                     robot.Id,
//                     missionRun.Id
//                 );
//                 try
//                 {
//                     await AbortAllScheduledNormalMissions(
//                         robot.Id,
//                         "Aborted: Robot was at different inspection area"
//                     );
//                 }
//                 catch (RobotNotFoundException)
//                 {
//                     logger.LogError(
//                         "Failed to abort scheduled missions for robot {RobotId}",
//                         robot.Id
//                     );
//                 }

//                 return;
//             }
