using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Api.Database.Models;
using Api.Services;

namespace Api.Test.Mocks
{
    public class MockMapService : IMapService
    {
        public async Task<MissionMap> AssignMapToMission(string assetCode, List<PlannedTask> tasks)
        {
            await Task.Run(() => Thread.Sleep(1));
            return new MissionMap();
        }

        public async Task<string> FetchMapImage(string missionId)
        {
            await Task.Run(() => Thread.Sleep(1));
            return "filepath";
        }
    }
}
