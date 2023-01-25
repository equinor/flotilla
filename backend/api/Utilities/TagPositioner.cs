using System.Text.Json;
using static Api.Controllers.Models.IsarTaskDefinition;

namespace Api.Utilities
{
    public interface ITagPositioner
    {
        public abstract IsarPose GetPoseFromTag(string tagId);
    }

    public class TagPositioner : ITagPositioner
    {
        public TagPositioner() { }

        /// <summary>
        /// A placeholder method to be replaced by Unity algorithm in the future
        /// </summary>
        /// <param name="tagId"></param>
        /// <returns>The pose the robot should inspect the tag from</returns>
        public IsarPose GetPoseFromTag(string tagId)
        {
            using var r = new StreamReader("./Utilities/PredefinedPoses.json");
            string json = r.ReadToEnd();
            var predefinedPoses = JsonSerializer.Deserialize<List<PredefinedPose>>(json);

            if (predefinedPoses == null)
                throw new RobotPositionNotFoundException(
                    "Could not find any predefined poses in the workaround file"
                );

            var predefinedPose = predefinedPoses.Find(x => x.Tag == tagId);

            if (predefinedPose == null)
                throw new RobotPositionNotFoundException(
                    $"Could not find tag {tagId} in the workaround file"
                );

            return new IsarPose(predefinedPose);
        }
    }
}
