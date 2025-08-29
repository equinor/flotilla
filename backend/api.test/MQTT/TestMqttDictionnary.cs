using System;
using System.Collections.Generic;
using Api.Mqtt;
using Xunit;

namespace Api.Test.MQTT
{
    public class TestMqttDictionary
    {
        private readonly Dictionary<string, string> _topics = [];

        public TestMqttDictionary()
        {
            _topics["isar/+/task/+"] = "leveledTask";
            _topics["isar/task"] = "simpleTask";
            _topics["many/#"] = "wrong";
            _topics["many/specific"] = "wrong";
        }

        [Fact]
        public void ShouldErrorWithSeveralMatches()
        {
            const string Topic = "many/specific";
            Assert.Throws<InvalidOperationException>(() => _topics.GetItemByTopic(Topic));
        }

        [Fact]
        public void ShouldMatchWildcardTopic()
        {
            const string Topic = "isar/extraLevel/task/+";
            string? value = _topics.GetItemByTopic(Topic);

            Assert.NotNull(value);
            Assert.Equal("leveledTask", value);
        }

        [Fact]
        public void ShouldMatchSimpleTopic()
        {
            const string Topic = "isar/task";
            string? value = _topics.GetItemByTopic(Topic);

            Assert.NotNull(value);
            Assert.Equal("simpleTask", value);
        }
    }
}
