using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Api.Controllers.Models;
using Api.Database.Models;
using Api.Test.Mocks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
namespace Api.Test
{
    [Collection("Database collection")]
    public class RoleAccessTests : IClassFixture<TestWebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;
        private readonly MockHttpContextAccessor _httpContextAccessor;
        private readonly JsonSerializerOptions _serializerOptions =
            new()
            {
                Converters =
                {
                    new JsonStringEnumConverter()
                },
                PropertyNameCaseInsensitive = true
            };

        public RoleAccessTests(TestWebApplicationFactory<Program> factory)
        {
            _httpContextAccessor = (MockHttpContextAccessor)factory.Services.GetService<IHttpContextAccessor>()!;
            _httpContextAccessor.SetHttpContextRoles(["Role.Admin"]);
            //var x = new HttpContextAccessor();
            _client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                BaseAddress = new Uri("https://localhost:8000")
            });
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                TestAuthHandler.AuthenticationScheme
            );
        }

        [Fact]
        public async Task AuthorisedGetPlantTest_NotFound()
        {
            // Arrange
            var accessRoleQuery = new CreateAccessRoleQuery
            {
                InstallationCode = "AuthorisedGetPlantTest_NotFoundInstallation",
                RoleName = "User.AuthorisedGetPlantTest_NotFoundInstallation",
                AccessLevel = RoleAccessLevel.USER
            };
            var accessRoleContent = new StringContent(
                JsonSerializer.Serialize(accessRoleQuery),
                null,
                "application/json"
            );

            var testPose = new Pose
            {
                Position = new Position
                {
                    X = 1,
                    Y = 2,
                    Z = 2
                },
                Orientation = new Orientation
                {
                    X = 0,
                    Y = 0,
                    Z = 0,
                    W = 1
                }
            };

            string testInstallation = "AuthorisedGetPlantTest_NotFoundInstallation";
            var installationQuery = new CreateInstallationQuery
            {
                InstallationCode = testInstallation,
                Name = testInstallation
            };

            string testPlant = "AuthorisedGetPlantTest_NotFoundPlant";
            var plantQuery = new CreatePlantQuery
            {
                InstallationCode = testInstallation,
                PlantCode = testPlant,
                Name = testPlant
            };

            var installationContent = new StringContent(
                JsonSerializer.Serialize(installationQuery),
                null,
                "application/json"
            );

            var plantContent = new StringContent(
                JsonSerializer.Serialize(plantQuery),
                null,
                "application/json"
            );

            // Act
            string installationUrl = "/installations";
            var installationResponse = await _client.PostAsync(installationUrl, installationContent);
            string accessRoleUrl = "/access-roles";
            var accessRoleResponse = await _client.PostAsync(accessRoleUrl, accessRoleContent);
            string plantUrl = "/plants";
            var plantResponse = await _client.PostAsync(plantUrl, plantContent);

            // Only restrict ourselves to non-admin role after doing POSTs
            _httpContextAccessor.SetHttpContextRoles(["User.TestInstallationAreaTest_Wrong"]);

            // Assert
            Assert.True(accessRoleResponse.IsSuccessStatusCode);
            Assert.True(installationResponse.IsSuccessStatusCode);
            Assert.True(plantResponse.IsSuccessStatusCode);

            var plant = await plantResponse.Content.ReadFromJsonAsync<Plant>(_serializerOptions);
            Assert.True(plant != null);

            // Act
            string getPlantUrl = $"/plants/{plant.Id}";
            var samePlantResponse = await _client.GetAsync(getPlantUrl);

            // Assert
            Assert.False(samePlantResponse.IsSuccessStatusCode);
            Assert.Equal("NotFound", samePlantResponse.StatusCode.ToString());
        }

        [Fact]
        public async Task ExplicitlyAuthorisedPostInstallationPlantDeckAndAreaTest()
        {
            // Arrange
            var accessRoleQuery = new CreateAccessRoleQuery
            {
                InstallationCode = "ExplicitlyAuthorisedPostInstallationPlantDeckAndAreaTestInstallation",
                RoleName = "User.ExplicitlyAuthorisedPostInstallationPlantDeckAndAreaTestInstallation",
                AccessLevel = RoleAccessLevel.USER
            };
            var accessRoleContent = new StringContent(
                JsonSerializer.Serialize(accessRoleQuery),
                null,
                "application/json"
            );

            var testPose = new Pose
            {
                Position = new Position
                {
                    X = 1,
                    Y = 2,
                    Z = 2
                },
                Orientation = new Orientation
                {
                    X = 0,
                    Y = 0,
                    Z = 0,
                    W = 1
                }
            };

            string testInstallation = "ExplicitlyAuthorisedPostInstallationPlantDeckAndAreaTestInstallation";
            var installationQuery = new CreateInstallationQuery
            {
                InstallationCode = testInstallation,
                Name = testInstallation
            };

            string testPlant = "ExplicitlyAuthorisedPostInstallationPlantDeckAndAreaTestPlant";
            var plantQuery = new CreatePlantQuery
            {
                InstallationCode = testInstallation,
                PlantCode = testPlant,
                Name = testPlant
            };

            string testDeck = "ExplicitlyAuthorisedPostInstallationPlantDeckAndAreaTestDeck";
            var deckQuery = new CreateDeckQuery
            {
                InstallationCode = testInstallation,
                PlantCode = testPlant,
                Name = testDeck
            };

            string testArea = "ExplicitlyAuthorisedPostInstallationPlantDeckAndAreaTestArea";
            var areaQuery = new CreateAreaQuery
            {
                InstallationCode = testInstallation,
                PlantCode = testPlant,
                DeckName = testDeck,
                AreaName = testArea,
                DefaultLocalizationPose = testPose
            };

            var installationContent = new StringContent(
                JsonSerializer.Serialize(installationQuery),
                null,
                "application/json"
            );

            var plantContent = new StringContent(
                JsonSerializer.Serialize(plantQuery),
                null,
                "application/json"
            );

            var deckContent = new StringContent(
                JsonSerializer.Serialize(deckQuery),
                null,
                "application/json"
            );

            var areaContent = new StringContent(
                JsonSerializer.Serialize(areaQuery),
                null,
                "application/json"
            );

            // Act
            string installationUrl = "/installations";
            var installationResponse = await _client.PostAsync(installationUrl, installationContent);
            string accessRoleUrl = "/access-roles";
            var accessRoleResponse = await _client.PostAsync(accessRoleUrl, accessRoleContent);
            string plantUrl = "/plants";
            var plantResponse = await _client.PostAsync(plantUrl, plantContent);
            string deckUrl = "/decks";
            var deckResponse = await _client.PostAsync(deckUrl, deckContent);
            string areaUrl = "/areas";
            var areaResponse = await _client.PostAsync(areaUrl, areaContent);

            // Only restrict ourselves to non-admin role after doing POSTs
            _httpContextAccessor.SetHttpContextRoles(["User.ExplicitlyAuthorisedPostInstallationPlantDeckAndAreaTestInstallation"]);

            // Assert
            Assert.True(accessRoleResponse.IsSuccessStatusCode);
            Assert.True(installationResponse.IsSuccessStatusCode);
            Assert.True(plantResponse.IsSuccessStatusCode);
            Assert.True(deckResponse.IsSuccessStatusCode);
            Assert.True(areaResponse.IsSuccessStatusCode);
            var area = await areaResponse.Content.ReadFromJsonAsync<AreaResponse>(_serializerOptions);
            Assert.True(area != null);

            // Act
            string getAreaUrl = $"/areas/{area.Id}";
            var sameAreaResponse = await _client.GetAsync(getAreaUrl);

            // Assert
            Assert.True(sameAreaResponse.IsSuccessStatusCode);
            var sameArea = await sameAreaResponse.Content.ReadFromJsonAsync<AreaResponse>(_serializerOptions);
            Assert.True(sameArea != null);
            Assert.Equal(sameArea.Id, area.Id);
        }

        [Fact]
        public async Task AdminAuthorisedPostInstallationPlantDeckAndAreaTest()
        {
            // Arrange
            var testPose = new Pose
            {
                Position = new Position
                {
                    X = 1,
                    Y = 2,
                    Z = 2
                },
                Orientation = new Orientation
                {
                    X = 0,
                    Y = 0,
                    Z = 0,
                    W = 1
                }
            };

            string testInstallation = "AdminAuthorisedPostInstallationPlantDeckAndAreaTestInstallation";
            var installationQuery = new CreateInstallationQuery
            {
                InstallationCode = testInstallation,
                Name = testInstallation
            };

            string testPlant = "AdminAuthorisedPostInstallationPlantDeckAndAreaTestPlant";
            var plantQuery = new CreatePlantQuery
            {
                InstallationCode = testInstallation,
                PlantCode = testPlant,
                Name = testPlant
            };

            string testDeck = "AdminAuthorisedPostInstallationPlantDeckAndAreaTestDeck";
            var deckQuery = new CreateDeckQuery
            {
                InstallationCode = testInstallation,
                PlantCode = testPlant,
                Name = testDeck
            };

            string testArea = "AdminAuthorisedPostInstallationPlantDeckAndAreaTestArea";
            var areaQuery = new CreateAreaQuery
            {
                InstallationCode = testInstallation,
                PlantCode = testPlant,
                DeckName = testDeck,
                AreaName = testArea,
                DefaultLocalizationPose = testPose
            };

            var installationContent = new StringContent(
                JsonSerializer.Serialize(installationQuery),
                null,
                "application/json"
            );

            var plantContent = new StringContent(
                JsonSerializer.Serialize(plantQuery),
                null,
                "application/json"
            );

            var deckContent = new StringContent(
                JsonSerializer.Serialize(deckQuery),
                null,
                "application/json"
            );

            var areaContent = new StringContent(
                JsonSerializer.Serialize(areaQuery),
                null,
                "application/json"
            );

            // Act
            string installationUrl = "/installations";
            var installationResponse = await _client.PostAsync(installationUrl, installationContent);
            string plantUrl = "/plants";
            var plantResponse = await _client.PostAsync(plantUrl, plantContent);
            string deckUrl = "/decks";
            var deckResponse = await _client.PostAsync(deckUrl, deckContent);
            string areaUrl = "/areas";
            var areaResponse = await _client.PostAsync(areaUrl, areaContent);

            // Assert
            Assert.True(installationResponse.IsSuccessStatusCode);
            Assert.True(plantResponse.IsSuccessStatusCode);
            Assert.True(deckResponse.IsSuccessStatusCode);
            Assert.True(areaResponse.IsSuccessStatusCode);
            var area = await areaResponse.Content.ReadFromJsonAsync<AreaResponse>(_serializerOptions);
            Assert.True(area != null);
        }
    }
}
