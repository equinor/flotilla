using System.Text.Json;
using Api.Controllers.Models;
using static Api.Controllers.Models.IsarTaskDefinition;
namespace Api.Utilities
{
    public class TagPositioner
    {
        /// <summary>
        /// A placeholder method to be replaced by Unity algorithm in the future
        /// </summary>
        /// <param name="tag"></param>
        /// <returns>The pose the robot should inspect the tag from</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter",
            Justification = "Will be implemented, is here for documentation of intended behavior")]
        public static IsarPose GetPoseFromTag(EchoTag tag)
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

        /// <summary>
        /// A placeholder method to get tag position from echo/3D model
        /// </summary>
        /// <param name="tag"></param>
        /// <returns>The position of the tag on the asset</returns>
        ///
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter",
            Justification = "Will be implemented, is here for documentation of intended behavior")]
        public static IsarPosition GetTagPositionFromTag(EchoTag tag)
        {
            const string Frame = "asset";
            return new IsarPosition(0, 0, 0, Frame);
        }
    }
}
