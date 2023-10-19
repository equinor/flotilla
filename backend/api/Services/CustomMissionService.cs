using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Api.Controllers.Models;
using Api.Database.Models;
using Api.Options;
using Api.Utilities;
using Microsoft.Extensions.Options;
namespace Api.Services
{

    public interface ICustomMissionService
    {
        Task<string> UploadSource(List<MissionTask> tasks);

        Task<List<MissionTask>?> GetMissionTasksFromSourceId(string id);

        public Task<MissionRun> QueueCustomMissionRun(CustomMissionQuery customMissionQuery, string missionDefinitionId, string robotId, IList<MissionTask> missionTasks);

        string CalculateHashFromTasks(IList<MissionTask> tasks);
    }

    public class CustomMissionService : ICustomMissionService
    {
        private readonly IBlobService _blobService;
        private readonly ILogger<CustomMissionService> _logger;
        private readonly IMapService _mapService;
        private readonly IMissionDefinitionService _missionDefinitionService;
        private readonly IMissionRunService _missionRunService;
        private readonly IRobotService _robotService;
        private readonly IOptions<StorageOptions> _storageOptions;

        public CustomMissionService(
            ILogger<CustomMissionService> logger,
            IOptions<StorageOptions> storageOptions,
            IBlobService blobService,
            IMissionDefinitionService missionDefinitionService,
            IMissionRunService missionRunService,
            IRobotService robotService, IMapService mapService)
        {
            _logger = logger;
            _storageOptions = storageOptions;
            _blobService = blobService;
            _missionDefinitionService = missionDefinitionService;
            _missionRunService = missionRunService;
            _robotService = robotService;
            _mapService = mapService;
        }

        public async Task<string> UploadSource(List<MissionTask> tasks)
        {
            string json = JsonSerializer.Serialize(tasks);
            string hash = CalculateHashFromTasks(tasks);
            await _blobService.UploadJsonToBlob(json, hash, _storageOptions.Value.CustomMissionContainerName, _storageOptions.Value.AccountName, false);

            return hash;
        }

        public async Task<List<MissionTask>?> GetMissionTasksFromSourceId(string id)
        {
            List<MissionTask>? content;
            try
            {
                byte[] rawContent = await _blobService.DownloadBlob(id, _storageOptions.Value.CustomMissionContainerName, _storageOptions.Value.AccountName);
                var rawBinaryContent = new BinaryData(rawContent);
                content = rawBinaryContent.ToObjectFromJson<List<MissionTask>>();
                foreach (var task in content)
                {
                    task.Id = Guid.NewGuid().ToString(); // This is needed as tasks are owned by mission runs
                }
            }
            catch (Exception)
            {
                return null;
            }

            return content;
        }

        public string CalculateHashFromTasks(IList<MissionTask> tasks)
        {
            IList<MissionTask> genericTasks = new List<MissionTask>();
            foreach (var task in tasks)
            {
                var taskCopy = new MissionTask(task);
                genericTasks.Add(taskCopy);
            }

            string json = JsonSerializer.Serialize(genericTasks);
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));
            return BitConverter.ToString(hash).Replace("-", "", StringComparison.CurrentCulture).ToUpperInvariant();
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
