using System.Net.Http.Headers;
using System.Text.Json;
using System.Web;
using Api.Models;
using Api.Utilities;
using Azure.Core;
using Azure.Identity;
using Database.Models;

namespace Api.Services
{
    public class EchoService
    {
        private readonly DefaultAzureCredential _credentials;
        private readonly IConfiguration _config;
        private static readonly HttpClient client = new();
        private readonly string _echoApiUrl;
        private readonly string[] _requestScope;
        private readonly string _installationCode;

        public EchoService(DefaultAzureCredential credentials, IConfiguration config)
        {
            _credentials = credentials;
            _config = config;

            _echoApiUrl = _config.GetSection("Echo").GetValue<string>("ApiUrl");
            string echoAppScope = _config.GetSection("Echo").GetValue<string>("AppScope");
            string echoClientId = _config.GetSection("Echo").GetValue<string>("ClientId");
            _installationCode = config.GetValue<string>("InstallationCode");

            _requestScope = new string[1] { $"{echoClientId}/{echoAppScope}" };
        }

        public async Task<IList<Mission>> GetMissions()
        {
            var accessToken = await AcquireAccessToken();
            ConfigureRequest(accessToken);
            var uri = ConstructGetMissionsRequestUri();

            var response = await client.GetAsync(uri);
            if (!response.IsSuccessStatusCode)
            {
                throw new MissionNotFoundException(
                    $"Failed to retrieve missions from Echo: {response.ReasonPhrase}"
                );
            }

            var echoMissions = await response.Content.ReadFromJsonAsync<
                List<EchoMissionResponse>
            >();

            if (echoMissions is null)
                throw new JsonException("Failed to deserialize missions from Echo");

            var missions = ProcessEchoMissions(echoMissions);
            return missions;
        }

        public async Task<Mission> GetMission(int missionId)
        {
            var accessToken = await AcquireAccessToken();
            ConfigureRequest(accessToken);
            var uri = ConstructGetMissionRequestUri(missionId);

            var response = await client.GetAsync(uri);
            if (!response.IsSuccessStatusCode)
            {
                throw new MissionNotFoundException(
                    $"Failed to retrieve mission with ID: {missionId} from Echo: {response.ReasonPhrase}"
                );
            }

            var echoMission = await response.Content.ReadFromJsonAsync<EchoMissionResponse>();

            if (echoMission is null)
                throw new JsonException("Failed to deserialize mission from Echo");

            var mission = ProcessEchoMission(echoMission);
            return mission;
        }

        private async Task<AccessToken> AcquireAccessToken()
        {
            var context = new TokenRequestContext(_requestScope);
            var accessToken = await _credentials.GetTokenAsync(requestContext: context);
            return accessToken;
        }

        private static void ConfigureRequest(AccessToken accessToken)
        {
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json")
            );
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                accessToken.Token
            );
        }

        private Uri ConstructGetMissionsRequestUri()
        {
            var builder = new UriBuilder($"{_echoApiUrl}/robots/robot-plan");
            var query = HttpUtility.ParseQueryString(builder.Query);
            query["InstallationCode"] = _installationCode;
            builder.Query.ToString();
            var uri = new Uri(builder.ToString());

            return uri;
        }

        private Uri ConstructGetMissionRequestUri(int missionId)
        {
            var builder = new UriBuilder($"{_echoApiUrl}/robots/robot-plan/{missionId}");
            var uri = new Uri(builder.ToString());

            return uri;
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

        private List<Tag> ProcessPlanItems(List<PlanItem> planItems)
        {
            var tags = new List<Tag>();

            foreach (var planItem in planItems)
            {
                var tag = new Tag()
                {
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

        private List<Mission> ProcessEchoMissions(List<EchoMissionResponse> echoMissions)
        {
            var missions = new List<Mission>();

            foreach (var echoMission in echoMissions)
            {
                var mission = ProcessEchoMission(echoMission);

                if (mission is null)
                    continue;

                missions.Add(ProcessEchoMission(echoMission));
            }

            return missions;
        }

        private Mission ProcessEchoMission(EchoMissionResponse echoMission)
        {
            if (echoMission.PlanItems is null)
                throw new MissionNotFoundException("Mission has no tags");

            var mission = new Mission()
            {
                Name = echoMission.Name,
                URL = new Uri($"https://echo.equinor.com/mp?editId={echoMission.Id}"),
                Tags = ProcessPlanItems(echoMission.PlanItems)
            };

            return mission;
        }
    }
}
