using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Enums;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;
using ZakYip.Sorting.RuleEngine.Infrastructure.Utilities;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Adapters.Dws;

/// <summary>
/// 基于MQTTnet的DWS MQTT适配器
/// 支持订阅主题和自动重连
/// DWS MQTT adapter based on MQTTnet
/// Supports topic subscription and automatic reconnection
/// </summary>
public class MqttDwsAdapter : IDwsAdapter, IDisposable
{
    private readonly ILogger<MqttDwsAdapter> _logger;
    private readonly ICommunicationLogRepository _communicationLogRepository;
    private readonly string _brokerHost;
    private readonly int _brokerPort;
    private readonly string _subscribeTopic;
    private readonly string? _clientId;
    private readonly string? _username;
    private readonly string? _password;
    private IManagedMqttClient? _mqttClient;
    private bool _isRunning;
    private bool _isDisposed;

    public string AdapterName => "MQTT-DWS";
    public string ProtocolType => "MQTT";

    public event Func<DwsData, Task>? OnDwsDataReceived;

    /// <summary>
    /// 构造函数
    /// Constructor
    /// </summary>
    /// <param name="brokerHost">MQTT代理服务器地址 / MQTT broker host</param>
    /// <param name="brokerPort">MQTT代理服务器端口 / MQTT broker port</param>
    /// <param name="subscribeTopic">订阅主题 / Subscribe topic</param>
    /// <param name="logger">日志记录器 / Logger</param>
    /// <param name="communicationLogRepository">通信日志仓储 / Communication log repository</param>
    /// <param name="clientId">客户端ID（可选） / Client ID (optional)</param>
    /// <param name="username">用户名（可选） / Username (optional)</param>
    /// <param name="password">密码（可选） / Password (optional)</param>
    public MqttDwsAdapter(
        string brokerHost,
        int brokerPort,
        string subscribeTopic,
        ILogger<MqttDwsAdapter> logger,
        ICommunicationLogRepository communicationLogRepository,
        string? clientId = null,
        string? username = null,
        string? password = null)
    {
        _brokerHost = brokerHost;
        _brokerPort = brokerPort;
        _subscribeTopic = subscribeTopic;
        _logger = logger;
        _communicationLogRepository = communicationLogRepository;
        _clientId = clientId ?? $"dws-{Guid.NewGuid()}";
        _username = username;
        _password = password;
    }

    /// <summary>
    /// 启动DWS MQTT监听
    /// Start DWS MQTT listener
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_isRunning)
        {
            _logger.LogWarning("DWS MQTT适配器已经在运行中");
            return;
        }

        try
        {
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

            // 设置消息接收事件
            _mqttClient.ApplicationMessageReceivedAsync += OnMessageReceivedAsync;

            // 设置连接状态变化事件
            _mqttClient.ConnectedAsync += async e =>
            {
                _logger.LogInformation("DWS MQTT连接已建立，代理: {Broker}:{Port}", _brokerHost, _brokerPort);
                await _communicationLogRepository.LogCommunicationAsync(
                    CommunicationType.Mqtt,
                    CommunicationDirection.Inbound,
                    $"DWS MQTT连接已建立: {_brokerHost}:{_brokerPort}",
                    remoteAddress: $"{_brokerHost}:{_brokerPort}",
                    isSuccess: true);

                // 订阅主题
                await _mqttClient.SubscribeAsync(_subscribeTopic);
                _logger.LogInformation("DWS MQTT已订阅主题: {Topic}", _subscribeTopic);
            };

            _mqttClient.DisconnectedAsync += async e =>
            {
                _logger.LogWarning("DWS MQTT连接已断开，代理: {Broker}:{Port}", _brokerHost, _brokerPort);
                await _communicationLogRepository.LogCommunicationAsync(
                    CommunicationType.Mqtt,
                    CommunicationDirection.Inbound,
                    $"DWS MQTT连接已断开: {_brokerHost}:{_brokerPort}",
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

            _isRunning = true;
            _logger.LogInformation("DWS MQTT监听已启动: {Broker}:{Port}, 主题: {Topic}", 
                _brokerHost, _brokerPort, _subscribeTopic);

            await _communicationLogRepository.LogCommunicationAsync(
                CommunicationType.Mqtt,
                CommunicationDirection.Inbound,
                $"DWS MQTT监听已启动: {_brokerHost}:{_brokerPort}, 主题: {_subscribeTopic}",
                remoteAddress: $"{_brokerHost}:{_brokerPort}/{_subscribeTopic}",
                isSuccess: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "启动DWS MQTT监听失败");
            await _communicationLogRepository.LogCommunicationAsync(
                CommunicationType.Mqtt,
                CommunicationDirection.Inbound,
                $"启动DWS MQTT监听失败: {ex.Message}",
                remoteAddress: $"{_brokerHost}:{_brokerPort}",
                isSuccess: false,
                errorMessage: ex.Message);
            throw;
        }
    }

    /// <summary>
    /// 停止DWS MQTT监听
    /// Stop DWS MQTT listener
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!_isRunning || _mqttClient == null)
        {
            return;
        }

        try
        {
            // 取消订阅主题
            await _mqttClient.UnsubscribeAsync(_subscribeTopic);
            
            await _mqttClient.StopAsync();
            _mqttClient.Dispose();
            _mqttClient = null;
            _isRunning = false;

            _logger.LogInformation("DWS MQTT监听已停止");
            await _communicationLogRepository.LogCommunicationAsync(
                CommunicationType.Mqtt,
                CommunicationDirection.Inbound,
                "DWS MQTT监听已停止",
                isSuccess: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "停止DWS MQTT监听失败");
        }
    }

    /// <summary>
    /// MQTT消息接收处理
    /// MQTT message received handler
    /// </summary>
    private async Task OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs e)
    {
        try
        {
            var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
            _logger.LogInformation("收到DWS MQTT数据，主题: {Topic}, 消息: {Message}", 
                e.ApplicationMessage.Topic, payload);

            await _communicationLogRepository.LogCommunicationAsync(
                CommunicationType.Mqtt,
                CommunicationDirection.Inbound,
                payload,
                remoteAddress: $"{_brokerHost}:{_brokerPort}/{e.ApplicationMessage.Topic}",
                isSuccess: true);

            // 解析DWS数据（JSON格式）
            var dwsData = JsonSerializer.Deserialize<DwsData>(payload, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (dwsData != null)
            {
                // 🛡️ 安全触发事件委托，防止订阅者异常导致适配器崩溃
                // Safely trigger event delegate, prevent subscriber exceptions from crashing adapter
                await OnDwsDataReceived.SafeInvokeAsync(dwsData, _logger, nameof(OnDwsDataReceived)).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理DWS MQTT数据失败");
            await _communicationLogRepository.LogCommunicationAsync(
                CommunicationType.Mqtt,
                CommunicationDirection.Inbound,
                Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment),
                remoteAddress: $"{_brokerHost}:{_brokerPort}/{e.ApplicationMessage.Topic}",
                isSuccess: false,
                errorMessage: ex.Message);
        }
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        StopAsync().Wait();
    }
}
