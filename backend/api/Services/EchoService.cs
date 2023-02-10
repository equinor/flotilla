﻿using System.Text.Json;
using Api.Controllers.Models;
using Api.Database.Models;
using Api.Services.Models;
using Api.Utilities;
using Microsoft.Identity.Web;
using static Api.Database.Models.IsarStep;

namespace Api.Services
{
    public interface IEchoService
    {
        public abstract Task<IList<EchoMission>> GetMissions(string? installationCode);

        public abstract Task<EchoMission> GetMissionById(int missionId);

        public abstract Task<IList<EchoPlantInfo>> GetEchoPlantInfos();
        public abstract Task<EchoPoseResponse> GetRobotPoseFromPoseId(int poseId);
    }

    public class EchoService : IEchoService
    {
        public const string ServiceName = "EchoApi";
        private readonly IDownstreamWebApi _echoApi;
        private readonly string _installationCode;
        private readonly ILogger<EchoService> _logger;
        public EchoService(IConfiguration config, IDownstreamWebApi downstreamWebApi, ILogger<EchoService> logger)
        {
            _echoApi = downstreamWebApi;
            _installationCode = config.GetValue<string>("InstallationCode");
            _logger = logger;
        }

        public async Task<IList<EchoMission>> GetMissions(string? installationCode)
        {
            string relativePath = string.IsNullOrEmpty(installationCode)
              ? $"robots/robot-plan?Status=Ready"
              : $"robots/robot-plan?InstallationCode={installationCode}&&Status=Ready";

            var response = await _echoApi.CallWebApiForAppAsync(
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
            >();
            if (echoMissions is null)
                throw new JsonException("Failed to deserialize missions from Echo");

            var missions = ProcessEchoMissions(echoMissions);

            return missions;
        }

        public async Task<EchoMission> GetMissionById(int missionId)
        {
            string relativePath = $"robots/robot-plan/{missionId}";

            var response = await _echoApi.CallWebApiForAppAsync(
                ServiceName,
                options =>
                {
                    options.HttpMethod = HttpMethod.Get;
                    options.RelativePath = relativePath;
                }
            );

            response.EnsureSuccessStatusCode();

            var echoMission = await response.Content.ReadFromJsonAsync<EchoMissionResponse>();

            if (echoMission is null)
                throw new JsonException("Failed to deserialize mission from Echo");

            var mission = ProcessEchoMission(echoMission);
            if (mission == null)
            {
                throw new InvalidDataException($"EchoMission with id: {missionId} is invalid.");
            }

            return mission;
        }

        public async Task<IList<EchoPlantInfo>> GetEchoPlantInfos()
        {
            string relativePath = "plantinfo";
            var response = await _echoApi.CallWebApiForAppAsync(
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
            >();
            if (echoPlantInfoResponse is null)
                throw new JsonException("Failed to deserialize plant information from Echo");
            var installations = ProcessEchoPlantInfos(echoPlantInfoResponse);
            return installations;
        }
        public async Task<EchoPoseResponse> GetRobotPoseFromPoseId(int poseId)
        {
            string relativePath = $"/robots/pose/{poseId}";
            var response = await _echoApi.CallWebApiForAppAsync(
                ServiceName,
                options =>
                {
                    options.HttpMethod = HttpMethod.Get;
                    options.RelativePath = relativePath;
                }
            );
            response.EnsureSuccessStatusCode();
            var echoPoseResponse = await response.Content.ReadFromJsonAsync<EchoPoseResponse>();
            if (echoPoseResponse is null)
            {
                throw new JsonException("Failed to deserialize robot pose from Echo");
            }
            return echoPoseResponse;
        }
        private static IList<EchoInspection> ProcessSensorTypes(List<SensorType> sensorTypes)
        {
            var inspections = new List<EchoInspection>();

            bool isEmpty = !sensorTypes.Any();
            if (isEmpty)
            {
                inspections.Add(new EchoInspection(InspectionTypeEnum.Image, null));
            }
            else
            {
                foreach (var sensorType in sensorTypes)
                {
                    var inspectionType = InspectionTypeFromString(sensorType.Key);
                    inspections.Add(
                        new EchoInspection(inspectionType, (float?)sensorType.TimeInSeconds)
                    );
                }
            }

            return inspections;
        }

        private List<EchoTag> ProcessPlanItems(List<PlanItem> planItems)
        {
            var tags = new List<EchoTag>();

            foreach (var planItem in planItems)
            {
                if (planItem.PoseId is null)
                {
                    string message = $"Invalid EchoMission: {planItem.Tag} has no associated pose id.";
                    _logger.LogError(message);
                    throw new InvalidDataException(message);

                }
                var robotPose = GetRobotPoseFromPoseId(planItem.PoseId.Value).Result;
                var tag = new EchoTag()
                {
                    Id = planItem.Id,
                    TagId = planItem.Tag,
                    PoseId = planItem.PoseId.Value,
                    Pose = new Pose(robotPose.Position, robotPose.LookDirectionNormalized, robotPose.TiltDegreesClockwise),
                    URL = new Uri(
                    $"https://stid.equinor.com/{_installationCode}/tag?tagNo={planItem.Tag}"
                ),
                    Inspections = ProcessSensorTypes(planItem.SensorTypes)
                };

                tags.Add(tag);
            }

            return tags;
        }

        private List<EchoMission> ProcessEchoMissions(List<EchoMissionResponse> echoMissions)
        {
            var missions = new List<EchoMission>();

            foreach (var echoMission in echoMissions)
            {
                var mission = ProcessEchoMission(echoMission);

                if (mission is null)
                    continue;

                missions.Add(mission);
            }

            return missions;
        }

        private EchoMission? ProcessEchoMission(EchoMissionResponse echoMission)
        {
            if (echoMission.PlanItems is null)
                throw new MissionNotFoundException("Mission has no tags");
            try
            {
                var mission = new EchoMission()
                {
                    Id = echoMission.Id,
                    Name = echoMission.Name,
                    AssetCode = echoMission.InstallationCode,
                    URL = new Uri($"https://echo.equinor.com/mp?editId={echoMission.Id}"),
                    Tags = ProcessPlanItems(echoMission.PlanItems)
                };
                return mission;
            }
            catch (InvalidDataException)
            {
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
                    continue;

                var echoPlantInfo = new EchoPlantInfo()
                {
                    InstallationCode = plant.InstallationCode,
                    ProjectDescription = plant.ProjectDescription
                };

                echoPlantInfos.Add(echoPlantInfo);
            }
            return echoPlantInfos;
        }
    }
}
