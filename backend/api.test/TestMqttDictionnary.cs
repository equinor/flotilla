using System;
using System.Collections.Generic;
using Api.Mqtt;
using Xunit;

namespace Api.Test
{
    public class TestMqttDictionnary
    {
        private readonly Dictionary<string, string> _topics = new();

        public TestMqttDictionnary()
        {
            _topics["isar/+/task"] = "leveledTask";
            _topics["isar/task"] = "simpleTask";
            _topics["many/#"] = "wrong";
            _topics["many/specific"] = "wrong";
        }

        [Fact]
        public void ShouldErrorWithSeveralMatches()
        {
            string topic = "many/specific";
            Assert.Throws<InvalidOperationException>(() => _topics.GetItemByTopic(topic));
        }

        [Fact]
        public void ShouldMatchWildcardTopic()
        {
            string topic = "isar/extraLevel/task";
            string? value = _topics.GetItemByTopic(topic);

            Assert.NotNull(value);
            Assert.Equal("leveledTask", value);
        }

        [Fact]
        public void ShouldMatchSimpleTopic()
        {
            string topic = "isar/task";
            string? value = _topics.GetItemByTopic(topic);

            Assert.NotNull(value);
            Assert.Equal("simpleTask", value);
        }
    }
}
