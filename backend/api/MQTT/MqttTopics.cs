using System.Text.RegularExpressions;
using Api.Mqtt.MessageModels;

namespace Api.Mqtt
{
    /// <summary>
    ///     This class contains a list of topics linked to their message models
    /// </summary>
    public static class MqttTopics
    {
        /// <summary>
        ///     A dictionary linking MQTT topics to their respective message models
        /// </summary>
        public static readonly Dictionary<string, Type> TopicsToMessages =
            new()
            {
                { "isar/+/status", typeof(IsarStatusMessage) },
                { "isar/+/robot_info", typeof(IsarRobotInfoMessage) },
                { "isar/+/robot_heartbeat", typeof(IsarRobotHeartbeatMessage) },
                { "isar/+/aborted_mission", typeof(IsarMissionAbortedMessage) },
                { "isar/+/mission/+", typeof(IsarMissionMessage) },
                { "isar/+/task/+", typeof(IsarTaskMessage) },
                { "isar/+/battery", typeof(IsarBatteryMessage) },
                { "isar/+/pressure", typeof(IsarPressureMessage) },
                { "isar/+/pose", typeof(IsarPoseMessage) },
                { "isar/+/cloud_health", typeof(IsarCloudHealthMessage) },
                { "isar/+/intervention_needed", typeof(IsarInterventionNeededMessage) },
                { "isar/+/startup", typeof(IsarStartupMessage) },
                { "sara/visualization_available", typeof(SaraInspectionResultMessage) },
                { "sara/analysis_result_available", typeof(SaraAnalysisResultMessage) },
            };

        /// <summary>
        ///     Searches a dictionary for a specific topic name and returns the corresponding value from the wildcarded dictionary
        /// </summary>
        /// <remarks>
        ///     Will throw <see cref="InvalidOperationException"></see> if there are more than one matches for the topic.
        /// </remarks>
        /// <typeparam name="T"></typeparam>
        /// <param name="dict"></param>
        /// <param name="topic"></param>
        /// <returns>
        ///     The value corresponding to the wildcarded topic
        /// </returns>
        /// <exception cref="InvalidOperationException">There was more than one topic pattern matching the provided topic</exception>
        public static T? GetItemByTopic<T>(this Dictionary<string, T> dict, string topic)
        {
            return (
                from p in dict
                where Regex.IsMatch(topic, ConvertTopicToRegex(p.Key))
                select p.Value
            ).SingleOrDefault();
        }

        /// <summary>
        ///     Converts from mqtt topic wildcards to the corresponding
        ///     regex expression to allow for searching the dictionary for the topic of a message
        /// </summary>
        /// <param name="topic"></param>
        /// <returns></returns>
        private static string ConvertTopicToRegex(string topic)
        {
            return topic.Replace('#', '*').Replace("+", "[^/\n]*", StringComparison.Ordinal);
        }
    }
}
