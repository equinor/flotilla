using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Api.Mqtt.Events;
using Api.Mqtt.MessageModels;
using Api.Utilities;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Packets;

namespace Api.Mqtt
{
    public class MqttService : BackgroundService
    {
        private readonly ILogger<MqttService> _logger;
        private readonly int _maxRetryAttempts;

        private readonly IManagedMqttClient _mqttClient;

        private readonly ManagedMqttClientOptions _options;

        private readonly TimeSpan _reconnectDelay = TimeSpan.FromSeconds(5);

        //private readonly bool _notProduction;

        private readonly string _serverHost;
        private readonly int _serverPort;
        private readonly bool _shouldFailOnMaxRetries;

        private static readonly JsonSerializerOptions serializerOptions =
            new() { Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) } };

        private CancellationToken _cancellationToken;
        private int _reconnectAttempts;

        public MqttService(ILogger<MqttService> logger, IConfiguration config)
        {
            _reconnectAttempts = 0;
            _logger = logger;
            var mqttFactory = new MqttFactory();
            _mqttClient = mqttFactory.CreateManagedMqttClient();

            /*_notProduction = !(
                config.GetValue<string?>("ASPNETCORE_ENVIRONMENT") ?? "Production"
            ).Equals("Production", StringComparison.OrdinalIgnoreCase);*/

            var mqttConfig = config.GetSection("Mqtt");
            string password = mqttConfig.GetValue<string>("Password") ?? "";
            string username = mqttConfig.GetValue<string>("Username") ?? "";
            _serverHost = mqttConfig.GetValue<string>("Host") ?? "";
            _serverPort = mqttConfig.GetValue<int>("Port");
            _maxRetryAttempts = mqttConfig.GetValue<int>("MaxRetryAttempts");
            _shouldFailOnMaxRetries = mqttConfig.GetValue<bool>("ShouldFailOnMaxRetries");

            var tlsOptions = new MqttClientTlsOptions
            {
                UseTls = true,
                /* Currently disabled to use self-signed certificate in the internal broker communication */
                //if (_notProduction)
                IgnoreCertificateChainErrors = true,
            };
            var builder = new MqttClientOptionsBuilder()
                .WithTcpServer(_serverHost, _serverPort)
                .WithTlsOptions(tlsOptions)
                .WithCredentials(username, password);

            _options = new ManagedMqttClientOptionsBuilder()
                .WithAutoReconnectDelay(_reconnectDelay)
                .WithClientOptions(builder.Build())
                .Build();

            RegisterCallbacks();

            var topics = mqttConfig.GetSection("Topics").Get<List<string>>() ?? [];
            SubscribeToTopics(topics);
        }

        public static event EventHandler<MqttReceivedArgs>? MqttIsarStatusReceived;
        public static event EventHandler<MqttReceivedArgs>? MqttIsarRobotInfoReceived;
        public static event EventHandler<MqttReceivedArgs>? MqttIsarRobotHeartbeatReceived;
        public static event EventHandler<MqttReceivedArgs>? MqttIsarMissionReceived;
        public static event EventHandler<MqttReceivedArgs>? MqttIsarTaskReceived;
        public static event EventHandler<MqttReceivedArgs>? MqttIsarBatteryReceived;
        public static event EventHandler<MqttReceivedArgs>? MqttIsarPressureReceived;
        public static event EventHandler<MqttReceivedArgs>? MqttIsarPoseReceived;
        public static event EventHandler<MqttReceivedArgs>? MqttIsarCloudHealthReceived;
        public static event EventHandler<MqttReceivedArgs>? MqttIdaInspectionResultReceived;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _cancellationToken = stoppingToken;
            _logger.LogInformation("MQTT client STARTED");
            await _mqttClient.StartAsync(_options);
            await _cancellationToken;
            await _mqttClient.StopAsync();
            _logger.LogInformation("MQTT client STOPPED");
        }

        /// <summary>
        ///     The callback function for when a subscribed topic publishes a message
        /// </summary>
        /// <param name="messageReceivedEvent"> The event information for the MQTT message </param>
        private Task OnMessageReceived(MqttApplicationMessageReceivedEventArgs messageReceivedEvent)
        {
            string content = messageReceivedEvent.ApplicationMessage.ConvertPayloadToString();
            string topic = messageReceivedEvent.ApplicationMessage.Topic;

            var messageType = MqttTopics.TopicsToMessages.GetItemByTopic(topic);
            if (messageType is null)
            {
                _logger.LogError("No message class defined for topic '{topicName}'", topic);
                return Task.CompletedTask;
            }

            _logger.LogDebug("Topic: {topic} - Message received: \n{payload}", topic, content);

            switch (messageType)
            {
                case Type type when type == typeof(IsarStatusMessage):
                    OnIsarTopicReceived<IsarStatusMessage>(content);
                    break;
                case Type type when type == typeof(IsarRobotInfoMessage):
                    OnIsarTopicReceived<IsarRobotInfoMessage>(content);
                    break;
                case Type type when type == typeof(IsarRobotHeartbeatMessage):
                    OnIsarTopicReceived<IsarRobotHeartbeatMessage>(content);
                    break;
                case Type type when type == typeof(IsarMissionMessage):
                    OnIsarTopicReceived<IsarMissionMessage>(content);
                    break;
                case Type type when type == typeof(IsarTaskMessage):
                    OnIsarTopicReceived<IsarTaskMessage>(content);
                    break;
                case Type type when type == typeof(IsarBatteryMessage):
                    OnIsarTopicReceived<IsarBatteryMessage>(content);
                    break;
                case Type type when type == typeof(IsarPressureMessage):
                    OnIsarTopicReceived<IsarPressureMessage>(content);
                    break;
                case Type type when type == typeof(IsarPoseMessage):
                    OnIsarTopicReceived<IsarPoseMessage>(content);
                    break;
                case Type type when type == typeof(IsarCloudHealthMessage):
                    OnIsarTopicReceived<IsarCloudHealthMessage>(content);
                    break;
                case Type type when type == typeof(IdaInspectionResultMessage):
                    OnIdaTopicReceived<IdaInspectionResultMessage>(content);
                    break;
                default:
                    _logger.LogWarning(
                        "No callback defined for MQTT message type '{type}'",
                        messageType.Name
                    );
                    break;
            }

            return Task.CompletedTask;
        }

        private Task OnConnected(MqttClientConnectedEventArgs obj)
        {
            _logger.LogInformation(
                "Successfully connected to broker at {host}:{port}.",
                _serverHost,
                _serverPort
            );
            _reconnectAttempts = 0;

            return Task.CompletedTask;
        }

        private Task OnConnectingFailed(ConnectingFailedEventArgs obj)
        {
            if (_reconnectAttempts == -1)
            {
                return Task.CompletedTask;
            }

            string errorMsg =
                "Failed to connect to MQTT broker. Exception: " + obj.Exception.Message;

            if (_reconnectAttempts >= _maxRetryAttempts)
            {
                _logger.LogError("{errorMsg}\n      Exceeded max reconnect attempts.", errorMsg);

                if (_shouldFailOnMaxRetries)
                {
                    _logger.LogError("Stopping MQTT client due to critical failure");
                    StopAsync(_cancellationToken);
                    return Task.CompletedTask;
                }

                _reconnectAttempts = -1;
                return Task.CompletedTask;
            }

            _reconnectAttempts++;
            _logger.LogWarning(
                "{errorMsg}\n      Retrying in {time}s ({attempt}/{maxAttempts})",
                errorMsg,
                _reconnectDelay.Seconds,
                _reconnectAttempts,
                _maxRetryAttempts
            );
            return Task.CompletedTask;
        }

        private Task OnDisconnected(MqttClientDisconnectedEventArgs obj)
        {
            // Only log a disconnect if previously connected (not on reconnect attempt)
            if (obj.ClientWasConnected)
            {
                if (obj.Reason is MqttClientDisconnectReason.NormalDisconnection)
                {
                    _logger.LogInformation(
                        "Successfully disconnected from broker at {host}:{port}",
                        _serverHost,
                        _serverPort
                    );
                }
                else
                {
                    _logger.LogWarning(
                        "Lost connection to broker at {host}:{port}",
                        _serverHost,
                        _serverPort
                    );
                }
            }

            return Task.CompletedTask;
        }

        private void RegisterCallbacks()
        {
            _mqttClient.ConnectedAsync += OnConnected;
            _mqttClient.DisconnectedAsync += OnDisconnected;
            _mqttClient.ConnectingFailedAsync += OnConnectingFailed;
            _mqttClient.ApplicationMessageReceivedAsync += OnMessageReceived;
        }

        public void SubscribeToTopics(List<string> topics)
        {
            List<MqttTopicFilter> topicFilters = [];
            StringBuilder sb = new();
            sb.AppendLine("Mqtt service subscribing to the following topics:");
            topics.ForEach(topic =>
            {
                topicFilters.Add(new MqttTopicFilter { Topic = topic });
                sb.AppendLine(topic);
            });
            _logger.LogInformation("{topicContent}", sb.ToString());
            _mqttClient.SubscribeAsync(topicFilters).Wait();
        }

        private void OnIsarTopicReceived<T>(string content)
            where T : MqttMessage
        {
            T? message;

            try
            {
                message = JsonSerializer.Deserialize<T>(content, serializerOptions);
                if (message is null)
                {
                    throw new JsonException();
                }
            }
            catch (Exception ex)
                when (ex is JsonException or NotSupportedException or ArgumentException)
            {
                _logger.LogError(
                    "Could not create '{className}' object from ISAR MQTT message json",
                    typeof(T).Name
                );
                return;
            }

            var type = typeof(T);
            try
            {
                var raiseEvent = type switch
                {
                    _ when type == typeof(IsarStatusMessage) => MqttIsarStatusReceived,
                    _ when type == typeof(IsarRobotInfoMessage) => MqttIsarRobotInfoReceived,
                    _ when type == typeof(IsarRobotHeartbeatMessage) =>
                        MqttIsarRobotHeartbeatReceived,
                    _ when type == typeof(IsarMissionMessage) => MqttIsarMissionReceived,
                    _ when type == typeof(IsarTaskMessage) => MqttIsarTaskReceived,
                    _ when type == typeof(IsarBatteryMessage) => MqttIsarBatteryReceived,
                    _ when type == typeof(IsarPressureMessage) => MqttIsarPressureReceived,
                    _ when type == typeof(IsarPoseMessage) => MqttIsarPoseReceived,
                    _ when type == typeof(IsarCloudHealthMessage) => MqttIsarCloudHealthReceived,
                    _ => throw new NotImplementedException(
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

        private void OnIdaTopicReceived<T>(string content)
            where T : MqttMessage
        {
            T? message;

            try
            {
                message = JsonSerializer.Deserialize<T>(content, serializerOptions);
                if (message is null)
                {
                    throw new JsonException();
                }
            }
            catch (Exception ex)
                when (ex is JsonException or NotSupportedException or ArgumentException)
            {
                _logger.LogError(
                    "Could not create '{className}' object from IDA MQTT message json",
                    typeof(T).Name
                );
                return;
            }

            var type = typeof(T);
            try
            {
                var raiseEvent = type switch
                {
                    _ when type == typeof(IdaInspectionResultMessage) =>
                        MqttIdaInspectionResultReceived,
                    _ => throw new NotImplementedException(
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
