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
        public Task<IQueryable<CondensedMissionDefinition>> GetAvailableMissions(
            string? installationCode
        );
        public Task<CondensedMissionDefinition?> GetMissionById(string sourceMissionId);
        public Task<List<MissionTask>?> GetTasksForMission(string missionSourceId);
    }

    public class EchoService(
        ILogger<EchoService> logger,
        IDownstreamApi echoApi,
        ISourceService sourceService,
        IInspectionService inspectionService
    ) : IEchoService
    {
        public const string ServiceName = "EchoApi";

        public async Task<IQueryable<CondensedMissionDefinition>> GetAvailableMissions(
            string? installationCode
        )
        {
            string relativePath = string.IsNullOrEmpty(installationCode)
                ? "robots/robot-plan?Status=Ready"
                : $"robots/robot-plan?InstallationCode={installationCode}&&Status=Ready";

            var response = await echoApi.CallApiForAppAsync(
                ServiceName,
                options =>
                {
                    options.HttpMethod = HttpMethod.Get.Method;
                    options.RelativePath = relativePath;
                }
            );

            if (!response.IsSuccessStatusCode)
            {
                throw new MissionLoaderUnavailableException(
                    $"Echo API unavailable. Status code: {response.StatusCode}"
                );
            }

            var echoMissions =
                await response.Content.ReadFromJsonAsync<List<EchoMissionResponse>>()
                ?? throw new JsonException("Failed to deserialize missions from Echo");

            var availableMissions = new List<CondensedMissionDefinition>();

            foreach (var echoMissionResponse in echoMissions)
            {
                var echoMission = ProcessEchoMission(echoMissionResponse);
                if (echoMission == null)
                {
                    continue;
                }
                var missionDefinitionResponse = await EchoMissionToCondensedMissionDefinition(
                    echoMission
                );
                if (missionDefinitionResponse == null)
                {
                    continue;
                }
                availableMissions.Add(missionDefinitionResponse);
            }

            return availableMissions.AsQueryable();
        }

        public async Task<CondensedMissionDefinition?> GetMissionById(string sourceMissionId)
        {
            var echoMission = await GetEchoMission(sourceMissionId);
            var mission = await EchoMissionToCondensedMissionDefinition(echoMission);
            return mission;
        }

        private async Task<EchoMission> GetEchoMission(string echoMissionId)
        {
            string relativePath = $"robots/robot-plan/{echoMissionId}";

            var response = await echoApi.CallApiForAppAsync(
                ServiceName,
                options =>
                {
                    options.HttpMethod = HttpMethod.Get.Method;
                    options.RelativePath = relativePath;
                }
            );

            if (!response.IsSuccessStatusCode)
            {
                throw new MissionLoaderUnavailableException(
                    $"Echo API unavailable. Status code: {response.StatusCode}"
                );
            }

            var echoMission =
                await response.Content.ReadFromJsonAsync<EchoMissionResponse>()
                ?? throw new JsonException("Failed to deserialize mission from Echo");
            var processedEchoMission =
                ProcessEchoMission(echoMission)
                ?? throw new InvalidDataException(
                    $"EchoMission with id: {echoMissionId} is invalid"
                );
            return processedEchoMission;
        }

        public async Task<List<MissionTask>?> GetTasksForMission(string missionSourceId)
        {
            var echoMission = await GetEchoMission(missionSourceId);
            var missionTasks = echoMission
                .Tags.Select(t => MissionTasksFromEchoTag(t))
                .SelectMany(task => task.Result)
                .ToList();
            return missionTasks;
        }

        private async Task<CondensedMissionDefinition?> EchoMissionToCondensedMissionDefinition(
            EchoMission echoMission
        )
        {
            var source =
                await sourceService.CheckForExistingSource(echoMission.Id)
                ?? await sourceService.Create(new Source { SourceId = $"{echoMission.Id}" });

            var missionDefinition = new CondensedMissionDefinition
            {
                Id = Guid.NewGuid().ToString(),
                SourceId = source.SourceId,
                Name = echoMission.Name,
                InstallationCode = echoMission.InstallationCode,
            };
            return missionDefinition;
        }

        private static List<EchoTag> ProcessPlanItems(
            List<PlanItem> planItems,
            string installationCode
        )
        {
            var tags = new List<EchoTag>();

            var indices = new HashSet<int>();
            bool inconsistentIndices = false;

            for (int i = 0; i < planItems.Count; i++)
            {
                var planItem = planItems[i];
                if (
                    planItem.SortingOrder < 0
                    || planItem.SortingOrder >= planItems.Count
                    || indices.Contains(planItem.SortingOrder)
                )
                    inconsistentIndices = true;
                indices.Add(planItem.SortingOrder);

                if (planItem.PoseId is null)
                {
                    string message =
                        $"Invalid EchoMission {planItem.Tag} has no associated pose id";
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
                    Inspections =
                    [
                        .. planItem
                            .SensorTypes.Select(sensor => new EchoInspection(
                                sensor,
                                planItem.InspectionPoint.EnuPosition.ToPosition(),
                                planItem.InspectionPoint.Name
                            ))
                            .Distinct(new EchoInspectionComparer()),
                    ],
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
                    Tags = ProcessPlanItems(echoMission.PlanItems, echoMission.InstallationCode),
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

        private async Task<IList<MissionTask>> MissionTasksFromEchoTag(EchoTag echoTag)
        {
            var inspections = echoTag
                .Inspections.Select(inspection => new Inspection(
                    inspectionType: inspection.InspectionType,
                    videoDuration: inspection.TimeInSeconds,
                    inspectionTarget: inspection.InspectionPoint,
                    inspectionTargetName: inspection.InspectionPointName
                ))
                .ToList();

            var missionTasks = new List<MissionTask>();

            foreach (var inspection in inspections)
            {
                missionTasks.Add(
                    new MissionTask(
                        inspection: inspection,
                        tagLink: echoTag.URL,
                        tagId: echoTag.TagId,
                        robotPose: new Pose(echoTag.Pose),
                        poseId: echoTag.PoseId,
                        taskOrder: echoTag.PlanOrder,
                        taskDescription: inspection.InspectionTargetName,
                        zoomDescription: await inspectionService.FindInspectionZoom(echoTag),
                        status: Database.Models.TaskStatus.NotStarted
                    )
                );
            }

            return missionTasks;
        }
    }
}
