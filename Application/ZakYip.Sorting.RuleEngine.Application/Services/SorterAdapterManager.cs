using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Application.Interfaces;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
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
    private readonly ILoggerFactory _loggerFactory;
    private SorterConfig? _currentConfig;
    private ISorterAdapter? _currentAdapter;
    private bool _isConnected;
    private readonly object _lock = new();

    public SorterAdapterManager(
        ILogger<SorterAdapterManager> logger,
        ILoggerFactory loggerFactory)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
    }

    public bool IsConnected => _isConnected;

    public async Task ConnectAsync(SorterConfig config, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "开始连接下游分拣机: Protocol={Protocol}, Host={Host}, Port={Port}",
                config.Protocol, config.Host, config.Port);

            lock (_lock)
            {
                // 保存配置
                _currentConfig = config;

                // 根据协议类型创建相应的适配器
                // Create adapter based on protocol type
                _currentAdapter = CreateAdapterForProtocol(config);

                _isConnected = true;
            }

            _logger.LogInformation("下游分拣机适配器已创建: {Protocol}, AdapterName={AdapterName}", 
                config.Protocol, _currentAdapter.AdapterName);
            
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "连接下游分拣机失败");
            _isConnected = false;
            throw;
        }
    }

    /// <summary>
    /// 根据协议类型创建适配器
    /// Create adapter based on protocol type
    /// </summary>
    private ISorterAdapter CreateAdapterForProtocol(SorterConfig config)
    {
        var protocol = config.Protocol.ToUpperInvariant();
        
        return protocol switch
        {
            "TCP" => CreateTcpAdapter(config),
            _ => throw new NotSupportedException(
                $"不支持的协议类型: {config.Protocol}。当前仅支持 TCP 协议与下游分拣系统通信。" +
                $" / Unsupported protocol type: {config.Protocol}. Currently only TCP protocol is supported for downstream sorter communication.")
        };
    }

    /// <summary>
    /// 创建 TCP 适配器
    /// Create TCP adapter
    /// </summary>
    private ISorterAdapter CreateTcpAdapter(SorterConfig config)
    {
        var logger = _loggerFactory.CreateLogger("ZakYip.Sorting.RuleEngine.Infrastructure.Adapters.Sorter.TcpSorterAdapter");
        
        // 使用反射创建 TcpSorterAdapter，避免直接引用 Infrastructure 层
        // Use reflection to create TcpSorterAdapter to avoid direct reference to Infrastructure layer
        var adapterType = Type.GetType("ZakYip.Sorting.RuleEngine.Infrastructure.Adapters.Sorter.TcpSorterAdapter, ZakYip.Sorting.RuleEngine.Infrastructure");
        
        if (adapterType == null)
        {
            throw new InvalidOperationException("无法加载 TcpSorterAdapter 类型 / Cannot load TcpSorterAdapter type");
        }

        var adapter = Activator.CreateInstance(adapterType, config.Host, config.Port, logger) as ISorterAdapter;
        
        if (adapter == null)
        {
            throw new InvalidOperationException("无法创建 TcpSorterAdapter 实例 / Cannot create TcpSorterAdapter instance");
        }

        return adapter;
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

            lock (_lock)
            {
                // 清理适配器资源
                if (_currentAdapter is IDisposable disposable)
                {
                    disposable.Dispose();
                }
                _currentAdapter = null;
                _isConnected = false;
            }

            _logger.LogInformation("分拣机连接已断开");
            
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "断开分拣机连接失败");
            throw;
        }
    }

    public async Task<bool> SendChuteNumberAsync(string parcelId, string chuteNumber, CancellationToken cancellationToken = default)
    {
        try
        {
            ISorterAdapter? adapter;
            lock (_lock)
            {
                if (!_isConnected || _currentAdapter == null)
                {
                    _logger.LogWarning("分拣机未连接，无法发送格口号");
                    return false;
                }
                adapter = _currentAdapter;
            }

            _logger.LogInformation(
                "发送格口号到分拣机: ParcelId={ParcelId}, ChuteNumber={ChuteNumber}",
                parcelId, chuteNumber);

            // 调用适配器发送格口号到下游分拣机
            return await adapter.SendChuteNumberAsync(parcelId, chuteNumber, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送格口号失败: ParcelId={ParcelId}", parcelId);
            return false;
        }
    }
}
