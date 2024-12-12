using System;
using System.Linq;
using System.Threading.Tasks;
using Api.Database.Context;
using Api.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Services
{
    public class MockStidService(FlotillaDbContext context) : IStidService
    {
        public const string ServiceName = "StidApi";

        public virtual async Task<Area?> GetTagArea(string tag, string installationCode)
        {
            await Task.CompletedTask;
            string testAreaName = "StidServiceMockArea";

            var area = context.Areas
                .Include(a => a.Deck).ThenInclude(d => d.Installation)
                .Include(a => a.Deck).ThenInclude(d => d.Plant).ThenInclude(p => p.Installation)
                .Include(d => d.Plant)
                .Include(i => i.Installation).Include(d => d.DefaultLocalizationPose)
                .Where(area => area.Name.Contains(testAreaName)).ToList().FirstOrDefault();
            if (area != null) { return area; }

            var testInstallation = new Installation
            {
                Id = Guid.NewGuid().ToString(),
                Name = "StidServiceMockInstallation",
                InstallationCode = "TTT"
            };

            var testPlant = new Plant
            {
                Id = Guid.NewGuid().ToString(),
                Installation = testInstallation,
                Name = "StidServiceMockPlant",
                PlantCode = "TTT"
            };

            var testDeck = new Deck
            {
                Id = Guid.NewGuid().ToString(),
                Plant = testPlant,
                Installation = testPlant.Installation,
                DefaultLocalizationPose = new DefaultLocalizationPose(),
                Name = "StidServiceMockDeck"
            };

            var testArea = new Area
            {
                Id = Guid.NewGuid().ToString(),
                Deck = testDeck,
                Plant = testDeck.Plant,
                Installation = testDeck.Plant.Installation,
                Name = testAreaName,
                MapMetadata = new MapMetadata(),
                DefaultLocalizationPose = new DefaultLocalizationPose(),
            };

            context.Add(testInstallation);
            context.Add(testPlant);
            context.Add(testDeck);
            context.Add(testArea);

            await context.SaveChangesAsync();

            return testArea;
        }
    }
}
