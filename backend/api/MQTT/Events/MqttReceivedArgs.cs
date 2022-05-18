using Api.Mqtt.MessageModels;

namespace Api.Mqtt.Events
{
    public class MqttReceivedArgs : EventArgs
    {
        public MqttMessage Message { get; set; }

        public MqttReceivedArgs(MqttMessage message)
        {
            Message = message;
        }
    }
}
