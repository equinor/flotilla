using System.Text;
using System.Text.Json;
using Api.Mqtt.Events;
using Api.Mqtt.MessageModels;
using Api.Utilities;
using MQTTnet;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using MQTTnet.Client.Receiving;
using MQTTnet.Extensions.ManagedClient;

namespace Api.Mqtt
{
    public class MqttService : BackgroundService
    {
        public static event EventHandler<MqttReceivedArgs>? MqttIsarMissionReceived;
        public static event EventHandler<MqttReceivedArgs>? MqttIsarTaskReceived;
        public static event EventHandler<MqttReceivedArgs>? MqttIsarStepReceived;

        private readonly ILogger<MqttService> _logger;

        private readonly IManagedMqttClient _mqttClient;

        private readonly ManagedMqttClientOptions _options;

        private readonly string _serverHost;
        private readonly int _serverPort;

        private readonly TimeSpan _reconnectDelay = TimeSpan.FromSeconds(5);

        public MqttService(ILogger<MqttService> logger, IConfiguration config)
        {
            _logger = logger;
            var mqttFactory = new MqttFactory();
            _mqttClient = mqttFactory.CreateManagedMqttClient();

            string password = config.GetValue<string>("mqtt-broker-password");

            var mqttConfig = config.GetSection("Mqtt");
            string username = mqttConfig.GetValue<string>("Username");
            _serverHost = mqttConfig.GetValue<string>("Host");
            _serverPort = mqttConfig.GetValue<int>("Port");

            var builder = new MqttClientOptionsBuilder()
                .WithTcpServer(_serverHost, _serverPort)
                .WithCredentials(username, password);

            _options = new ManagedMqttClientOptionsBuilder()
                .WithAutoReconnectDelay(_reconnectDelay)
                .WithClientOptions(builder.Build())
                .Build();

            RegisterCallbacks();

            var topics = mqttConfig.GetSection("Topics").Get<List<string>>();
            SubscribeToTopics(topics);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("MQTT client STARTED");
            await _mqttClient.StartAsync(_options);
            await stoppingToken;
            await _mqttClient.StopAsync();
            _logger.LogInformation("MQTT client STOPPED");
        }

        /// <summary>
        /// The callback function for when a subscribed topic publishes a message
        /// </summary>
        /// <param name="messageReceivedEvent"> The event information for the MQTT message </param>
        private void OnMessageReceived(MqttApplicationMessageReceivedEventArgs messageReceivedEvent)
        {
            string content = messageReceivedEvent.ApplicationMessage.ConvertPayloadToString();
            string topic = messageReceivedEvent.ApplicationMessage.Topic;

            var messageType = MqttTopics.TopicsToMessages.GetItemByTopic(topic);
            if (messageType is null)
            {
                _logger.LogError("No message class defined for topic '{topicName}'", topic);
                return;
            }

            _logger.LogInformation(
                "Topic: {topic} - Message recieved: \n{payload}",
                topic,
                content
            );

            switch (messageType)
            {
                case Type type when type == typeof(IsarMissionMessage):
                    OnIsarTopicReceived<IsarMissionMessage>(content);
                    break;
                case Type type when type == typeof(IsarTaskMessage):
                    OnIsarTopicReceived<IsarTaskMessage>(content);
                    break;
                case Type type when type == typeof(IsarStepMessage):
                    OnIsarTopicReceived<IsarStepMessage>(content);
                    break;
                default:
                    _logger.LogWarning(
                        "No callback defined for MQTT message type '{type}'",
                        messageType.Name
                    );
                    break;
            }
        }

        private void OnConnected(MqttClientConnectedEventArgs obj)
        {
            _logger.LogInformation(
                "Successfully connected to broker at {host}:{port}.",
                _serverHost,
                _serverPort
            );
        }

        private void OnConnectingFailed(ManagedProcessFailedEventArgs obj)
        {
            _logger.LogWarning(
                "Failed to connect to MQTT broker. Exception: {message}\n"
                    + "      Retrying in {time}s",
                obj.Exception.Message,
                _reconnectDelay.Seconds
            );
        }

        private void OnDisconnected(MqttClientDisconnectedEventArgs obj)
        {
            _logger.LogInformation(
                "Successfully disconnected from broker at {host}:{port}.",
                _serverHost,
                _serverPort
            );
        }

        private void RegisterCallbacks()
        {
            _mqttClient.ConnectedHandler = new MqttClientConnectedHandlerDelegate(OnConnected);
            _mqttClient.DisconnectedHandler = new MqttClientDisconnectedHandlerDelegate(
                OnDisconnected
            );
            _mqttClient.ConnectingFailedHandler = new ConnectingFailedHandlerDelegate(
                OnConnectingFailed
            );
            _mqttClient.ApplicationMessageReceivedHandler =
                new MqttApplicationMessageReceivedHandlerDelegate(OnMessageReceived);
        }

        public void SubscribeToTopics(List<string> topics)
        {
            List<MqttTopicFilter> topicFilters = new();
            StringBuilder sb = new();
            sb.AppendLine("Mqtt service subscribing to the following topics:");
            topics.ForEach(
                topic =>
                {
                    topicFilters.Add(new MqttTopicFilter() { Topic = topic });
                    sb.AppendLine(topic);
                }
            );
            _logger.LogInformation("{topicContent}", sb.ToString());
            _mqttClient.SubscribeAsync(topicFilters).Wait();
        }

        private void OnIsarTopicReceived<T>(string content) where T : MqttMessage
        {
            T? message;
            try
            {
                message = JsonSerializer.Deserialize<T>(content);
                if (message is null)
                    throw new JsonException();
            }
            catch (Exception ex)
                when (ex is JsonException || ex is NotSupportedException || ex is ArgumentException)
            {
                _logger.LogError(
                    "Could not create '{className}' object from MQTT message json",
                    typeof(T).Name
                );
                return;
            }

            var type = typeof(T);
            try
            {
                var raiseEvent = type switch
                {
                    _ when type == typeof(IsarMissionMessage) => MqttIsarMissionReceived,
                    _ when type == typeof(IsarTaskMessage) => MqttIsarTaskReceived,
                    _ when type == typeof(IsarStepMessage) => MqttIsarStepReceived,
                    _
                      => throw new NotImplementedException(
                          $"No event defined for message type '{typeof(T).Name}'"
                      ),
                };
                // Event will be null if there are no subscribers
                if (raiseEvent is not null)
                {
                    raiseEvent(this, new MqttReceivedArgs(message));
                }
            }
            catch (NotImplementedException e)
            {
                _logger.LogWarning("{msg}", e.Message);
            }
        }
    }
}
