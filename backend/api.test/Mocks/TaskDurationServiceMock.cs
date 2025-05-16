using System.Threading;
using System.Threading.Tasks;
using Api.Database.Models;
using Api.Services.ActionServices;

namespace Api.Test.Mocks
{
    public class MockTaskDurationService : ITaskDurationService
    {
        public async Task UpdateAverageDurationPerTask(RobotType robotType)
        {
            await Task.Run(() => Thread.Sleep(1));
        }
    }
}
