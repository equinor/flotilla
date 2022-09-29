using System.Text.Json;
using Api.Controllers.Models;
using Api.Services;
using static Api.Controllers.Models.IsarTaskDefinition;
namespace Api.Utilities
{
    public interface ITagPositioner
    {
        public abstract IsarPose GetPoseFromTag(EchoTag tag);
        public abstract Task<IsarPosition> GetTagPositionFromTag(EchoTag tag);
    }
    public class TagPositioner : ITagPositioner
    {
        private readonly IStidService _stidService;

        public TagPositioner(IStidService stidService)
        {
            _stidService = stidService;
        }

        /// <summary>
        /// A placeholder method to be replaced by Unity algorithm in the future
        /// </summary>
        /// <param name="tag"></param>
        /// <returns>The pose the robot should inspect the tag from</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter",
            Justification = "Will be implemented, is here for documentation of intended behavior")]
        public IsarPose GetPoseFromTag(EchoTag tag)
        {
            using var r = new StreamReader("./Utilities/PredefinedPoses.json");
            string json = r.ReadToEnd();
            var predefinedPoses = JsonSerializer.Deserialize<List<PredefinedPose>>(json);

            if (predefinedPoses == null)
                throw new RobotPositionNotFoundException("Could not find any predefined poses in the workaround file");


            var predefinedPose = predefinedPoses.Find(x => x.Tag == tag.TagId);

            if (predefinedPose == null)
                throw new RobotPositionNotFoundException($"Could not find tag {tag.TagId} in the workaround file");


            return new IsarPose(predefinedPose);

        }

        public async Task<IsarPosition> GetTagPositionFromTag(EchoTag tag)
        {
            var tagPosition = await _stidService.GetTagPosition(tag.TagId);
            const string Frame = "asset";
            return new IsarPosition(tagPosition.X, tagPosition.Y, tagPosition.Z, Frame);
        }
    }
}
