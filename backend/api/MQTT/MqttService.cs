using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Api.Services.Events;
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

        private static readonly JsonSerializerOptions serializerOptions = new()
        {
            Converters = { new JsonStringEnumConverter() }, // Needed for enums becoming their names in strings, such as "Charging" instead of "1".
        };

        private CancellationToken _cancellationToken;
        private int _reconnectAttempts;
        private EventAggregatorSingletonService _eventAggregatorSingletonService;

        public MqttService(
            ILogger<MqttService> logger,
            IConfiguration config,
            EventAggregatorSingletonService eventAggregatorSingletonService
        )
        {
            _reconnectAttempts = 0;
            _logger = logger;
            var mqttFactory = new MqttFactory();
            _mqttClient = mqttFactory.CreateManagedMqttClient();
            _eventAggregatorSingletonService = eventAggregatorSingletonService;

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

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _cancellationToken = stoppingToken;
            _logger.LogInformation("MQTT client STARTED");
            await _mqttClient.StartAsync(_options);
            await Task.Delay(Timeout.Infinite, stoppingToken);
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
            PublishMessageBasedOnTopic(topic, content);

            return Task.CompletedTask;
        }

        public Task PublishMessageBasedOnTopic(string topic, string content)
        {
            var messageType = MqttTopics.TopicsToMessages.GetItemByTopic(topic);
            if (messageType is null)
            {
                _logger.LogError("No message class defined for topic '{topicName}'", topic);
                return Task.CompletedTask;
            }

            _logger.LogDebug("Topic: {topic} - Message received: \n{payload}", topic, content);

            var contentObject = JsonSerializer.Deserialize(content, messageType, serializerOptions);

            _eventAggregatorSingletonService.Publish(contentObject); // The type of this object determines what subscribers are being published to.

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
    }
}
