using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using System.Text;
using System.Text.Json;
using ZakYip.Sorting.RuleEngine.Domain.Enums;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Adapters.Sorter;

/// <summary>
/// 基于MQTTnet的分拣机MQTT适配器
/// 支持自动重连和QoS控制
/// MQTT sorter adapter based on MQTTnet
/// Supports automatic reconnection and QoS control
/// </summary>
public class MqttSorterAdapter : ISorterAdapter, IDisposable
{
    private readonly ILogger<MqttSorterAdapter> _logger;
    private readonly ICommunicationLogRepository _communicationLogRepository;
    private readonly ZakYip.Sorting.RuleEngine.Domain.Interfaces.ISystemClock _clock;
    private readonly string _brokerHost;
    private readonly int _brokerPort;
    private readonly string _publishTopic;
    private readonly string? _clientId;
    private readonly string? _username;
    private readonly string? _password;
    private IManagedMqttClient? _mqttClient;
    private readonly object _lockObj = new();
    private bool _isDisposed;

    public string AdapterName => "MQTT-Sorter";
    public string ProtocolType => "MQTT";

    /// <summary>
    /// 构造函数
    /// Constructor
    /// </summary>
    /// <param name="brokerHost">MQTT代理服务器地址 / MQTT broker host</param>
    /// <param name="brokerPort">MQTT代理服务器端口 / MQTT broker port</param>
    /// <param name="publishTopic">发布主题 / Publish topic</param>
    /// <param name="logger">日志记录器 / Logger</param>
    /// <param name="communicationLogRepository">通信日志仓储 / Communication log repository</param>
    /// <param name="clock">系统时钟 / System clock</param>
    /// <param name="clientId">客户端ID（可选） / Client ID (optional)</param>
    /// <param name="username">用户名（可选） / Username (optional)</param>
    /// <param name="password">密码（可选） / Password (optional)</param>
    public MqttSorterAdapter(
        string brokerHost,
        int brokerPort,
        string publishTopic,
        ILogger<MqttSorterAdapter> logger,
        ICommunicationLogRepository communicationLogRepository,
        ZakYip.Sorting.RuleEngine.Domain.Interfaces.ISystemClock clock,
        string? clientId = null,
        string? username = null,
        string? password = null)
    {
        _brokerHost = brokerHost;
        _brokerPort = brokerPort;
        _publishTopic = publishTopic;
        _logger = logger;
        _communicationLogRepository = communicationLogRepository;
        _clock = clock;
        _clientId = clientId ?? $"sorter-{Guid.NewGuid()}";
        _username = username;
        _password = password;
    }

    /// <summary>
    /// 发送格口号到分拣机
    /// Send chute number to sorter
    /// </summary>
    public async Task<bool> SendChuteNumberAsync(string parcelId, string chuteNumber, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureConnectedAsync(cancellationToken);

            if (_mqttClient?.IsConnected != true)
            {
                _logger.LogWarning("MQTT连接未建立，无法发送数据");
                await _communicationLogRepository.LogCommunicationAsync(
                    CommunicationType.Mqtt,
                    CommunicationDirection.Outbound,
                    $"包裹ID: {parcelId}, 格口: {chuteNumber}",
                    parcelId: parcelId,
                    remoteAddress: $"{_brokerHost}:{_brokerPort}",
                    isSuccess: false,
                    errorMessage: "MQTT连接未建立");
                return false;
            }

            // 构造消息：JSON格式，包含包裹ID和格口号
            var message = new
            {
                ParcelId = parcelId,
                ChuteNumber = chuteNumber,
                Timestamp = _clock.LocalNow
            };
            var jsonMessage = JsonSerializer.Serialize(message);
            var payload = Encoding.UTF8.GetBytes(jsonMessage);

            var mqttMessage = new MqttApplicationMessageBuilder()
                .WithTopic(_publishTopic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .WithRetainFlag(false)
                .Build();

            await _mqttClient.EnqueueAsync(mqttMessage);

            _logger.LogInformation("MQTT发送成功，包裹ID: {ParcelId}, 格口: {Chute}, 主题: {Topic}", 
                parcelId, chuteNumber, _publishTopic);
            await _communicationLogRepository.LogCommunicationAsync(
                CommunicationType.Mqtt,
                CommunicationDirection.Outbound,
                jsonMessage,
                parcelId: parcelId,
                remoteAddress: $"{_brokerHost}:{_brokerPort}/{_publishTopic}",
                isSuccess: true);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MQTT发送失败，包裹ID: {ParcelId}", parcelId);
            await _communicationLogRepository.LogCommunicationAsync(
                CommunicationType.Mqtt,
                CommunicationDirection.Outbound,
                $"包裹ID: {parcelId}, 格口: {chuteNumber}",
                parcelId: parcelId,
                remoteAddress: $"{_brokerHost}:{_brokerPort}/{_publishTopic}",
                isSuccess: false,
                errorMessage: ex.Message);
            return false;
        }
    }

