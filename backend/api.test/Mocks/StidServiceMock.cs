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
                .Include(a => a.InspectionArea).ThenInclude(d => d.Installation)
                .Include(a => a.InspectionArea).ThenInclude(d => d.Plant).ThenInclude(p => p.Installation)
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

            var testInspectionArea = new InspectionArea
            {
                Id = Guid.NewGuid().ToString(),
                Plant = testPlant,
                Installation = testPlant.Installation,
                DefaultLocalizationPose = new DefaultLocalizationPose(),
                Name = "StidServiceMockInspectionArea"
            };

            var testArea = new Area
            {
                Id = Guid.NewGuid().ToString(),
                InspectionArea = testInspectionArea,
                Plant = testInspectionArea.Plant,
                Installation = testInspectionArea.Plant.Installation,
                Name = testAreaName,
                MapMetadata = new MapMetadata(),
                DefaultLocalizationPose = new DefaultLocalizationPose(),
            };

            context.Add(testInstallation);
            context.Add(testPlant);
            context.Add(testInspectionArea);
            context.Add(testArea);

            await context.SaveChangesAsync();

            return testArea;
        }
    }
}
