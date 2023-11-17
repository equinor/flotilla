using Api.Mqtt.MessageModels;

namespace Api.Mqtt.Events
{
    public class MqttReceivedArgs(MqttMessage message) : EventArgs
    {
        public MqttMessage Message { get; } = message;
    }
}
