using System.Threading.Tasks;
using Api.Database.Models;

namespace Api.Services
{
    public class MockStidService : IStidService
    {
        public const string ServiceName = "StidApi";

        public async Task<Position> GetTagPosition(string tag, string installationCode)
        {
            await Task.CompletedTask;
            return new Position(
                x: 0,
                y: 0,
                z: 0
            );
        }
    }
}
