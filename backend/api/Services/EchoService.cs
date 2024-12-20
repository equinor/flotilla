using System.Globalization;
using System.Text.Json;
using Api.Controllers.Models;
using Api.Database.Context;
using Api.Database.Models;
using Api.Services.MissionLoaders;
using Api.Services.Models;
using Api.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Abstractions;
namespace Api.Services
{
    public interface IEchoService
    {
        public Task<IQueryable<MissionDefinition>> GetAvailableMissions(string? installationCode);
        public Task<MissionDefinition?> GetMissionById(string sourceMissionId);
        public Task<List<MissionTask>> GetTasksForMission(string missionSourceId);
        public Task<List<PlantInfo>> GetPlantInfos();
        public Task<TagInspectionMetadata> CreateOrUpdateTagInspectionMetadata(TagInspectionMetadata metadata);
    }

    public class EchoService(
            ILogger<EchoService> logger, IDownstreamApi echoApi, ISourceService sourceService, IStidService stidService, FlotillaDbContext context) : IEchoService
    {

        public const string ServiceName = "EchoApi";

        public async Task<IQueryable<MissionDefinition>> GetAvailableMissions(string? installationCode)
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

            var availableMissions = new List<MissionDefinition>();

            foreach (var echoMissionResponse in echoMissions)
            {
                var echoMission = ProcessEchoMission(echoMissionResponse);
                if (echoMission == null)
                {
                    continue;
                }
                var missionDefinition = await EchoMissionToMissionDefinition(echoMission);
                if (missionDefinition == null)
                {
                    continue;
                }
                availableMissions.Add(missionDefinition);
            }

            return availableMissions.AsQueryable();
        }

        public async Task<MissionDefinition?> GetMissionById(string sourceMissionId)
        {
            var echoMission = await GetEchoMission(sourceMissionId);

            var mission = await EchoMissionToMissionDefinition(echoMission);
            return mission;
        }

        private async Task<EchoMission> GetEchoMission(string echoMissionId)
        {
            string relativePath = $"robots/robot-plan/{echoMissionId}";

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
            var processedEchoMission = ProcessEchoMission(echoMission) ?? throw new InvalidDataException($"EchoMission with id: {echoMissionId} is invalid");
            return processedEchoMission;
        }

        public async Task<List<MissionTask>> GetTasksForMission(string missionSourceId)
        {
            var echoMission = await GetEchoMission(missionSourceId);
            var missionTasks = echoMission.Tags.Select(t => MissionTasksFromEchoTag(t)).SelectMany(task => task.Result).ToList();
            return missionTasks;
        }

        private async Task<MissionDefinition?> EchoMissionToMissionDefinition(EchoMission echoMission)
        {
            var source = await sourceService.CheckForExistingSource(echoMission.Id) ?? await sourceService.Create(
                    new Source
                    {
                        SourceId = $"{echoMission.Id}",
                    }
                );
            var missionTasks = echoMission.Tags;
            List<Area?> missionAreas;
            missionAreas = missionTasks
                .Where(t => t.TagId != null)
                .Select(t => stidService.GetTagArea(t.TagId, echoMission.InstallationCode).Result)
                .ToList();

            var missionInspectionAreaNames = missionAreas.Where(a => a != null).Select(a => a!.InspectionArea.Name).Distinct().ToList();
            if (missionInspectionAreaNames.Count > 1)
            {
                string joinedMissionInspectionAreaNames = string.Join(", ", [.. missionInspectionAreaNames]);
                logger.LogWarning($"Mission {echoMission.Name} has tags on more than one inspection area. The inspection areas are: {joinedMissionInspectionAreaNames}.");
            }

            var sortedAreas = missionAreas.GroupBy(i => i).OrderByDescending(grp => grp.Count()).Select(grp => grp.Key);
            var area = sortedAreas.First();

            if (area == null && sortedAreas.Count() > 1)
            {
                logger.LogWarning($"Most common area in mission {echoMission.Name} is null. Will use second most common area.");
                area = sortedAreas.Skip(1).First();

            }
            if (area == null)
            {
                logger.LogError($"Mission {echoMission.Name} doesn't have any tags with valid area.");
                return null;
            }

            var missionDefinition = new MissionDefinition
            {
                Id = Guid.NewGuid().ToString(),
                Source = source,
                Name = echoMission.Name,
                InstallationCode = echoMission.InstallationCode,
                InspectionArea = area.InspectionArea
            };
            return missionDefinition;
        }

        public async Task<List<PlantInfo>> GetPlantInfos()
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
                        .Select(sensor => new EchoInspection(sensor, planItem.InspectionPoint.EnuPosition.ToPosition(), planItem.InspectionPoint.Name)).Distinct(new EchoInspectionComparer()).ToList()
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
                    Id = echoMission.Id.ToString(CultureInfo.CurrentCulture),
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

        private static List<PlantInfo> ProcessEchoPlantInfos(
            List<EchoPlantInfoResponse> echoPlantInfoResponse
        )
        {
            var echoPlantInfos = new List<PlantInfo>();
            foreach (var plant in echoPlantInfoResponse)
            {
                if (plant.InstallationCode is null || plant.ProjectDescription is null)
                {
                    continue;
                }

                var echoPlantInfo = new PlantInfo
                {
                    PlantCode = plant.InstallationCode,
                    ProjectDescription = plant.ProjectDescription
                };

                echoPlantInfos.Add(echoPlantInfo);
            }
            return echoPlantInfos;
        }

        public async Task<IList<MissionTask>> MissionTasksFromEchoTag(EchoTag echoTag)
        {
            var inspections = echoTag.Inspections
                .Select(inspection => new Inspection(
                    inspectionType: inspection.InspectionType,
                    videoDuration: inspection.TimeInSeconds,
                    inspectionTarget: inspection.InspectionPoint,
                    inspectionTargetName: inspection.InspectionPointName,
                    status: InspectionStatus.NotStarted))
                .ToList();

            var missionTasks = new List<MissionTask>();

            foreach (var inspection in inspections)
            {
                missionTasks.Add(
                    new MissionTask
                    (
                        inspection: inspection,
                        tagLink: echoTag.URL,
                        tagId: echoTag.TagId,
                        robotPose: echoTag.Pose,
                        poseId: echoTag.PoseId,
                        taskOrder: echoTag.PlanOrder,
                        taskDescription: inspection.InspectionTargetName,
                        zoomDescription: await FindInspectionZoom(echoTag),
                        status: Database.Models.TaskStatus.NotStarted,
                        type: MissionTaskType.Inspection
                    ));
            }

            return missionTasks;
        }

        public async Task<TagInspectionMetadata> CreateOrUpdateTagInspectionMetadata(TagInspectionMetadata metadata)
        {
            var existingMetadata = await context.TagInspectionMetadata.Where(e => e.TagId == metadata.TagId).FirstOrDefaultAsync();
            if (existingMetadata == null)
            {
                await context.TagInspectionMetadata.AddAsync(metadata);
            }
            else
            {
                existingMetadata.ZoomDescription = metadata.ZoomDescription;
                context.TagInspectionMetadata.Update(existingMetadata);
            }

            await context.SaveChangesAsync();
            return metadata;
        }

        private async Task<IsarZoomDescription?> FindInspectionZoom(EchoTag echoTag)
        {
            return (await context.TagInspectionMetadata.Where((e) => e.TagId == echoTag.TagId).FirstOrDefaultAsync())?.ZoomDescription;
        }
    }
}
