using System.Text.Json;
using Api.Controllers.Models;
using Api.Utilities;
using Microsoft.Identity.Web;
using static Api.Database.Models.IsarStep;

namespace Api.Services
{
    public interface IMapService
    {
        public abstract Task<String> GetMap();
    }
    public class MapService: IMapService
    {
        public async Task<String> GetMap()
        {
            return "Hello World";
        }
    }
}