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

    public class CustomMissionSchedulingService : ICustomMissionSchedulingService
    {
        private readonly IAreaService _areaService;
        private readonly ICustomMissionService _customMissionService;
        private readonly ILogger<MissionSchedulingService> _logger;
        private readonly IMapService _mapService;
        private readonly IMissionDefinitionService _missionDefinitionService;
        private readonly IMissionRunService _missionRunService;
        private readonly IRobotService _robotService;
        private readonly ISourceService _sourceService;

        public CustomMissionSchedulingService(
            ILogger<MissionSchedulingService> logger,
            ICustomMissionService customMissionService,
            IAreaService areaService,
            ISourceService sourceService,
            IMissionDefinitionService missionDefinitionService,
            IMissionRunService missionRunService,
            IRobotService robotService,
            IMapService mapService
        )
        {
            _logger = logger;
            _customMissionService = customMissionService;
            _areaService = areaService;
            _sourceService = sourceService;
            _missionDefinitionService = missionDefinitionService;
            _missionRunService = missionRunService;
            _robotService = robotService;
            _mapService = mapService;
        }

        public async Task<MissionDefinition> FindExistingOrCreateCustomMissionDefinition(CustomMissionQuery customMissionQuery, List<MissionTask> missionTasks)
        {
            Area? area = null;
            if (customMissionQuery.AreaName != null) { area = await _areaService.ReadByInstallationAndName(customMissionQuery.InstallationCode, customMissionQuery.AreaName); }

            var source = await _sourceService.CheckForExistingCustomSource(missionTasks);

            MissionDefinition? existingMissionDefinition = null;
            if (source == null)
            {
                try
                {
                    string sourceUrl = await _customMissionService.UploadSource(missionTasks);
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
                        _logger.LogError(e, "{Message}", errorMessage);
                        throw new SourceException(errorMessage);
                    }
                }
            }
            else
            {
                var missionDefinitions = await _missionDefinitionService.ReadBySourceId(source.SourceId);
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

            if (existingMissionDefinition == null) { await _missionDefinitionService.Create(customMissionDefinition); }

            return customMissionDefinition;
        }

        public async Task<MissionRun> QueueCustomMissionRun(CustomMissionQuery customMissionQuery, string missionDefinitionId, string robotId, IList<MissionTask> missionTasks)
        {
            var missionDefinition = await _missionDefinitionService.ReadById(missionDefinitionId);
            if (missionDefinition is null)
            {
                string errorMessage = $"The mission definition with ID {missionDefinition} could not be found";
                _logger.LogError("{Message}", errorMessage);
                throw new MissionNotFoundException(errorMessage);
            }

            var robot = await _robotService.ReadById(robotId);
            if (robot is null)
            {
                string errorMessage = $"The robot with ID {robotId} could not be found";
                _logger.LogError("{Message}", errorMessage);
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
                DesiredStartTime = customMissionQuery.DesiredStartTime ?? DateTimeOffset.UtcNow,
                Tasks = missionTasks,
                InstallationCode = customMissionQuery.InstallationCode,
                Area = missionDefinition.Area,
                Map = new MapMetadata()
            };

            await _mapService.AssignMapToMission(scheduledMission);

            if (scheduledMission.Tasks.Any()) { scheduledMission.CalculateEstimatedDuration(); }

            return await _missionRunService.Create(scheduledMission);
        }
    }
}
