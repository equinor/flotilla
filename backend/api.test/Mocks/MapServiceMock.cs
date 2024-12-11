using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Api.Database.Models;
using Api.Services;

namespace Api.Test.Mocks
{
    public class MockMapService : IMapService
    {
        public async Task<MapMetadata?> ChooseMapFromPositions(IList<Position> positions, string installationCode)
        {
            await Task.Run(() => Thread.Sleep(1));
            return new MapMetadata();
        }

        public async Task<MapMetadata?> ChooseMapFromMissionRunTasks(MissionRun mission)
        {
            await Task.Run(() => Thread.Sleep(1));
            return new MapMetadata();
        }

        public async Task<byte[]> FetchMapImage(string mapName, string installationCode)
        {
            await Task.Run(() => Thread.Sleep(1));
            string filePath = Directory.GetCurrentDirectory() + "Images/MockMapImage.png";
            return File.ReadAllBytes(filePath);
        }
    }
}
