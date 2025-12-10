using ZakYip.Sorting.RuleEngine.Domain.Entities;

namespace ZakYip.Sorting.RuleEngine.Application.Interfaces;

/// <summary>
/// WCS适配器管理器接口
/// WCS adapter manager interface
/// </summary>
public interface IWcsAdapterManager
{
    /// <summary>
    /// 使用新配置连接WCS
    /// Connect to WCS with new configuration
    /// </summary>
    Task ConnectAsync(WcsApiConfig config, CancellationToken cancellationToken = default);

    /// <summary>
    /// 断开WCS连接
    /// Disconnect from WCS
    /// </summary>
    Task DisconnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取连接状态
    /// Get connection status
    /// </summary>
    bool IsConnected { get; }
}
