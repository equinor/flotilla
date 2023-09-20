using System.Text.Json;
using Api.Controllers.Models;
using Api.Database.Models;
using Api.Services.Models;
using Api.Utilities;
using Microsoft.Identity.Abstractions;
using Microsoft.IdentityModel.Tokens;
namespace Api.Services
{
    public interface IEchoService
    {
        public Task<IList<CondensedMissionDefinition>> GetAvailableMissions(string? installationCode);

        public Task<EchoMission> GetMissionById(int missionId);

        public Task<IList<EchoPlantInfo>> GetEchoPlantInfos();
        public Task<EchoPoseResponse> GetRobotPoseFromPoseId(int poseId);
    }

    public class EchoService : IEchoService
    {
        public const string ServiceName = "EchoApi";
        private readonly IDownstreamApi _echoApi;
        private readonly ILogger<EchoService> _logger;

        public EchoService(IDownstreamApi downstreamWebApi, ILogger<EchoService> logger)
        {
            _echoApi = downstreamWebApi;
            _logger = logger;
        }

        public async Task<IList<CondensedMissionDefinition>> GetAvailableMissions(string? installationCode)
        {
            string relativePath = string.IsNullOrEmpty(installationCode)
                ? "robots/robot-plan?Status=Ready"
                : $"robots/robot-plan?InstallationCode={installationCode}&&Status=Ready";

            var response = await _echoApi.CallApiForUserAsync(
                ServiceName,
                options =>
                {
                    options.HttpMethod = HttpMethod.Get;
                    options.RelativePath = relativePath;
                }
            );

            response.EnsureSuccessStatusCode();

            var echoMissions = await response.Content.ReadFromJsonAsync<
                List<EchoMissionResponse>
            >() ?? throw new JsonException("Failed to deserialize missions from Echo");
            var availableMissions = ProcessAvailableEchoMission(echoMissions);

            return availableMissions;
        }

        public async Task<EchoMission> GetMissionById(int missionId)
        {
            string relativePath = $"robots/robot-plan/{missionId}";

            var response = await _echoApi.CallApiForUserAsync(
                ServiceName,
                options =>
                {
                    options.HttpMethod = HttpMethod.Get;
                    options.RelativePath = relativePath;
                }
            );

            response.EnsureSuccessStatusCode();

            var echoMission = await response.Content.ReadFromJsonAsync<EchoMissionResponse>() ?? throw new JsonException("Failed to deserialize mission from Echo");
            var processedEchoMission = ProcessEchoMission(echoMission) ?? throw new InvalidDataException($"EchoMission with id: {missionId} is invalid.");
            return processedEchoMission;
        }

        public async Task<IList<EchoPlantInfo>> GetEchoPlantInfos()
        {
            string relativePath = "plantinfo";
            var response = await _echoApi.CallApiForUserAsync(
                ServiceName,
                options =>
                {
                    options.HttpMethod = HttpMethod.Get;
                    options.RelativePath = relativePath;
                }
            );

            response.EnsureSuccessStatusCode();
            var echoPlantInfoResponse = await response.Content.ReadFromJsonAsync<
                List<EchoPlantInfoResponse>
            >() ?? throw new JsonException("Failed to deserialize plant information from Echo");
            var installations = ProcessEchoPlantInfos(echoPlantInfoResponse);
            return installations;
        }

        public async Task<EchoPoseResponse> GetRobotPoseFromPoseId(int poseId)
        {
            string relativePath = $"/robots/pose/{poseId}";
            var response = await _echoApi.CallApiForUserAsync(
                ServiceName,
                options =>
                {
                    options.HttpMethod = HttpMethod.Get;
                    options.RelativePath = relativePath;
                }
            );
            response.EnsureSuccessStatusCode();
            var echoPoseResponse = await response.Content.ReadFromJsonAsync<EchoPoseResponse>() ?? throw new JsonException("Failed to deserialize robot pose from Echo");
            return echoPoseResponse;
        }

        private List<EchoTag> ProcessPlanItems(List<PlanItem> planItems, string installationCode)
        {
            var tags = new List<EchoTag>();

            foreach (var planItem in planItems)
            {
                if (planItem.PoseId is null)
                {
                    string message =
                        $"Invalid EchoMission: {planItem.Tag} has no associated pose id.";
                    throw new InvalidDataException(message);
                }
                var robotPose = GetRobotPoseFromPoseId(planItem.PoseId.Value).Result;
                var tag = new EchoTag
                {
                    Id = planItem.Id,
                    TagId = planItem.Tag,
                    PoseId = planItem.PoseId.Value,
                    PlanOrder = planItem.SortingOrder,
                    Pose = new Pose(
                        robotPose.Position,
                        robotPose.RobotBodyDirectionDegrees * MathF.PI / 180
                    ),
                    URL = new Uri(
                        $"https://stid.equinor.com/{installationCode}/tag?tagNo={planItem.Tag}"
                    ),
                    Inspections = planItem.SensorTypes
                        .Select(sensor => new EchoInspection(sensor))
                        .ToList()
                };

                if (tag.Inspections.IsNullOrEmpty())
                {
                    tag.Inspections.Add(new EchoInspection());
                }

                tags.Add(tag);
            }

            return tags;
        }

        private List<CondensedMissionDefinition> ProcessAvailableEchoMission(List<EchoMissionResponse> echoMissions)
        {
            var availableMissions = new List<CondensedMissionDefinition>();

            foreach (var echoMission in echoMissions)
            {
                if (echoMission.PlanItems is null)
                {
                    continue;
                }
                try
                {
                    var condensedEchoMissionDefinition = new CondensedMissionDefinition
                    {
                        EchoMissionId = echoMission.Id,
                        Name = echoMission.Name,
                        InstallationCode = echoMission.InstallationCode
                    };
                    availableMissions.Add(condensedEchoMissionDefinition);
                }
                catch (InvalidDataException e)
                {
                    _logger.LogWarning(
                        "Echo mission with ID '{id}' is invalid: '{message}'",
                        echoMission.Id,
                        e.Message
                    );
                }
            }
            return availableMissions;
        }

        private EchoMission? ProcessEchoMission(EchoMissionResponse echoMission)
        {
            if (echoMission.PlanItems is null)
            {
                throw new MissionNotFoundException("Mission has no tags");
            }
            try
            {
                var mission = new EchoMission
                {
                    Id = echoMission.Id,
                    Name = echoMission.Name,
                    InstallationCode = echoMission.InstallationCode,
                    URL = new Uri($"https://echo.equinor.com/mp?editId={echoMission.Id}"),
                    Tags = ProcessPlanItems(echoMission.PlanItems, echoMission.InstallationCode)
                };
                return mission;
            }
            catch (InvalidDataException e)
            {
                _logger.LogWarning(
                    "Echo mission with ID '{id}' is invalid: '{message}'",
                    echoMission.Id,
                    e.Message
                );
                return null;
            }
        }

        private static List<EchoPlantInfo> ProcessEchoPlantInfos(
            List<EchoPlantInfoResponse> echoPlantInfoResponse
        )
        {
            var echoPlantInfos = new List<EchoPlantInfo>();
            foreach (var plant in echoPlantInfoResponse)
            {
                if (plant.InstallationCode is null || plant.ProjectDescription is null)
                {
                    continue;
                }

                var echoPlantInfo = new EchoPlantInfo
                {
                    PlantCode = plant.InstallationCode,
                    ProjectDescription = plant.ProjectDescription
                };

                echoPlantInfos.Add(echoPlantInfo);
            }
            return echoPlantInfos;
        }
    }
}
