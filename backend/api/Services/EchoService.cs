using System.Text.Json;
using Api.Controllers.Models;
using Api.Database.Models;
using Api.Utilities;
using Microsoft.Identity.Abstractions;
namespace Api.Services
{
    public interface IEchoService
    {
        public Task<IList<CondensedEchoMissionDefinition>> GetAvailableMissions(string? installationCode);

        public Task<EchoMission> GetMissionById(int missionId);

        public Task<IList<EchoPlantInfo>> GetEchoPlantInfos();
    }

    public class EchoService(IDownstreamApi echoApi, ILogger<EchoService> logger) : IEchoService
    {
        public const string ServiceName = "EchoApi";

        public async Task<IList<CondensedEchoMissionDefinition>> GetAvailableMissions(string? installationCode)
        {
            string relativePath = string.IsNullOrEmpty(installationCode)
                ? "robots/robot-plan?Status=Ready"
                : $"robots/robot-plan?InstallationCode={installationCode}&&Status=Ready";

            var response = await echoApi.CallApiForUserAsync(
                ServiceName,
                options =>
                {
                    options.HttpMethod = HttpMethod.Get.Method;
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

            var response = await echoApi.CallApiForUserAsync(
                ServiceName,
                options =>
                {
                    options.HttpMethod = HttpMethod.Get.Method;
                    options.RelativePath = relativePath;
                }
            );

            response.EnsureSuccessStatusCode();

            var echoMission = await response.Content.ReadFromJsonAsync<EchoMissionResponse>() ?? throw new JsonException("Failed to deserialize mission from Echo");
            var processedEchoMission = ProcessEchoMission(echoMission) ?? throw new InvalidDataException($"EchoMission with id: {missionId} is invalid");
            return processedEchoMission;
        }

        public async Task<IList<EchoPlantInfo>> GetEchoPlantInfos()
        {
            string relativePath = "plantinfo";
            var response = await echoApi.CallApiForUserAsync(
                ServiceName,
                options =>
                {
                    options.HttpMethod = HttpMethod.Get.Method;
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

        private static List<EchoTag> ProcessPlanItems(List<PlanItem> planItems, string installationCode)
        {
            var tags = new List<EchoTag>();

            var indices = new HashSet<int>();
            bool inconsistentIndices = false;

            for (int i = 0; i < planItems.Count; i++)
            {
                var planItem = planItems[i];
                if (planItem.SortingOrder < 0 || planItem.SortingOrder >= planItems.Count || indices.Contains(planItem.SortingOrder))
                    inconsistentIndices = true;
                indices.Add(planItem.SortingOrder);

                if (planItem.PoseId is null)
                {
                    string message = $"Invalid EchoMission {planItem.Tag} has no associated pose id";
                    throw new InvalidDataException(message);
                }

                var tag = new EchoTag
                {
                    Id = planItem.Id,
                    TagId = planItem.Tag,
                    PoseId = planItem.PoseId.Value,
                    PlanOrder = planItem.SortingOrder,
                    Pose = new Pose(
                        planItem.EchoPose.Position,
                        planItem.EchoPose.RobotBodyDirectionDegrees * MathF.PI / 180
                    ),
                    URL = new Uri(
                        $"https://stid.equinor.com/{installationCode}/tag?tagNo={planItem.Tag}"
                    ),
                    Inspections = planItem.SensorTypes
                        .Select(sensor => new EchoInspection(sensor, planItem.InspectionPoint.EnuPosition.ToPosition())).Distinct(new EchoInspectionComparer()).ToList()
                };

                if (tag.Inspections.Count < 1)
                {
                    tag.Inspections.Add(new EchoInspection());
                }

                tags.Add(tag);
            }

            if (inconsistentIndices)
                for (int i = 0; i < tags.Count; i++)
                    tags[i].PlanOrder = i;

            return tags;
        }

        private List<CondensedEchoMissionDefinition> ProcessAvailableEchoMission(List<EchoMissionResponse> echoMissions)
        {
            var availableMissions = new List<CondensedEchoMissionDefinition>();

            foreach (var echoMission in echoMissions)
            {
                if (echoMission.PlanItems is null)
                {
                    continue;
                }
                try
                {
                    var condensedEchoMissionDefinition = new CondensedEchoMissionDefinition
                    {
                        EchoMissionId = echoMission.Id,
                        Name = echoMission.Name,
                        InstallationCode = echoMission.InstallationCode
                    };
                    availableMissions.Add(condensedEchoMissionDefinition);
                }
                catch (InvalidDataException e)
                {
                    logger.LogWarning(
                        "Echo mission with ID '{Id}' is invalid: '{Message}'",
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
                logger.LogWarning(
                    "Echo mission with ID '{Id}' is invalid: '{Message}'",
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