    /// <summary>
    /// 检查连接状态
    /// Get connection status
    /// </summary>
    public Task<bool> IsConnectedAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_mqttClient?.IsConnected == true);
    }

    /// <summary>
    /// 确保MQTT连接已建立
    /// Ensure MQTT connection is established
    /// </summary>
    private async Task EnsureConnectedAsync(CancellationToken cancellationToken)
    {
        lock (_lockObj)
        {
            if (_mqttClient?.IsConnected == true)
                return;
        }

        try
        {
            if (_mqttClient != null)
            {
                await _mqttClient.StopAsync();
                _mqttClient.Dispose();
            }

            var mqttFactory = new MqttFactory();
            _mqttClient = mqttFactory.CreateManagedMqttClient();

            // 配置MQTT选项
            var clientOptionsBuilder = new MqttClientOptionsBuilder()
                .WithTcpServer(_brokerHost, _brokerPort)
                .WithClientId(_clientId)
                .WithCleanSession();

            if (!string.IsNullOrEmpty(_username) && !string.IsNullOrEmpty(_password))
            {
                clientOptionsBuilder.WithCredentials(_username, _password);
            }

            var managedOptions = new ManagedMqttClientOptionsBuilder()
                .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                .WithClientOptions(clientOptionsBuilder.Build())
                .Build();

            // 设置连接状态变化事件
            _mqttClient.ConnectedAsync += async e =>
            {
                _logger.LogInformation("MQTT连接已建立，代理: {Broker}:{Port}", _brokerHost, _brokerPort);
                await _communicationLogRepository.LogCommunicationAsync(
                    CommunicationType.Mqtt,
                    CommunicationDirection.Outbound,
                    $"MQTT连接已建立: {_brokerHost}:{_brokerPort}",
                    remoteAddress: $"{_brokerHost}:{_brokerPort}",
                    isSuccess: true);
            };

            _mqttClient.DisconnectedAsync += async e =>
            {
                _logger.LogWarning("MQTT连接已断开，代理: {Broker}:{Port}", _brokerHost, _brokerPort);
                await _communicationLogRepository.LogCommunicationAsync(
                    CommunicationType.Mqtt,
                    CommunicationDirection.Outbound,
                    $"MQTT连接已断开: {_brokerHost}:{_brokerPort}",
                    remoteAddress: $"{_brokerHost}:{_brokerPort}",
                    isSuccess: false,
                    errorMessage: e.Reason.ToString());
            };

            await _mqttClient.StartAsync(managedOptions);

            // 等待连接建立（最多5秒）
            var waitTime = TimeSpan.FromSeconds(5);
            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (!_mqttClient.IsConnected && sw.Elapsed < waitTime)
            {
                await Task.Delay(100, cancellationToken);
            }
            sw.Stop();

            if (!_mqttClient.IsConnected)
            {
                throw new InvalidOperationException($"无法连接到MQTT代理: {_brokerHost}:{_brokerPort}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MQTT连接失败，代理: {Broker}:{Port}", _brokerHost, _brokerPort);
            await _communicationLogRepository.LogCommunicationAsync(
                CommunicationType.Mqtt,
                CommunicationDirection.Outbound,
                $"MQTT连接失败: {_brokerHost}:{_brokerPort}",
                remoteAddress: $"{_brokerHost}:{_brokerPort}",
                isSuccess: false,
                errorMessage: ex.Message);
            throw;
        }
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        _mqttClient?.StopAsync().Wait();
        _mqttClient?.Dispose();
    }
}
