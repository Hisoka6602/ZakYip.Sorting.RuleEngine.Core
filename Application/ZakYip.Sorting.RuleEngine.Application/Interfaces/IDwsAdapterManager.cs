using ZakYip.Sorting.RuleEngine.Domain.Entities;

namespace ZakYip.Sorting.RuleEngine.Application.Interfaces;

/// <summary>
/// DWS适配器管理器接口
/// DWS adapter manager interface
/// </summary>
public interface IDwsAdapterManager
{
    /// <summary>
    /// 使用新配置连接DWS
    /// Connect to DWS with new configuration
    /// </summary>
    Task ConnectAsync(DwsConfig config, CancellationToken cancellationToken = default);

    /// <summary>
    /// 断开DWS连接
    /// Disconnect from DWS
    /// </summary>
    Task DisconnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取连接状态
    /// Get connection status
    /// </summary>
    bool IsConnected { get; }
}
