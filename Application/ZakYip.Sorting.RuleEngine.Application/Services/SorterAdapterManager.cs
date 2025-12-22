using System.Text.Json;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Application.DTOs.Downstream;
using ZakYip.Sorting.RuleEngine.Application.Interfaces;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Events;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Application.Services;

/// <summary>
/// 分拣机适配器管理器实现
/// Sorter adapter manager implementation
/// 
/// 负责管理与下游分拣系统（ZakYip.WheelDiverterSorter）的通信
/// Manages communication with downstream sorter system (ZakYip.WheelDiverterSorter)
/// </summary>
public class SorterAdapterManager : ISorterAdapterManager
{
    private readonly ILogger<SorterAdapterManager> _logger;
    private readonly ISystemClock _clock;
    private readonly IAutoResponseModeService _autoResponseModeService;
    private readonly IDownstreamCommunication _downstreamCommunication;
    private SorterConfig? _currentConfig;
    private bool _isConnected;
    private readonly object _adapterLock = new();

    public SorterAdapterManager(
        ILogger<SorterAdapterManager> logger,
        ISystemClock clock,
        IAutoResponseModeService autoResponseModeService,
        IDownstreamCommunication downstreamCommunication)
    {
        _logger = logger;
        _clock = clock;
        _autoResponseModeService = autoResponseModeService;
        _downstreamCommunication = downstreamCommunication;
    }

    public bool IsConnected => _isConnected;

    public async Task ConnectAsync(SorterConfig config, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "开始连接下游分拣机: Protocol={Protocol}, ConnectionMode={ConnectionMode}, Host={Host}, Port={Port}",
                config.Protocol, config.ConnectionMode, config.Host, config.Port);

            lock (_adapterLock)
            {
                // 保存配置
                _currentConfig = config;

                // 验证协议类型
                if (!config.Protocol.Equals("TCP", StringComparison.OrdinalIgnoreCase))
                {
                    throw new NotSupportedException(
                        $"不支持的协议类型: {config.Protocol}。当前仅支持 TCP 协议与下游分拣系统通信。" +
                        $" / Unsupported protocol type: {config.Protocol}. Currently only TCP protocol is supported for downstream sorter communication.");
                }

                // 验证连接模式（目前仅支持Server模式）
                if (!config.ConnectionMode.Equals("SERVER", StringComparison.OrdinalIgnoreCase))
                {
                    throw new NotSupportedException(
                        $"连接模式 {config.ConnectionMode} 暂不支持。当前仅支持 SERVER 模式。" +
                        $" / Connection mode {config.ConnectionMode} is not supported yet. Only SERVER mode is currently supported.");
                }

                _isConnected = true;
            }

            // 启动下游通信服务
            await _downstreamCommunication.StartAsync(cancellationToken).ConfigureAwait(false);

            // 订阅事件
            SubscribeToDownstreamEvents();

