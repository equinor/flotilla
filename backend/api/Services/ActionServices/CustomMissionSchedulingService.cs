using Api.Controllers.Models;
using Api.Database.Models;
using Api.Utilities;
namespace Api.Services.ActionServices
{
    public interface ICustomMissionSchedulingService
    {
        public Task<MissionDefinition> FindExistingOrCreateCustomMissionDefinition(CustomMissionQuery customMissionQuery, List<MissionTask> missionTasks);

        public Task<MissionRun> QueueCustomMissionRun(CustomMissionQuery customMissionQuery, string missionDefinitionId, string robotId, IList<MissionTask> missionTasks);
    }

    public class CustomMissionSchedulingService(
            ILogger<CustomMissionSchedulingService> logger,
            ICustomMissionService customMissionService,
            IAreaService areaService,
            ISourceService sourceService,
            IMissionDefinitionService missionDefinitionService,
            IMissionRunService missionRunService,
            IRobotService robotService,
            IMapService mapService
        ) : ICustomMissionSchedulingService
    {
        public async Task<MissionDefinition> FindExistingOrCreateCustomMissionDefinition(CustomMissionQuery customMissionQuery, List<MissionTask> missionTasks)
        {
            Area? area = null;
            if (customMissionQuery.AreaName != null) { area = await areaService.ReadByInstallationAndName(customMissionQuery.InstallationCode, customMissionQuery.AreaName); }

            var source = await sourceService.CheckForExistingCustomSource(missionTasks);

            MissionDefinition? existingMissionDefinition = null;
            if (source == null)
            {
                try
                {
                    string sourceUrl = await customMissionService.UploadSource(missionTasks);
                    source = new Source
                    {
                        SourceId = sourceUrl,
                        Type = MissionSourceType.Custom
                    };
                }
                catch (Exception e)
                {
                    {
                        string errorMessage = $"Unable to upload source for mission {customMissionQuery.Name}";
                        logger.LogError(e, "{Message}", errorMessage);
                        throw new SourceException(errorMessage);
                    }
                }
            }
            else
            {
                var missionDefinitions = await missionDefinitionService.ReadBySourceId(source.SourceId);
                if (missionDefinitions.Count > 0) { existingMissionDefinition = missionDefinitions.First(); }
            }

            var customMissionDefinition = existingMissionDefinition ?? new MissionDefinition
            {
                Id = Guid.NewGuid().ToString(),
                Source = source,
                Name = customMissionQuery.Name,
                InspectionFrequency = customMissionQuery.InspectionFrequency,
                InstallationCode = customMissionQuery.InstallationCode,
                Area = area
            };

            if (existingMissionDefinition == null) { await missionDefinitionService.Create(customMissionDefinition); }

            return customMissionDefinition;
        }

        public async Task<MissionRun> QueueCustomMissionRun(CustomMissionQuery customMissionQuery, string missionDefinitionId, string robotId, IList<MissionTask> missionTasks)
        {
            var missionDefinition = await missionDefinitionService.ReadById(missionDefinitionId);
            if (missionDefinition is null)
            {
                string errorMessage = $"The mission definition with ID {missionDefinition} could not be found";
                logger.LogError("{Message}", errorMessage);
                throw new MissionNotFoundException(errorMessage);
            }

            var robot = await robotService.ReadById(robotId);
            if (robot is null)
            {
                string errorMessage = $"The robot with ID {robotId} could not be found";
                logger.LogError("{Message}", errorMessage);
                throw new RobotNotFoundException(errorMessage);
            }

            var scheduledMission = new MissionRun
            {
                Name = customMissionQuery.Name,
                Description = customMissionQuery.Description,
                MissionId = missionDefinition.Id,
                Comment = customMissionQuery.Comment,
                Robot = robot,
                Status = MissionStatus.Pending,
                MissionRunPriority = MissionRunPriority.Normal,
                DesiredStartTime = customMissionQuery.DesiredStartTime ?? DateTime.UtcNow,
                Tasks = missionTasks,
                InstallationCode = customMissionQuery.InstallationCode,
                Area = missionDefinition.Area,
                Map = new MapMetadata()
            };

            await mapService.AssignMapToMission(scheduledMission);

            if (scheduledMission.Tasks.Any()) { scheduledMission.CalculateEstimatedDuration(); }

            return await missionRunService.Create(scheduledMission);
        }
    }
}
