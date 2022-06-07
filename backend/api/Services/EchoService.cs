using System.Text.Json;
using Api.Controllers.Models;
using Api.Database.Models;
using Api.Utilities;
using Microsoft.Identity.Web;

namespace Api.Services
{
    public class EchoService
    {
        public const string ServiceName = "EchoApi";
        private readonly IDownstreamWebApi _echoApi;
        private readonly string _installationCode;

        public EchoService(IConfiguration config, IDownstreamWebApi downstreamWebApi)
        {
            _echoApi = downstreamWebApi;
            _installationCode = config.GetValue<string>("InstallationCode");
        }

        public async Task<IList<EchoMission>> GetMissions()
        {
            string relativePath = $"robots/robot-plan?InstallationCode={_installationCode}";

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
            string relativePath =
                $"robots/robot-plan/{missionId}?InstallationCode={_installationCode}";

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
            return mission;
        }

        private static IList<InspectionType> ProcessSensorTypes(List<SensorType> sensorTypes)
        {
            var inspectionTypes = new List<InspectionType>();

            bool isEmpty = !sensorTypes.Any();
            if (isEmpty)
            {
                inspectionTypes.Add(InspectionType.Image);
            }
            else
            {
                foreach (var sensorType in sensorTypes)
                {
                    var inspectionType = SelectInspectionType.FromSensorTypeAsString(
                        sensorType.Key
                    );
                    inspectionTypes.Add(inspectionType);
                }
            }

            return inspectionTypes;
        }

        private List<EchoTag> ProcessPlanItems(List<PlanItem> planItems)
        {
            var tags = new List<EchoTag>();

            foreach (var planItem in planItems)
            {
                var tag = new EchoTag()
                {
                    Id = planItem.Id,
                    TagId = planItem.Tag,
                    URL = new Uri(
                        $"https://stid.equinor.com/{_installationCode}/tag?tagNo={planItem.Tag}"
                    ),
                    InspectionTypes = ProcessSensorTypes(planItem.SensorTypes)
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

                missions.Add(ProcessEchoMission(echoMission));
            }

            return missions;
        }

        private EchoMission ProcessEchoMission(EchoMissionResponse echoMission)
        {
            if (echoMission.PlanItems is null)
                throw new MissionNotFoundException("Mission has no tags");

            var mission = new EchoMission()
            {
                Id = echoMission.Id,
                Name = echoMission.Name,
                URL = new Uri($"https://echo.equinor.com/mp?editId={echoMission.Id}"),
                Tags = ProcessPlanItems(echoMission.PlanItems)
            };

            return mission;
        }
    }
}