            _logger.LogInformation(
                "下游分拣机已连接: Protocol={Protocol}, ConnectionMode={ConnectionMode}", 
                config.Protocol, config.ConnectionMode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "连接下游分拣机失败");
            _isConnected = false;
            throw;
        }
    }

    /// <summary>
    /// 订阅下游通信事件
    /// Subscribe to downstream communication events
    /// </summary>
    private void SubscribeToDownstreamEvents()
    {
        // 订阅包裹检测事件
        _downstreamCommunication.ParcelNotificationReceived += OnParcelNotificationReceived;
        
        // 订阅分拣完成事件
        _downstreamCommunication.SortingCompletedReceived += OnSortingCompletedReceived;
        
        // 订阅客户端连接事件
        _downstreamCommunication.ClientConnected += OnClientConnected;
        _downstreamCommunication.ClientDisconnected += OnClientDisconnected;
        
        _logger.LogInformation("已订阅下游通信事件 / Subscribed to downstream communication events");
    }

    /// <summary>
    /// 取消订阅下游通信事件
    /// Unsubscribe from downstream communication events
    /// </summary>
    private void UnsubscribeFromDownstreamEvents()
    {
        _downstreamCommunication.ParcelNotificationReceived -= OnParcelNotificationReceived;
        _downstreamCommunication.SortingCompletedReceived -= OnSortingCompletedReceived;
        _downstreamCommunication.ClientConnected -= OnClientConnected;
        _downstreamCommunication.ClientDisconnected -= OnClientDisconnected;
        
        _logger.LogInformation("已取消订阅下游通信事件 / Unsubscribed from downstream communication events");
    }

    private void OnParcelNotificationReceived(object? sender, ParcelNotificationReceivedEventArgs e)
    {
        _logger.LogInformation(
            "收到包裹检测通知: ParcelId={ParcelId}, ClientId={ClientId}",
            e.ParcelId, e.ClientId);
        
        // 自动应答逻辑可以在这里实现
        // Auto-response logic can be implemented here
    }

    private void OnSortingCompletedReceived(object? sender, SortingCompletedReceivedEventArgs e)
    {
        _logger.LogInformation(
            "收到分拣完成通知: ParcelId={ParcelId}, FinalStatus={FinalStatus}",
            e.ParcelId, e.FinalStatus);
    }

    private void OnClientConnected(object? sender, ClientConnectionEventArgs e)
    {
        _logger.LogInformation("下游客户端已连接: ClientId={ClientId}", e.ClientId);
    }

    private void OnClientDisconnected(object? sender, ClientConnectionEventArgs e)
    {
        _logger.LogInformation("下游客户端已断开: ClientId={ClientId}", e.ClientId);
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_isConnected)
            {
                _logger.LogInformation("分拣机未连接，无需断开");
                return;
            }

            _logger.LogInformation("开始断开分拣机连接");

            // 取消订阅事件
            UnsubscribeFromDownstreamEvents();

            // 停止下游通信服务
            await _downstreamCommunication.StopAsync(cancellationToken).ConfigureAwait(false);

            lock (_adapterLock)
            {
                _isConnected = false;
                _currentConfig = null;
            }

            _logger.LogInformation("分拣机连接已断开");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "断开分拣机连接时发生错误");
            throw;
        }
    }

    public async Task<bool> SendChuteNumberAsync(string parcelId, string chuteNumber, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_isConnected)
            {
                _logger.LogWarning("分拣机未连接，无法发送格口号");
                return false;
            }

            // 使用 TryParse 安全解析 ParcelId
            if (!long.TryParse(parcelId, out var parcelIdValue))
            {
                _logger.LogWarning("解析 ParcelId 失败，输入值无效: {ParcelId}", parcelId);
                return false;
            }

            // 使用 TryParse 安全解析 ChuteId
            if (!long.TryParse(chuteNumber, out var chuteIdValue))
            {
                _logger.LogWarning("解析 ChuteId 失败，输入值无效: {ChuteNumber}", chuteNumber);
                return false;
            }

            // 构造 ChuteAssignmentNotification 对象
            var notification = new ChuteAssignmentNotification
            {
                ParcelId = parcelIdValue,
                ChuteId = chuteIdValue,
                AssignedAt = _clock.LocalNow
            };

            // 序列化为JSON
            var json = JsonSerializer.Serialize(notification);

            // 调用下游通信接口发送
            await _downstreamCommunication.BroadcastChuteAssignmentAsync(json).ConfigureAwait(false);

            _logger.LogInformation(
                "已发送格口分配: ParcelId={ParcelId}, ChuteId={ChuteId}",
                parcelIdValue, chuteIdValue);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送格口号失败: ParcelId={ParcelId}, ChuteNumber={ChuteNumber}", parcelId, chuteNumber);
            return false;
        }
    }

    public async Task<string?> GetConnectionInfoAsync(CancellationToken cancellationToken = default)
    {
        if (_currentConfig == null)
        {
            return null;
        }

        return await Task.FromResult(
            $"Protocol={_currentConfig.Protocol}, Mode={_currentConfig.ConnectionMode}, " +
            $"Host={_currentConfig.Host}, Port={_currentConfig.Port}, Connected={_isConnected}");
    }
}
